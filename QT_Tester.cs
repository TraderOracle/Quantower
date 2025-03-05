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

public sealed class IndicatorOmnibus : Indicator, IWatchlistIndicator
{
    private Indicator fastSMA;
    private Indicator slowSMA;
    private Indicator signal;
    private Indicator Rsi;
    private Indicator ma;
    private Indicator BB;
    private Indicator SAR;
    private Indicator waddahFast;
    private Indicator waddahSlow;

    public List<Bars> barslist = new List<Bars>();
    public List<Imbalance> volImb = new List<Imbalance>();
    public String sNextAlert = string.Empty;

    [InputParameter("Play Alert Sounds")]
    public bool bAlerts = true;
    [InputParameter("Sound Directory")]
    public String sWavDir = @"C:\temp\sounds";

    public int MinHistoryDepths => this.MaxEMAPeriod + 16;
    private int MaxEMAPeriod => Math.Max(3, 9);

    public IndicatorOmnibus()
        : base()
    {
        this.Name = "Omnibus";
    }

    protected override void OnInit()
    {
        // LINDA MACD
        this.fastSMA = Core.Indicators.BuiltIn.SMA(3, PriceType.Typical);
        this.slowSMA = Core.Indicators.BuiltIn.SMA(9, PriceType.Typical);
        this.signal = Core.Indicators.BuiltIn.SMA(16, PriceType.Typical);
        this.ma = Core.Indicators.BuiltIn.MA(20, PriceType.Close, MaMode.SMA, Indicator.DEFAULT_CALCULATION_TYPE);
        this.AddIndicator(this.fastSMA);
        this.AddIndicator(this.slowSMA);
        this.AddIndicator(this.signal);
        this.AddIndicator(this.ma);

        // WADDAH EXPLOSION
        waddahFast = Core.Indicators.BuiltIn.SMA(20, PriceType.Close);
        waddahSlow = Core.Indicators.BuiltIn.SMA(40, PriceType.Close);
        AddIndicator(waddahFast);
        AddIndicator(waddahSlow);

        // REMAINDER
        this.Rsi = Core.Indicators.BuiltIn.RSI(14, PriceType.Close, RSIMode.Exponential, MaMode.EMA, 10);
        this.BB = Core.Indicators.BuiltIn.BB(20, 2, PriceType.Close, MaMode.EMA);
        this.SAR = Core.Indicators.BuiltIn.SAR(0.02, 0.2);
        this.AddIndicator(this.Rsi);
        this.AddIndicator(this.BB);
        this.AddIndicator(this.SAR);

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
                 
                Pen dashPen = new Pen(Color.FromArgb(255, 139, 188, 252), 2);
                dashPen.DashPattern = new float [] { 5, 1, 3, 1 };
                graphics.DrawLine(dashPen, xCoord, yy, iEnd, yy);
            });

        barslist.FindAll(item => true)
            .ForEach(item =>
            {
                double drawingPrice = item.setupDefault == Bars.HiLo.Long ? item.bar.Low : item.bar.High;

                int xCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(item.bar.TimeRight) - (CurrentChart.BarsWidth / 2));
                int yCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(drawingPrice));

                graphics.FillEllipse(new SolidBrush(item.color), xCoord - 5, yCoord - 12, 5, 5);
                //graphics.DrawString("TR", new Font("Arial", 11, FontStyle.Bold), new SolidBrush(Color.Yellow), xCoord - 10, yCoord);
            });
    }

    private void SendAlert(String sSound)
    {
        sNextAlert = string.Empty;
        try
        {
            System.Diagnostics.Process.Start("cmd.exe", "/c " + sWavDir + "\\" + sSound + ".wav");
        }
        catch { }
    }

    protected override void OnUpdate(UpdateArgs args)
    {
        if (this.Count < this.MaxEMAPeriod)
            return;

        if (args.Reason == UpdateReason.NewBar && sNextAlert != string.Empty)
            SendAlert(sNextAlert);

            if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.HistoricalBar)
        {
            HistoryItemBar candle = (HistoryItemBar)HistoricalData[Count - 1, SeekOriginHistory.Begin];
            HistoryItemBar prev = (HistoryItemBar)HistoricalData[Count - 2, SeekOriginHistory.Begin];

            // WADDAH EXPLOSION
            var fastMinusSlowCurr = waddahFast.GetValue(0) - waddahSlow.GetValue(0);
            var fastMinusSlowPrev = waddahFast.GetValue(1) - waddahSlow.GetValue(1);
            var fastMinusSlowMorePrev = waddahFast.GetValue(2, 0) - waddahSlow.GetValue(2, 0);
            var t1 = (fastMinusSlowCurr - fastMinusSlowPrev) * 150;
            var t1Prev = (fastMinusSlowPrev - fastMinusSlowMorePrev) * 150;
            var e1 = BB.GetValue(0, 0) - BB.GetValue(0, 2);
            var trendUp = t1 >= 0 ? t1 : 0;
            var trendUpPrev = t1Prev >= 0 ? t1Prev : 0;
            var trendDown = t1 < 0 ? (t1 * -1) : 0;
            var trendDownPrev = t1Prev < 0 ? (t1Prev * -1) : 0;
            bool bWaddahUp = t1 > 0;
            bool bWaddahDown = t1 <= 0;

            // LINDA MACD
            double fast = this.fastSMA.GetValue();
            double slow = this.slowSMA.GetValue();
            double sig = this.signal.GetValue();
            double macdLine = fast - slow;
            double histogram = macdLine - sig;

            //if (macdLine > 0 && bWaddahUp)
            //{
            //    Bars b = new Bars(candle, 1, Bars.HiLo.Long, Color.Lime);
            //    if (!barslist.Contains(b))
            //        barslist.Add(b);
            //}

            // VOLUME IMBALANCE RESOLUTION
            volImb.FindAll(item => true).ForEach(item =>
            {
                if (item.end == null && High() > item.begin.Close && Low() < item.begin.Close)
                {
                    if (args.Reason == UpdateReason.NewBar && 
                        Low() < item.begin.Close && 
                        Open() > item.begin.Close && 
                        Close() < Open())
                            sNextAlert = "volimbfill";
                    if (args.Reason == UpdateReason.NewBar && 
                        High() > item.begin.Close && 
                        Close() < item.begin.Close && 
                        Close() > Open())
                        sNextAlert = "volimbfill";
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
                sNextAlert = "volimb";
            }

            if ((Low() < lb || Low(1) < lb) &&
               (c0Body > c1Body) && (c1R && c0G) &&
               (Open() < Close(1) || Open() == Close(1)))
            {
                //Bars b = new Bars(candle, 1, Bars.HiLo.Long, Color.White);
                //if (!barslist.Contains(b))
                //    barslist.Add(b);
                SetBarColor(Color.White);
                sNextAlert = "engulf";
            }

            if ((High() > ub || High(1) > ub || High(2) > ub) &&
               (c0Body > c1Body) && (c1G && c0R) &&
               (Close() < Open(1) || Open() == Close(1)))
            {
                //Bars b = new Bars(candle, 1, Bars.HiLo.Short, Color.White);
                //if (!barslist.Contains(b))
                //    barslist.Add(b);
                SetBarColor(Color.White);
                sNextAlert = "engulf";
            }

            double rsi = Rsi.GetValue();
            double rsi1 = Rsi.GetValue(1);
            double rsi2 = Rsi.GetValue(2);

            // TRAMPOLINE
            if (c0R && c1R && Close() < Close(1) && (rsi >= 70 || rsi1 >= 70 || rsi2 >= 70) && c2G && High(2) >= ub)
            {
                //Bars b = new Bars(candle, 1, Bars.HiLo.Short, Color.White);
                //if (!barslist.Contains(b))
                //    barslist.Add(b);
            }
            if (c0G && c1G && candle.Close > Close(1) && (rsi < 25 || rsi1 < 25 || rsi2 < 25) && c2R && Low(2) <= lb)
            {
                //Bars b = new Bars(candle, 1, Bars.HiLo.Long, Color.White);
                //if (!barslist.Contains(b))
                //    barslist.Add(b);
            }

        }

    }

}