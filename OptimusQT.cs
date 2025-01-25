using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;
using TradingPlatform.BusinessLayer.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace Omnibus
{
    public enum KaasSetup
    {
        Long, Short, None
    }

    public class Bars
    {
        public HistoryItemBar bar;
        public int barIndex;
        public KaasSetup setupDefault = KaasSetup.None;
        public Color color;

        public Bars(HistoryItemBar b, int i, KaasSetup s, Color color)
        {
            this.bar = b;
            this.barIndex = i;
            this.setupDefault = s;
            this.color = color;
        }
    }

    public class Omnibus : Indicator
    {
        private List<Bars> barslist = new List<Bars>();

        private Indicator BB;
        private Indicator EMA200;
        private Indicator KAMA;
        private Indicator MACD;
        private Indicator MACD2;
        private Indicator SAR;
        private Indicator RSI;
        private Indicator fastSma;
        private Indicator slowSma;
        private Indicator bbSma;

        public Color LongColor = Color.Lime;
        public Color ShortColor = Color.Red;

        [InputParameter("Hull Moving Average Period")]
        public int Period = 9;

        [InputParameter("Waddah Sensitivity")]
        public int Sensitivity = 150;

        [InputParameter("Waddah BB Length")]
        public int BBChanLength = 20;

        [InputParameter("Waddah Dead zone")]
        public int DeadZone = 20;

        [InputParameter("Waddah BB StdDev")]
        public double BBMult = 2.0;

        [InputParameter("Waddah Fast SMA period")]
        public int FastPeriod = 20;

        [InputParameter("Waddah Slow SMA period")]
        public int SlowPeriod = 40;

        private int sqrtPeriod;

        private double[] macdBuffer;
        private double[] priceBuffer;
        private double[] priceBuffer2;
        private double[] wmaFull;
        private double[] wmaHalf;
        private double[] wmaDiff;
        private double[] wmaSmoothed;

        private bool bWaddahUp = false;
        private bool bWaddahDown = false;
        private bool bHullUp = false;
        private bool bHullDown = false;
        private bool bPSARUp = false;
        private bool bPSARDown = false;
        private double dLindaMACD = 0;

        public Omnibus()
            : base()
        {
            Name = "Omnibus";
            Description = "Immortal Hulk Omnibus";

            AddLineSeries("UB", Color.White, 1, LineStyle.Solid);
            AddLineSeries("BB", Color.White, 1, LineStyle.Solid);
            AddLineSeries("Touch Marker", Color.Transparent, 0, LineStyle.Points);

            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            sqrtPeriod = (int)Math.Sqrt(Period);

            int dataCount = this.HistoricalData.Count;
            priceBuffer = new double[dataCount];
            priceBuffer2 = new double[dataCount];
            macdBuffer = new double[dataCount];
            wmaFull = new double[dataCount];
            wmaHalf = new double[dataCount];
            wmaDiff = new double[dataCount];
            wmaSmoothed = new double[dataCount];

            BB = Core.Indicators.BuiltIn.BB(20, 2.0, PriceType.Close, MaMode.SMA);
            AddIndicator(BB);
            EMA200 = Core.Indicators.BuiltIn.EMA(200, PriceType.Close);
            AddIndicator(EMA200);
            KAMA = Core.Indicators.BuiltIn.KAMA(9, 2, 109, PriceType.Close);
            AddIndicator(KAMA);
            MACD = Core.Indicators.BuiltIn.MACD(12, 26, 9);
            AddIndicator(MACD);
            SAR = Core.Indicators.BuiltIn.SAR(0.02, 0.2);
            AddIndicator(SAR);
            RSI = Core.Indicators.BuiltIn.RSI(14, PriceType.Close, RSIMode.Exponential, MaMode.SMA, 9);
            AddIndicator(RSI);
            MACD2 = Core.Indicators.BuiltIn.MACD(20, 40, 9);
            AddIndicator(MACD2);
            fastSma = Core.Indicators.BuiltIn.SMA(20, PriceType.Close);
            AddIndicator(fastSma);
            slowSma = Core.Indicators.BuiltIn.SMA(40, PriceType.Close);
            AddIndicator(slowSma);
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.HistoricalBar)
            {
                bPSARUp = SAR.GetValue() < this.Close();
                bPSARDown = SAR.GetValue() > this.Close();

                // WADDAH EXPLOSION
                var fastMinusSlowCurr = fastSma.GetValue(0) - slowSma.GetValue(0);
                var fastMinusSlowPrev = fastSma.GetValue(1) - slowSma.GetValue(1);
                var fastMinusSlowMorePrev = fastSma.GetValue(2, 0) - slowSma.GetValue(2, 0);
                var t1 = (fastMinusSlowCurr - fastMinusSlowPrev) * Sensitivity;
                var t1Prev = (fastMinusSlowPrev - fastMinusSlowMorePrev) * Sensitivity;
                var e1 = BB.GetValue(0, 0) - BB.GetValue(0, 2); // Upper = index 0, Lower = index 2
                var trendUp = t1 >= 0 ? t1 : 0;
                var trendUpPrev = t1Prev >= 0 ? t1Prev : 0;
                var trendDown = t1 < 0 ? (t1 * -1) : 0;
                var trendDownPrev = t1Prev < 0 ? (t1Prev * -1) : 0;
                bWaddahUp = t1 >= 0;
                bWaddahDown = t1 < 0;

                // HULL MOVING AVERAGE
                int lastIndex = this.Count;
                int secondIndex = this.Count - 1;
                double closePrice = this.GetPrice(PriceType.Close, 0);
                priceBuffer[lastIndex] = closePrice;
                wmaFull[lastIndex] = WeightedMovingAverage(priceBuffer, Period, lastIndex);
                int halfPeriod = Math.Max(1, Period / 2); 
                wmaHalf[lastIndex] = WeightedMovingAverage(priceBuffer, halfPeriod, lastIndex);
                wmaDiff[lastIndex] = 2.0d * wmaHalf[lastIndex] - wmaFull[lastIndex];
                wmaSmoothed[lastIndex] = WeightedMovingAverage(wmaDiff, sqrtPeriod, lastIndex);
                bHullUp = wmaSmoothed[lastIndex] > wmaSmoothed[secondIndex];
                bHullDown = wmaSmoothed[lastIndex] < wmaSmoothed[secondIndex];
                //this.SetValue(wmaSmoothed[lastIndex]);

                // LINDA MACD
                //priceBuffer[lastIndex] = closePrice;
                double fastSMA = SimpleMovingAverage(priceBuffer, 3, lastIndex);
                double slowSMA = SimpleMovingAverage(priceBuffer, 10, lastIndex);
                double macdLine = fastSMA - slowSMA;
                macdBuffer[lastIndex] = macdLine;
                double signalLine = SimpleMovingAverage(macdBuffer, 16, lastIndex);
                double histogram = macdLine - signalLine;
                dLindaMACD = histogram;
                if (histogram >= 0)
                {
                    // Positive histogram

                }
                else
                {
                    // Negative histogram

                }
            }

            GetBars();
        }

        public void GetBars()
        {
            int StartingIndex = Math.Max(Count - 1, 2);

            for (int i = 1; i < 3; i++)
            {
                int currentIndex = StartingIndex - i;
                if (currentIndex < 0 || currentIndex >= Count)
                    break;

                double upperBand = BB.GetValue(StartingIndex - i, 0, SeekOriginHistory.Begin);
                double lowerBand = BB.GetValue(StartingIndex - i, 2, SeekOriginHistory.Begin);

                HistoryItemBar candle = (HistoryItemBar)HistoricalData[StartingIndex - i, SeekOriginHistory.Begin];
                HistoryItemBar NextCandle = (HistoryItemBar)HistoricalData[StartingIndex - i + 1, SeekOriginHistory.Begin];

                if (bWaddahUp && dLindaMACD > 0 && bHullUp && bPSARUp)
                {
                    Bars b = new Bars(candle, i, KaasSetup.Long, LongColor);
                    if (!barslist.Contains(b))
                    {
                        barslist.Add(b);
                    }
                }

                if (bWaddahDown && dLindaMACD < 0 && bHullDown && bPSARDown)
                {
                    Bars b = new Bars(candle, i, KaasSetup.Short, ShortColor);
                    if (!barslist.Contains(b))
                    {
                        barslist.Add(b);
                    }
                }
            }
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);
            if (CurrentChart == null)
                return;

            Graphics graphics = args.Graphics;
            IChartWindow mainWindow = CurrentChart.MainWindow;

            barslist.FindAll(item => true)
                .ForEach(item =>
                {
                    double drawingPrice = item.bar.Low;
                    int xCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(item.bar.TimeRight) - (CurrentChart.BarsWidth / 2));
                    int yCoord = 0;

                    if (item.setupDefault == KaasSetup.Long)
                    {
                        yCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(drawingPrice));
                        graphics.FillEllipse(new SolidBrush(item.color), xCoord - 2, yCoord + 11, 7, 7);
                    }
                    else
                    {
                        drawingPrice = item.bar.High;
                        yCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(drawingPrice));
                        graphics.FillEllipse(new SolidBrush(item.color), xCoord - 2, yCoord - 15, 7, 7);
                    }

                });
        }

        private double WeightedMovingAverage(double[] source, int length, int currentIndex)
        {
            if (length <= 0 || currentIndex - length + 1 < 0)
                return 0;

            double sum = 0;
            double weightSum = 0;
            int weight = length;

            for (int i = currentIndex; i > currentIndex - length; i--)
            {
                sum += source[i] * weight;
                weightSum += weight;
                weight--;
            }

            if (weightSum == 0)
                return 0;

            return sum / weightSum;
        }

        private double SimpleMovingAverage(double[] source, int length, int currentIndex)
        {
            if (length <= 0 || currentIndex - length + 1 < 0)
                return 0.0;

            double sum = 0.0;
            for (int i = currentIndex; i > currentIndex - length; i--)
                sum += source[i];

            return sum / length;
        }

    }
}
