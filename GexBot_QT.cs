#region Using declarations
using System;
using System.Timers;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using TradingPlatform.BusinessLayer;
using Newtonsoft.Json.Linq;
using TradingPlatform.BusinessLayer.Chart;
using Font = System.Drawing.Font;
using Timer = System.Threading.Timer;
#endregion

namespace GexBot_QT
{
	public class GexBot_QT : Indicator
    {
        private string sVersion = "version 1.1";

        #region InputParameter

        [InputParameter("API Key")] 
        public string APIKey = "Input Key Here";

        [InputParameter("Symbol", 0, variants: new object[] {
                    "SPX","SPX",
                    "ES","ES_SPX",
                    "NDX","NDX",
                    "NQ","NQ_NDX",
                    "QQQ","QQQ",
                    "TQQQ","TQQQ",
                    "AAPL","AAPL",
                    "TSLA","TSLA",
                    "MSFT","MSFT",
                    "AMZN","AMZN",
                    "NVDA","NVDA",
                    "META","META",
                    "VIX","VIX",
                    "GOOG","GOOG",
                    "IWM","IWM",
                    "TLT","TLT",
                    "GLD","GLD",
                    "USO","USO" })]
        public string ticker = "NQ_NDX";

        [InputParameter("Pull Type", 0, variants: new object[] { "full", "full", "zero", "zero" })]
        public string nextFull = "full";

        [InputParameter("Classic or State", 0, variants: new object[] { "classic", "classic", "state", "state" })]
        public string sState = "classic";

        [InputParameter("Greek", 0, variants: new object[] {  
        "none", "none",
        "delta 0dte", "delta",
        "gamma 0dte", "gamma",
        "charm 0dte", "charm",
        "vanna 0dte", "vanna",
        "delta 1dte", "onedelta",
        "gamma 1dte", "onegamma",
        "charm 1dte", "onecharm",
        "vanna 1dte", "onevanna" })]
        public string Greek = "none";

        [InputParameter("Standard Dot Size")]
        public int DotSize = 4;

        [InputParameter("Greek Dot Size")]
        public int GreekDotSize = 7;

        [InputParameter("Width Scale")]
        public double WidthScale = 100;

        [InputParameter("Positive Vol Line Color")]
        public Brush posVol = Brushes.Lime;

        [InputParameter("Positive Vol Line Width")]
        public int posW = 1;

        [InputParameter("Negative Vol Line Color")]
        public Brush negVol = Brushes.Orange;

        [InputParameter("Negative Vol Line Width")]
        public int negW = 1;

        [InputParameter("Conversion Ratio")]
        public double convFactor = 1;

        [InputParameter("Show Gex Maj/Min Level Lines")]
        public bool bShowMajorLines = false;

        [InputParameter("Line Type", 0, variants: new object[] { "Solid", "Solid", "Dash", "Dash", "Dot", "Dot" })]
        public string sLineType = "Dash";

        [InputParameter("Show Status Text")]
        public bool bShowStatusText = true;

        [InputParameter("Text Y Position")]
        public int iTextPos = 900;

        #endregion

        #region Configs

        private struct lines
        {
            public double volume;
            public double oi;
            public double price;
            public double call;
            public double put;
        }
        List<lines> ll = new List<lines>();

        public struct dots
        {
            public double volume;
            public double price;
            public int i;
        }
        List<dots> ld = new List<dots>();

        private bool bProcessing = false;

        private string VolGex = "";
        private string Vol0Gamma = "";
        private string VolMajPos = "";
        private string VolMinNeg = "";
        private string DeltaReversal = "";
        private string Spot = "";
        private string OIGex = "";
        private string OIMajPos = "";
        private string OIMinNeg = "";

        #endregion

        public GexBot_QT()
            : base()
        {
            Name = "GexBot";
            Description = "GexBot";
            SeparateWindow = false;
        }

        #region Fetch Data

        private async void FetchData()
        {
            int idx = 0;
            string symbol = Symbol.Name.Replace("/", "").Substring(0, 2);
            if (symbol.Equals("NQ"))
                symbol = "NQ_NDX";
            else if (symbol.Equals("ES"))
                symbol = "ES_SPX";
            List<lines> llT = new List<lines>();
            List<dots> ldT = new List<dots>();

            if (!Greek.Equals("none"))
            {
                sState = "state";
                nextFull = Greek;
            }
                
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string urls = "https://api.gexbot.com/" + symbol + "/" + sState + "/" + nextFull + "?key=" + APIKey;
                    var jsonResponse = await client.GetStringAsync(urls);
                    JObject jo = JObject.Parse(jsonResponse);

                    string sSection = "strikes";
                    if (Greek.Equals("none"))
                    {
                        VolGex = jo["sum_gex_vol"].Value<string>();
                        OIGex = jo["sum_gex_oi"].Value<string>();
                        DeltaReversal = jo["delta_risk_reversal"].Value<string>();
                        Spot = jo["spot"].Value<string>();
                        Vol0Gamma = jo["zero_gamma"].Value<string>();
                        VolMajPos = jo["major_pos_vol"].Value<string>();
                        OIMajPos = jo["major_pos_oi"].Value<string>();
                        VolMinNeg = jo["major_neg_vol"].Value<string>();
                        OIMinNeg = jo["major_neg_oi"].Value<string>();
                    }
                    else
                    {
                        sSection = "mini_contracts";
                        VolMajPos = jo["major_positive"].Value<string>();
                        VolMinNeg = jo["major_negative"].Value<string>();
                    }

                    var clientarray = jo[sSection].Value<JArray>();

                    foreach (JArray item in clientarray)
                    {
                        if (Greek.Equals("none"))
                        {
                            double price = item[0].ToObject<Double>();
                            double volume = item[1].ToObject<Double>();
                            double oi = item[2].ToObject<Double>();
                            lines line = new lines();
                            line.price = price * convFactor;
                            line.volume = volume;
                            line.oi = oi;
                            llT.Add(line);
                            var xxx = item[3].Value<JArray>();
                            int i = 1;
                            foreach (Double qqq in xxx)
                            {
                                dots dotz = new dots();
                                dotz.price = price;
                                dotz.volume = qqq;
                                dotz.i = i;
                                ldT.Add(dotz);
                                i++;
                            }
                            idx++;
                        }
                        else
                        {
                            double price = item[0].ToObject<Double>();
                            double call = item[1].ToObject<Double>();
                            double put = item[2].ToObject<Double>();
                            double sgreek = item[3].ToObject<Double>();
                            lines line = new lines();
                            line.price = price * convFactor;
                            line.volume = sgreek;
                            line.call = call;
                            line.put = put;
                            llT.Add(line);
                            idx++;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            bProcessing = true;
            ll = llT;
            ld = ldT;
            bProcessing = false;
        }

        #endregion

        #region Timed Event / Render

        private Timer timer;

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);
            if (CurrentChart == null || this.HistoricalData == null || bProcessing)
                return;

            if (VolGex.Equals(""))
                return;

            Graphics gr = args.Graphics;
            IChartWindow mainWindow = CurrentChart.MainWindow;
            HistoryItemBar bar1 = new HistoryItemBar();

            for (int i = (int)mainWindow.CoordinatesConverter.GetBarIndex(mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left)); i <= (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right))); i++)
                if (i > 0 && i < this.HistoricalData.Count && this.HistoricalData[i, SeekOriginHistory.Begin] is HistoryItemBar bar)
                    bar1 = (HistoryItemBar)this.HistoricalData[i - 1, SeekOriginHistory.Begin];

            // Get left and right time from visible part or history
            DateTime leftTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left);
            DateTime rightTime = mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right);

            // Convert left and right time to index of bar
            int leftIndex = (int)mainWindow.CoordinatesConverter.GetBarIndex(leftTime);
            int rightIndex = (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(rightTime));

            Pen customPen = new Pen(Color.Orange, 2);
            if (sLineType.Equals("Solid"))
                customPen.DashPattern = new float[] { 1 };
            else if (sLineType.Equals("Dash"))
                customPen.DashPattern = new float[] { 3, 1 };
            if (sLineType.Equals("Dot"))
                customPen.DashPattern = new float[] { 1, 1 };

            foreach (lines l in ll)
            {
                double finalVol = l.volume;

                if (!Greek.Equals("none"))
                {
                    double dCall = Math.Abs(l.call);
                    dCall = dCall * CurrentChart.MainWindow.ClientRectangle.Width;
                    double dPut = Math.Abs(l.put);
                    dPut = dPut * CurrentChart.MainWindow.ClientRectangle.Width;
                    gr.FillEllipse(Brushes.Lime, (float)dCall, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)), GreekDotSize, GreekDotSize);
                    gr.FillEllipse(Brushes.OrangeRed, (float)dPut, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)), GreekDotSize, GreekDotSize);
                }

                if (finalVol < 0)
                {
                    finalVol = Math.Abs(finalVol);
                    finalVol = (finalVol * CurrentChart.MainWindow.ClientRectangle.Width) * (WidthScale / 100);

                    //gr.FillRectangle(Brushes.Orange,
                    //    (int)CurrentChart.MainWindow.ClientRectangle.Left,
                    //    (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)), (float)finalVol, negW);
                    customPen.Color = Color.Orange;
                    args.Graphics.DrawLine(customPen, (int)CurrentChart.MainWindow.ClientRectangle.Left,
                    (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)), (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)));
                }
                else
                {
                    finalVol = (finalVol * CurrentChart.MainWindow.ClientRectangle.Width) * (WidthScale / 100);

                    customPen.Color = Color.LimeGreen;
                    args.Graphics.DrawLine(customPen, (int)CurrentChart.MainWindow.ClientRectangle.Left,
(int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)), (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)));

                    //gr.FillRectangle(Brushes.LimeGreen,
                    //    (int)CurrentChart.MainWindow.ClientRectangle.Left,
                    //    (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)), (float)finalVol, posW);
                }
            }

            if (Greek.Equals("none"))
                foreach (dots d in ld)
                {
                    double finalVol = d.volume;
                    if (finalVol < 0)
                    {
                        finalVol = finalVol * -1;
                        finalVol = (finalVol * CurrentChart.MainWindow.ClientRectangle.Width) * (WidthScale / 100);
                        switch (d.i)
                        {
                            case 1:
                                gr.FillEllipse(Brushes.Red, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                            case 2:
                                gr.FillEllipse(Brushes.Lime, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                            case 3:
                                gr.FillEllipse(Brushes.Green, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                            case 4:
                                gr.FillEllipse(Brushes.DarkGreen, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                            case 5:
                                gr.FillEllipse(Brushes.White, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                        }
                        //gr.FillEllipse(Brushes.Orange,(float)finalVol,(int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), 4, 4);
                    }
                    else
                    {
                        finalVol = (finalVol * CurrentChart.MainWindow.ClientRectangle.Width) * (WidthScale / 100);
                        switch (d.i)
                        {
                            case 1:
                                gr.FillEllipse(Brushes.Red, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                            case 2:
                                gr.FillEllipse(Brushes.Lime, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                            case 3:
                                gr.FillEllipse(Brushes.Green, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                            case 4:
                                gr.FillEllipse(Brushes.DarkGreen, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                            case 5:
                                gr.FillEllipse(Brushes.White, (float)finalVol, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), DotSize, DotSize);
                                break;
                        }
                        //gr.FillEllipse(Brushes.Lime,(float)finalVol,(int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(d.price)), 4, 4);
                    }
                }

            if (bShowStatusText)
            {
                double n1 = Convert.ToDouble(VolGex);
                if (VolGex.Contains("-"))
                    args.Graphics.DrawString($"{sState} Net GEX: {n1.Format(2)}", new Font("Arial", 12, FontStyle.Bold), Brushes.Orange, iTextPos, 90);
                else
                    args.Graphics.DrawString($"{sState} Net GEX: {n1.Format(2)}", new Font("Arial", 12, FontStyle.Bold), Brushes.Lime, iTextPos, 90);

                args.Graphics.DrawString($"Major Pos: {VolMajPos}", new Font("Arial", 10, FontStyle.Regular), Brushes.LightSteelBlue, iTextPos, 110);
                args.Graphics.DrawString($"Major Neg: {VolMinNeg}", new Font("Arial", 10, FontStyle.Regular), Brushes.LightSteelBlue, iTextPos, 130);
                args.Graphics.DrawString($"Zero Gamma: {Vol0Gamma}", new Font("Arial", 10, FontStyle.Regular), Brushes.PaleTurquoise, iTextPos, 150);
                args.Graphics.DrawString($"Delta Reversal: {DeltaReversal}", new Font("Arial", 10, FontStyle.Regular), Brushes.MistyRose, iTextPos, 170);
            }

            if (bShowMajorLines)
            {
                double dSpot = Convert.ToDouble(Spot);
                double dVolMajPos = Convert.ToDouble(VolMajPos);
                double dVolMinNeg = Convert.ToDouble(VolMinNeg);

                customPen.Color = Color.White;
                args.Graphics.DrawLine(customPen, (int)CurrentChart.MainWindow.ClientRectangle.Left,
                    (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(dSpot)), 200000, posW);
                gr.DrawString("Spot Gex", new Font("Arial", 9), Brushes.White,(int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2) + 100), (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(dSpot)));

                customPen.Color = Color.Lime;
                args.Graphics.DrawLine(customPen, (int)CurrentChart.MainWindow.ClientRectangle.Left,
                    (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(dVolMajPos)), 200000, posW);
                gr.DrawString("Major Positive", new Font("Arial", 9), Brushes.Lime,(int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2) + 100),(int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(dVolMajPos)));

                customPen.Color = Color.Orange;
                args.Graphics.DrawLine(customPen, (int)CurrentChart.MainWindow.ClientRectangle.Left,(int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(dVolMinNeg)), 200000, posW);

                gr.DrawString("Minor Negative", new Font("Arial", 9), Brushes.Orange,(int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2) + 100),(int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(dVolMinNeg)));
            }

            // gr.FillRectangle(Brushes.Yellow, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight.AddHours(-1)) - (CurrentChart.BarsWidth / 2)), (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(rd.Next(18800, 19000))), rd.Next(4000, 4200), 4);
            //       gr.FillRectangle(Brushes.Yellow,
            //(int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeLeft.AddHours(-1)) - (CurrentChart.BarsWidth / 2)),
            //(int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(l.price)), 400, 1);
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            FetchData();
        }

        #endregion

        protected override void OnInit()
        {
            FetchData();
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if(args.Reason == UpdateReason.NewBar)
            {
                ld.Clear();
                ll.Clear();
                FetchData();
            }

        }
    }
}
