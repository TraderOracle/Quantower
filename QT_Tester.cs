using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.ConstrainedExecution;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;

namespace Oscillators;

public class Imbalance
{
    public HistoryItemBar begin;
    public HistoryItemBar end;

    public Imbalance(HistoryItemBar b, HistoryItemBar e)
    {
        this.begin = b;
        this.end = e;
    }
}

public class Bars
{
    public enum HiLo
    {
        Long, Short, None
    }

    public HistoryItemBar bar;
    public HiLo setupDefault = HiLo.None;
    public int barIndex;
    public Color color;
    public int endBarIndex;

    public Bars(HistoryItemBar b, int i, HiLo s, Color color)
    {
        this.bar = b;
        this.barIndex = i;
        this.color = color;
    }
}

public sealed class IndicatorMovingAverageConvergenceDivergence : Indicator, IWatchlistIndicator
{
    private Indicator fastSMA;
    private Indicator slowSMA;
    private Indicator signal;
    private Indicator Rsi;
    private Indicator ma;
    public List<Bars> barslist = new List<Bars>();
    public List<Imbalance> volImb = new List<Imbalance>();

    [InputParameter("Fast SMA Period", 0, 1, 999, 1, 0)]
    public int FastPeriod = 3;

    [InputParameter("Slow SMA Period", 1, 1, 999, 1, 0)]
    public int SlowPeriod = 9;

    [InputParameter("Signal SMA Period", 2, 1, 999, 1, 0)]
    public int SignalPeriod = 16;

    public int MinHistoryDepths => this.MaxEMAPeriod + this.SignalPeriod;
    private int MaxEMAPeriod => Math.Max(this.FastPeriod, this.SlowPeriod);

    public IndicatorMovingAverageConvergenceDivergence()
        : base()
    {
        this.Name = "QT_Tester2";
 
        //this.AddLineSeries("MACD", Color.DodgerBlue, 1, LineStyle.Solid);
        //this.AddLineSeries("HistogramPos", Color.Green, 10, LineStyle.Columns);
        //this.AddLineSeries("HistogramNeg", Color.Red, 10, LineStyle.Columns);

        //this.SeparateWindow = true;
    }

    protected override void OnInit()
    {
        this.fastSMA = Core.Indicators.BuiltIn.SMA(this.FastPeriod, PriceType.Typical);
        this.slowSMA = Core.Indicators.BuiltIn.SMA(this.SlowPeriod, PriceType.Typical);
        this.signal = Core.Indicators.BuiltIn.SMA(this.SignalPeriod, PriceType.Typical);
        this.ma = Core.Indicators.BuiltIn.MA(20, PriceType.Close, MaMode.SMA, Indicator.DEFAULT_CALCULATION_TYPE);
        this.Rsi = Core.Indicators.BuiltIn.RSI(14, PriceType.Close, RSIMode.Exponential, MaMode.EMA, 10);

        this.AddIndicator(this.fastSMA);
        this.AddIndicator(this.slowSMA);
        this.AddIndicator(this.signal);
        this.AddIndicator(this.ma);
        this.AddIndicator(this.Rsi);
    }

    public override void OnPaintChart(PaintChartEventArgs args)
    {
        base.OnPaintChart(args);
        if (CurrentChart == null)
            return;

        Graphics graphics = args.Graphics;
        IChartWindow mainWindow = CurrentChart.MainWindow;

        volImb.FindAll(item => true)
            .ForEach(item =>
            {
                int xCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(item.begin.TimeRight));
                int yy = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(item.begin.Close));
                int iEnd = xCoord + 10000;
                if (item.end != null)
                    iEnd = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(item.end.TimeRight) - 
                        (CurrentChart.BarsWidth / 2));

                graphics.DrawLine(new Pen(new SolidBrush(Color.FromArgb(255, 139, 188, 252)), 3), xCoord, yy, iEnd, yy);
            });

        barslist.FindAll(item => true)
            .ForEach(item =>
            {
                double drawingPrice = item.setupDefault == Bars.HiLo.Long ? item.bar.Low : item.bar.High;

                int xCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(item.bar.TimeRight) - (CurrentChart.BarsWidth / 2));
                int yCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(drawingPrice));

                //graphics.FillEllipse(new SolidBrush(item.color), xCoord - 10, yCoord - 10, 8, 8);
                graphics.DrawString("TR", new Font("Arial", 11, FontStyle.Bold), new SolidBrush(Color.Yellow), xCoord - 10, yCoord);
            });
    }

    private void SendAlert()
    {

    }

    protected override void OnUpdate(UpdateArgs args)
    {
        if (this.Count < this.MaxEMAPeriod)
            return;

        if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.HistoricalBar)
        {
            HistoryItemBar candle = (HistoryItemBar)HistoricalData[Count - 1, SeekOriginHistory.Begin];
            HistoryItemBar prev = (HistoryItemBar)HistoricalData[Count - 2, SeekOriginHistory.Begin];

            // VOLUME IMBALANCE RESOLUTION
            volImb.FindAll(item => true).ForEach(item =>
            {
                if (item.end == null && High() > item.begin.Close && Low() < item.begin.Close)
                {
                    if (args.Reason == UpdateReason.NewBar && 
                        Low() < item.begin.Close && 
                        Open() > item.begin.Close && 
                        Close() < Open())
                            SendAlert();
                    if (args.Reason == UpdateReason.NewBar && 
                        High() > item.begin.Close && 
                        Close() < item.begin.Close && 
                        Close() > Open())
                            SendAlert();
                    item.end = candle;
                }
            });

            // Manually calculate BB 20/2
            double maValue = this.ma.GetValue();
            double sum = 0.0;
            for (int i = 0; i < 20; i++)
                sum += Math.Pow(this.GetPrice(PriceType.Close, i) - maValue, 2);
            sum = 2 * Math.Sqrt(sum / 20);
            double ub = maValue + sum;
            double lb = maValue - sum;

            bool c0G = Close() > Open();
            bool c1G = Close(1) > Open(1);
            bool c2G = Close(2) > Open(2);
            bool c0R = Close() < Open();
            bool c1R = Close(1) < Open(1);
            bool c2R = Close(2) < Open(2);

            double c0Body = Math.Abs(Close() - Open());
            double c1Body = Math.Abs(Close(1) - Open(1));

            // VOLUME IMBALANCE
            if ((c0G && c1G && Open() > Close(1)) || (c0R && c1R && Open() < Close(1)))
            {
                Imbalance b = new Imbalance(prev, null);
                if (!volImb.Contains(b))
                    volImb.Add(b);
            }

            if ((Low() < lb || Low(1) < lb) &&
               (c0Body > c1Body) && (c1R && c0G) &&
               (Open() < Close(1) || Open() == Close(1)))
            {
                //Bars b = new Bars(candle, 1, Bars.HiLo.Long, Color.White);
                //if (!barslist.Contains(b))
                //    barslist.Add(b);
                SetBarColor(Color.White);
            }

            if ((High() > ub || High(1) > ub || High(2) > ub) &&
               (c0Body > c1Body) && (c1G && c0R) &&
               (Close() < Open(1) || Open() == Close(1)))
            {
                //Bars b = new Bars(candle, 1, Bars.HiLo.Short, Color.White);
                //if (!barslist.Contains(b))
                //    barslist.Add(b);
                SetBarColor(Color.White);
            }

            double rsi = Rsi.GetValue();
            double rsi1 = Rsi.GetValue(1);
            double rsi2 = Rsi.GetValue(2);

            // TRAMPOLINE
            if (c0R && c1R && Close() < Close(1) && (rsi >= 70 || rsi1 >= 70 || rsi2 >= 70) && c2G && High(2) >= ub)
            {
                Bars b = new Bars(candle, 1, Bars.HiLo.Short, Color.White);
                if (!barslist.Contains(b))
                    barslist.Add(b);
            }
            if (c0G && c1G && candle.Close > Close(1) && (rsi < 25 || rsi1 < 25 || rsi2 < 25) && c2R && Low(2) <= lb)
            {
                Bars b = new Bars(candle, 1, Bars.HiLo.Long, Color.White);
                if (!barslist.Contains(b))
                    barslist.Add(b);
            }

        }

    }

}