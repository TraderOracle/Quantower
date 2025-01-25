using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace QuantowerIndicators
{
    public class LindaMACD : Indicator
    {
        [InputParameter("Fast SMA Period", 0, 1, 999, 1, 0)]
        public int FastPeriod = 3;

        [InputParameter("Slow SMA Period", 1, 1, 999, 1, 0)]
        public int SlowPeriod = 9;

        [InputParameter("Signal SMA Period", 2, 1, 999, 1, 0)]
        public int SignalPeriod = 16;

        private double[] priceBuffer;
        private double[] macdBuffer;

        public LindaMACD()
        {
            this.Name = "Linda MACD";
            this.Description = "A MACD indicator computed using Simple Moving Averages (SMA) instead of EMAs";
            this.SeparateWindow = true; 

            this.AddLineSeries("MACD", Color.Blue, 1, LineStyle.Solid);
            this.AddLineSeries("Signal", Color.Red, 1, LineStyle.Solid);
            this.AddLineSeries("HistogramPos", Color.Green, 10, LineStyle.Histogramm);
            this.AddLineSeries("HistogramNeg", Color.Red, 10, LineStyle.Histogramm);
        }

        protected override void OnInit()
        {
            int dataCount = this.HistoricalData.Count;
            priceBuffer = new double[dataCount];
            macdBuffer = new double[dataCount];
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.HistoricalBar)
            {
                int lastIndex = this.Count - 1;

                double closePrice = (double)this.GetPrice(PriceType.Close, 0);
                priceBuffer[lastIndex] = closePrice;

                double fastSMA = SimpleMovingAverage(priceBuffer, FastPeriod, lastIndex);

                double slowSMA = SimpleMovingAverage(priceBuffer, SlowPeriod, lastIndex);

                double macdLine = fastSMA - slowSMA;
                macdBuffer[lastIndex] = macdLine;

                double signalLine = SimpleMovingAverage(macdBuffer, SignalPeriod, lastIndex);

                double histogram = macdLine - signalLine;

                if (histogram >= 0)
                {
                    // Positive histogram
                    this.SetValue(histogram, 2);
                    this.SetValue(0, 3);  // Clear the negative line
                }
                else
                {
                    // Negative histogram
                    this.SetValue(0, 2); // Clear the positive line
                    this.SetValue(Math.Abs(histogram), 3);
                }

                //this.SetValue(macdLine, 0);
                //this.SetValue(signalLine, 1);
                //this.SetValue(Math.Abs(histogram), 2);
            }
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
