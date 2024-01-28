// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace Indicator3
{
    public class Indicator3 : Indicator
    {
        [InputParameter("Sensitivity", 10, 1, 9999, 1, 0)]
        public int Sensitivity = 150;

        [InputParameter("BB Channel Length", 10, 1, 9999, 1, 0)]
        public int BBChanLength = 20;

        [InputParameter("Dead zone", 10, 1, 9999, 1, 0)]
        public int DeadZone = 20;

        [InputParameter("BB StdDev Multiplier", 10, 1, 9999, 1, 0)]
        public double BBMult = 2.0;

        [InputParameter("Fast SMA period", 20, 1, 9999, 1, 0)]
        public int FastPeriod = 20;

        [InputParameter("Slow SMA period", 40, 1, 9999, 1, 0)]
        public int SlowPeriod = 40;

        private Indicator fastSma;
        private Indicator slowSma;
        private Indicator bbSma;

        public Indicator3()
            : base()
        {
            this.Name = "Waddah Explosion";

            this.AddLineSeries("Fast EMA line", Color.Red, 16, LineStyle.Histogramm);
            this.AddLineSeries("Slow EMA line", Color.Green, 16, LineStyle.Histogramm);
            this.AddLineSeries("E1", Color.White, 1, LineStyle.Solid);

            var FastAboveCloudColor = Color.Lime; //  Color.FromArgb(127, Color.Lime);
            var SlowAboveCloudColor = Color.Red; // Color.FromArgb(127, Color.Red);

            SeparateWindow = false;
        }

        Indicator BB;
        Indicator MACD;
        Indicator fastSMA;
        Indicator slowSMA;

        protected override void OnInit()
        {
            BB = Core.Indicators.BuiltIn.BB(this.BBChanLength, this.BBMult, PriceType.Close, MaMode.SMA);
            AddIndicator(BB);
            MACD = Core.Indicators.BuiltIn.MACD(20, 40, 9);
            AddIndicator(MACD);
            fastSma = Core.Indicators.BuiltIn.SMA(20, PriceType.Close);
            AddIndicator(fastSma);
            slowSma = Core.Indicators.BuiltIn.SMA(40, PriceType.Close);
            AddIndicator(slowSma);
        }

        protected override void OnClear()
        {

        }

        protected override void OnUpdate(UpdateArgs args)
        {
            // Calculate MACD
            var fastMinusSlowCurr = fastSma.GetValue(0) - slowSma.GetValue(0);
            var fastMinusSlowPrev = fastSma.GetValue(1) - slowSma.GetValue(1);
            var fastMinusSlowMorePrev = fastSma.GetValue(2, 0) - slowSma.GetValue(2, 0);

            var t1 = (fastMinusSlowCurr - fastMinusSlowPrev) * Sensitivity;
            var t1Prev = (fastMinusSlowPrev - fastMinusSlowMorePrev) * Sensitivity;

            // Calculate BB
            var e1 = BB.GetValue(0, 0) - BB.GetValue(0, 2); // Upper = index 0, Lower = index 2

            var trendUp = t1 >= 0 ? t1 : 0;
            var trendUpPrev = t1Prev >= 0 ? t1Prev : 0;

            var trendDown = t1 < 0 ? (t1 * -1) : 0;
            var trendDownPrev = t1Prev < 0 ? (t1Prev * -1) : 0;

/*
            if (trendDown < trendDownPrev)
                this.LinesSeries[0].Color = Color.Red;
            else
                this.LinesSeries[0].Color = Color.Red;

            if (trendUp < trendUpPrev)
                this.LinesSeries[1].Color = Color.Green;
            else
                this.LinesSeries[1].Color = Color.Lime;
*/

            //this.SetValue(trendDown, 0);
            //this.SetValue(trendUp, 1);
            //this.SetValue(e1, 2);

            // get current values (offset is '0' by default)
            var currFastValue = this.fastSma.GetValue();
            var currSlowValue = this.slowSma.GetValue();

            // get previous values (offset is '1')
            var prevFastValue = this.fastSma.GetValue(1);
            var prevSlowValue = this.slowSma.GetValue(1);

            var Pick = Color.White;

            int Opac = 0;
            double Intensity = Math.Abs(t1) * 1.3;
            if (Intensity > 255)
                Opac = 255;
            else if (Intensity < 0)
                Opac = 20;
            else
                Opac = (int)Intensity;

            try
            {
                if (trendUp > 0)
                    Pick = Color.FromArgb(Opac, Color.Lime);
                else
                    Pick = Color.FromArgb(Opac, Color.Red);
            }
            catch (Exception x)
            {
                Pick = Color.FromArgb(0, Color.Red);
            }

            //this.SetBarColor(Pick);

        }

        public int LinearInterp(int start, int end, double percentage) => start + (int)Math.Round(percentage * (end - start));
        public Color ColorInterp(Color start, Color end, double percentage) =>
            Color.FromArgb(LinearInterp(start.A, end.A, percentage),
                           LinearInterp(start.R, end.R, percentage),
                           LinearInterp(start.G, end.G, percentage),
                           LinearInterp(start.B, end.B, percentage));
        public Color GradientPick(double percentage, Color Start, Color Center, Color End)
        {
            if (percentage < 0.5)
                return ColorInterp(Start, Center, percentage / 0.5);
            else if (percentage == 0.5)
                return Center;
            else
                return ColorInterp(Center, End, (percentage - 0.5) / 0.5);
        }

    }
}
