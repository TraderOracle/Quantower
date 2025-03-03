using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace Oscillators;

public sealed class IndicatorMovingAverageConvergenceDivergence : Indicator, IWatchlistIndicator
{
    private Indicator fastSMA;
    private Indicator slowSMA;
    private Indicator signal;

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
        this.Name = "Linda MACD";

        this.AddLineSeries("MACD", Color.DodgerBlue, 1, LineStyle.Solid);
        this.AddLineSeries("HistogramPos", Color.Green, 10, LineStyle.Columns);
        this.AddLineSeries("HistogramNeg", Color.Red, 10, LineStyle.Columns);

        this.SeparateWindow = true;
    }

    protected override void OnInit()
    {
        this.fastSMA = Core.Indicators.BuiltIn.SMA(this.FastPeriod, PriceType.Typical);
        this.slowSMA = Core.Indicators.BuiltIn.SMA(this.SlowPeriod, PriceType.Typical);
        this.signal = Core.Indicators.BuiltIn.SMA(this.SignalPeriod, PriceType.Typical);

        this.AddIndicator(this.fastSMA);
        this.AddIndicator(this.slowSMA);
        this.AddIndicator(this.signal);
    }

    protected override void OnUpdate(UpdateArgs args)
    {
        if (this.Count < this.MaxEMAPeriod)
            return;

        double fast = this.fastSMA.GetValue();
        double slow = this.slowSMA.GetValue();
        double sig = this.signal.GetValue();

        double macdLine = fast - slow;
        double histogram = macdLine - sig;

        if (macdLine >= 0)
        {
            //this.SetValue(0);
            this.SetValue(macdLine, 1);
            this.SetValue(0, 2);
        }
        else
        {
            //this.SetValue(0);
            this.SetValue(0, 1);
            this.SetValue(Math.Abs(macdLine), 2);
        }
    }

}