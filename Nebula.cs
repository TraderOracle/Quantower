// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;

namespace Indicator3
{
    public class Indicator3 : Indicator
    {
        [InputParameter("Sensitivity", 10, 1, 9999, 1, 0)]
        private const int UP = 1;
        private const int DOWN = 1;
        public int Sensitivity = 150;
        private int FastPeriod = 20;
        private int SlowPeriod = 40;
        private Color FastAboveCloudColor;
        private Color SlowAboveCloudColor;
        private int iTrend = 1;
        bool barGapUp;
        bool barGapDown;

        public Indicator3()
            : base()
        {
            this.Name = "Nebula";
            this.AddLineSeries("KAMA 9", Color.Black, 1, LineStyle.Solid);
            this.AddLineSeries("KAMA 21", Color.Black, 1, LineStyle.Solid);
            this.AddLineSeries("ATR", Color.Transparent, 1, LineStyle.Solid);

            this.FastAboveCloudColor = Color.FromArgb(88, Color.Green);
            this.SlowAboveCloudColor = Color.FromArgb(88, Color.Red);

            SeparateWindow = false;
        }

        private Indicator fastEma;
        private Indicator slowEma;
        private Indicator BB;
        private Indicator MACD;
        private Indicator AO;
        private Indicator SAR;
        private Indicator KAMA9;
        private Indicator KAMA21;
        private Indicator MD;
        private Indicator ATR;

        protected override void OnInit()
        {
            ATR = Core.Indicators.BuiltIn.ATR(14, MaMode.EMA);
            AddIndicator(ATR);

            MD = Core.Indicators.BuiltIn.MD(14, 2, PriceType.Close);
            AddIndicator(MD);

            KAMA9 = Core.Indicators.BuiltIn.KAMA(9, 2, 105, PriceType.Close);
            AddIndicator(KAMA9);
            KAMA21 = Core.Indicators.BuiltIn.KAMA(21, 2, 105, PriceType.Close);
            AddIndicator(KAMA21);

            SAR = Core.Indicators.BuiltIn.SAR(0.02, 0.2);
            AddIndicator(SAR);

            AO = Core.Indicators.BuiltIn.AO();
            AddIndicator(AO);

            BB = Core.Indicators.BuiltIn.BB(20, 2, PriceType.Close, MaMode.SMA);
            AddIndicator(BB);
            
            MACD = Core.Indicators.BuiltIn.MACD(12, 26, 7);
            AddIndicator(MACD);
            
            fastEma = Core.Indicators.BuiltIn.EMA(20, PriceType.Close);
            AddIndicator(fastEma);
            
            slowEma = Core.Indicators.BuiltIn.EMA(40, PriceType.Close);
            AddIndicator(slowEma);
        }

        public void VolumeAnalysisData_Loaded()
        {
            // Set value to all previous indicators points
            for (int i = 0; i < this.Count; i++)
            {
                SetValue(this.HistoricalData[i].VolumeAnalysisData.Total.AverageBuySize, 0, i);
                SetValue(this.HistoricalData[i].VolumeAnalysisData.Total.AverageSellSize, 1, i);
            }
        }

        protected override void OnClear()
        {

        }
/*
        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (this.CurrentChart == null)
                return;

            Graphics graphics = args.Graphics;
            StringFormat stringFormat = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };

            var mainWindow = this.CurrentChart.MainWindow;
            Font font = new Font("Arial", 16, FontStyle.Bold);
            Font fontS = new Font("Arial", 10, FontStyle.Bold);

            DateTime leftTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left);
            DateTime rightTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right);

            int leftIndex = (int)mainWindow.CoordinatesConverter.GetBarIndex(leftTime);
            int rightIndex = (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(rightTime));

            for (int i = leftIndex; i <= rightIndex; i++)
            {
                if (i > 0 && i < this.HistoricalData.Count && this.HistoricalData[i, SeekOriginHistory.Begin] is HistoryItemBar bar)
                {
                    HistoryItemBar bar1 = (HistoryItemBar)this.HistoricalData[i - 1, SeekOriginHistory.Begin];
                    HistoryItemBar bar2 = (HistoryItemBar)this.HistoricalData[i - 2];
                    HistoryItemBar bar3 = (HistoryItemBar)this.HistoricalData[i - 3];
                    HistoryItemBar bar4 = (HistoryItemBar)this.HistoricalData[i - 4];

                    var c0G = bar.Close > bar.Open;
                    var c1G = bar1.Close > bar1.Open;
                    var c2G = bar2.Close > bar2.Open;
                    var c3G = bar3.Close > bar3.Open;
                    var c4G = bar4.Close > bar4.Open;

                    var c0R = bar.Close < bar.Open;
                    var c1R = bar1.Close < bar1.Open;
                    var c2R = bar2.Close < bar2.Open;
                    var c3R = bar3.Close < bar3.Open;
                    var c4R = bar4.Close < bar4.Open;

                    bool barGapUp = (c0G && c1G && bar.Open > bar1.Close);
                    bool barGapDown = (c0R && c1R && bar.Open < bar1.Close);

                    bool bodyOverbody = ((c0G && c1G && bar.Open >= bar1.Close) ||
                        (c0G && c2G && bar.Open > bar2.Close) ||
                        (c0G && c3G && bar.Open > bar3.Close) ||
                        (c0G && c4G && bar.Open > bar4.Close));
                    bool bodyUnderbody = ((c0R && c1R && bar.Open <= bar1.Close) ||
                        (c0R && c2R && bar.Open <= bar2.Close) ||
                        (c0R && c3R && bar.Open <= bar3.Close) ||
                        (c0R && c4R && bar.Open <= bar4.Close));

                    int X = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar.TimeLeft) + this.CurrentChart.BarsWidth / 2.0);
                    int belowBarY = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(bar.Low) + 20);
                    int aboveBarY = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(bar.High) - 20);

                    if (bodyOverbody && bDownTrend)
                    {
                        graphics.DrawString("➀", fontS, Brushes.Lime, X, belowBarY, stringFormat);
                        bUpTrend = true;
                        bDownTrend = false;
                    }
                    else if (barGapUp)
                    {
                        graphics.DrawString("+", font, Brushes.Lime, X, belowBarY, stringFormat);
                        bUpTrend = true;
                        bDownTrend = false;
                    }

                    if (bodyUnderbody && bUpTrend)
                    {
                        graphics.DrawString("➀", fontS, Brushes.Red, X, aboveBarY, stringFormat);
                        bDownTrend = true;
                        bUpTrend = false;
                    }
                    else if (barGapDown)
                    {
                        graphics.DrawString("+", font, Brushes.Red, X, aboveBarY, stringFormat);
                        bDownTrend = true;
                        bUpTrend = false;
                    }

                    // "↓" : "↑"
                }
            }
        }
*/

        protected override void OnUpdate(UpdateArgs args)
        {
            var c0G = Close() > Open();
            var c1G = Close(1) > Open(1);
            var c2G = Close(2) > Open(2);
            var c3G = Close(3) > Open(3);
            var c4G = Close(4) > Open(4);

            var c0R = Close() < Open();
            var c1R = Close(1) < Open(1);
            var c2R = Close(2) < Open(2);
            var c3R = Close(3) < Open(3);
            var c4R = Close(4) < Open(4);

            if (c0G && c1G && Open() > Close(1))
            {
                barGapUp = true;
                barGapDown = false;
                //iTrend = UP;
            }
            else if (c0R && c1R && Open() < Close(1))
            {
                barGapDown = true;
                barGapUp = false;
                //iTrend = DOWN;  
            }

            bool bodyOverbody = ((c0G && c1G && Open() >= Close(1)) ||
                (c0G && c2G && Open() > Close(2)) ||
                (c0G && c3G && Open() > Close(3)) ||
                (c0G && c4G && Open() > Close(4)));

            bool bodyUnderbody = ((c0R && c1R && Open() <= Close(1)) ||
                (c0R && c2R && Open() <= Close(2)) ||
                (c0R && c3R && Open() <= Close(3)) ||
                (c0R && c4R && Open() <= Close(4)));

            var t1 = ((fastEma.GetValue(0) - slowEma.GetValue(0)) - (fastEma.GetValue(1) - slowEma.GetValue(1))) * Sensitivity;
            var md = MD.GetValue();
            var kama9 = KAMA9.GetValue();
            var kama21 = KAMA21.GetValue();
            var sar1 = SAR.GetValue(0, 0);
            var sar2 = SAR.GetValue(0, 1);
            var ao = AO.GetValue();
            var atr = ATR.GetValue();

            var median = (Low() + High()) / 2;
            var dUpperLevel = median + atr * 1;
            var dLowerLevel = median - atr * 1;

            this.SetValue(kama9, 0);
            this.SetValue(kama21, 1);
            this.SetValue(dLowerLevel, 2);

            var currFastValue = KAMA9.GetValue();
            var currSlowValue = KAMA21.GetValue();

            var prevFastValue = KAMA9.GetValue(1);
            var prevSlowValue = KAMA21.GetValue(1);

            var waddah = Math.Min(Math.Abs(t1) + 70, 255);
            var Wad = t1 > 0 ? Color.FromArgb(255, 0, (byte)waddah, 0) : Color.FromArgb(255, (byte)waddah, 0, 0);
            this.SetBarColor(Wad);

            var isCrossing = currFastValue > currSlowValue && prevFastValue < prevSlowValue ||
                      currFastValue < currSlowValue && prevFastValue > prevSlowValue;

            if (isCrossing)
            {
                this.EndCloud(0, 1, Color.Empty);
                if (currFastValue > currSlowValue)
                    this.BeginCloud(0, 1, this.FastAboveCloudColor);
                else if (currFastValue < currSlowValue)
                    this.BeginCloud(0, 1, this.SlowAboveCloudColor);
            }
/*
            if (barGapUp && c1R)
            {
                if (currFastValue > currSlowValue)
                    LinesSeries[2].SetMarker(0, new IndicatorLineMarker(Color.Lime, bottomIcon: IndicatorLineMarkerIconType.UpArrow));
                else
                    LinesSeries[2].SetMarker(0, new IndicatorLineMarker(Color.Lime, bottomIcon: IndicatorLineMarkerIconType.UpArrow));
                iTrend = UP;
            }
            else if (barGapDown && iTrend == UP)
            {
                if (currFastValue > currSlowValue)
                    LinesSeries[2].SetMarker(0, new IndicatorLineMarker(Color.Orange, bottomIcon: IndicatorLineMarkerIconType.UpArrow));
                else
                    LinesSeries[2].SetMarker(0, new IndicatorLineMarker(Color.Orange, bottomIcon: IndicatorLineMarkerIconType.UpArrow));
                iTrend = DOWN;
            }
*/
        }

    }
}
