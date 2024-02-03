// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;
using System.Collections.Generic;

namespace Indicator3
{
    public class Indicator3 : Indicator
    {
        [InputParameter("Sensitivity", 10, 1, 9999, 1, 0)]
        public int Sensitivity = 150;
        private int FastPeriod = 20;
        private int SlowPeriod = 40;
        private Color FastAboveCloudColor;
        private Color SlowAboveCloudColor;

        public Indicator3()
            : base()
        {
            this.Name = "Nebula";
            this.AddLineSeries("KAMA 9", Color.Black, 1, LineStyle.Solid);
            this.AddLineSeries("KAMA 21", Color.Black, 1, LineStyle.Solid);

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

        protected override void OnInit()
        {
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

        protected override void OnClear()
        {

        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);

            if (this.CurrentChart == null)
                return;

            Graphics graphics = args.Graphics;

            // Use StringFormat class to center text
            StringFormat stringFormat = new StringFormat()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };

            var mainWindow = this.CurrentChart.MainWindow;
            Font font = new Font("Arial", 16, FontStyle.Bold);

            // Get left and right time from visible part or history
            DateTime leftTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left);
            DateTime rightTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right);

            // Convert left and right time to index of bar
            int leftIndex = (int)mainWindow.CoordinatesConverter.GetBarIndex(leftTime);
            int rightIndex = (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(rightTime));

            // Process only required (visible on the screen at the moment) range of bars
            for (int i = leftIndex; i <= rightIndex; i++)
            {
                if (i > 0 && i < this.HistoricalData.Count && this.HistoricalData[i, SeekOriginHistory.Begin] is HistoryItemBar bar)
                {
                    bool isBarGrowing = bar.Close > bar.Open;

                    // Calculate coordinates for drawing text. X - middle of the bar. Y - above High or below Low
                    int textXCoord = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar.TimeLeft) + this.CurrentChart.BarsWidth / 2.0);
                    int textYCoord = (int)Math.Round(isBarGrowing ? (mainWindow.CoordinatesConverter.GetChartY(bar.High) - 20) : (mainWindow.CoordinatesConverter.GetChartY(bar.Low) + 20));

                    graphics.DrawString(isBarGrowing ? "↓" : "↑" , font, isBarGrowing ? Brushes.Red : Brushes.Lime, textXCoord, textYCoord, stringFormat);
                }
            }
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            var c0G = Close() > Open();
            var c0R = Close() < Open();
            var c1G = Close(1) > Open(1);
            var c1R = Close(1) < Open(1);
            var c2G = Close(2) > Open(2);
            var c2R = Close(2) < Open(2);
            var c3G = Close(3) > Open(3);
            var c3R = Close(3) < Open(3);

            var t1 = ((fastEma.GetValue(0) - slowEma.GetValue(0)) - (fastEma.GetValue(1) - slowEma.GetValue(1))) * Sensitivity;
            var md = MD.GetValue();
            var kama9 = KAMA9.GetValue();
            var kama21 = KAMA21.GetValue();
            var sar1 = SAR.GetValue(0, 0);
            var sar2 = SAR.GetValue(0, 1);
            var ao = AO.GetValue();

            this.SetValue(kama9, 0);
            this.SetValue(kama21, 1);

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

        }

    }
}
