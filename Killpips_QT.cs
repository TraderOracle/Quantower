
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Numerics;
using TradingPlatform.BusinessLayer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;
using TradingPlatform.BusinessLayer.Utils;
using TradingPlatform.BusinessLayer.Chart;
using System.Drawing.Text;
using System.Linq;

namespace Killpips_QT
{
    public class Killpips_QT : Indicator
    {
        private bool bDrawn = false;
        private bool bProcessing = false;
        private const int LineLengthPixels = 200;
        private Color LineColor = Color.Red;

        [InputParameter("Values")]
        public string sym3 = "$NQ1!: vix r1, 22297, SD0, 21989, SD1, 21945, range daily min 21658";
        [InputParameter("Symbol 2")]
        public string sym2 = "$ES1!: vix r1, 6204, vix r2, SD0, 6142, SD1, 6132, range daily min, 6075";
        [InputParameter("Symbol 3")]
        public string sym1 = "$CL1!: vix r1, 76.04, vix r2, 76.14, SD1, 74.11, SD2, 73.58, range daily min, 72.54";
        [InputParameter("Symbol 4")]
        public string sym4 = "$GC1!: vix r1, 2788, vix r2, 2790, SD1, 2756, SD2, 2748, range daily min, 2731";
        [InputParameter("Symbol 5")]
        public string sym5 = "";
        [InputParameter("Symbol 6")]
        public string sym6 = "";
        [InputParameter("Symbol 7")]
        public string sym7 = "";
        [InputParameter("Symbol 8")]
        public string sym8 = "";
        [InputParameter("Symbol 9")]
        public string sym9 = "";

        [InputParameter("Font Size")]
        public int iFontSize = 8;
        [InputParameter("Font Color")]
        public Brush txtBrush = Brushes.White;
        [InputParameter("kvo1 / RD Color")]
        public Color ckvo1 = Color.CornflowerBlue;
        [InputParameter("kvo2 Color")]
        public Color ckvo2 = Color.Magenta;
        [InputParameter("vix Color")]
        public Color cvix = Color.Sienna;
        [InputParameter("max Color")]
        public Color cmax = Color.Red;
        [InputParameter("min Color")]
        public Color cmin = Color.Lime;
        [InputParameter("range Color")]
        public Color crange = Color.LightSteelBlue;
        [InputParameter("SD Color")]
        public Color cSD = Color.Brown;

        private struct shit
        {
            public string label;
            public double price;
            public Color color;
        }
        List<shit> lsL = new List<shit>();

        public Killpips_QT()
            : base()
        {
            Name = "Killpips";
            Description = "Killpips Support/Resistance Levels";
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            ParseSymbols();
            bDrawn = false;
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);
            if (CurrentChart == null || this.HistoricalData == null)
                return;

            if (bProcessing)
                return;

            Graphics gr = args.Graphics;
            
            IChartWindow mainWindow = CurrentChart.MainWindow;
            HistoryItemBar bar1 = new HistoryItemBar();

            for (int i = (int)mainWindow.CoordinatesConverter.GetBarIndex(mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left)); i <= (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right))); i++)
                if (i > 0 && i < this.HistoricalData.Count && this.HistoricalData[i, SeekOriginHistory.Begin] is HistoryItemBar bar)
                    bar1 = (HistoryItemBar)this.HistoricalData[i - 1, SeekOriginHistory.Begin];

            foreach (shit st in lsL)
                gr.DrawString(st.label, new Font("Arial", iFontSize), txtBrush, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2) + 100), (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(st.price)));

            //bDrawn = true;
        }

        private void ParseSymbols()
        {
            string symbol = "$" + this.Symbol.Name.ToUpper().Replace("/", "").Substring(0, 2);
            if (sym1.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym1.Split(':')[1].Trim());
            if (sym2.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym2.Split(':')[1].Trim());
            if (sym3.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym3.Split(':')[1].Trim());
            if (sym4.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym4.Split(':')[1].Trim());
            if (sym5.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym5.Split(':')[1].Trim());
            if (sym6.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym6.Split(':')[1].Trim());
            if (sym7.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym7.Split(':')[1].Trim());
            if (sym8.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym8.Split(':')[1].Trim());
            if (sym9.Split(':')[0].Trim().ToUpper().Substring(0, 3).Equals(symbol))
                FetchDataFromString(sym9.Split(':')[1].Trim());
        }

        private async void FetchDataFromString(string sLine)
        {
            if (bDrawn)
                return;

            try
            {
                int i = 0;
                string[] sb = sLine.Split(", ");
                string price = string.Empty, desc = string.Empty;
                foreach (string sr in sb)
                {
                    if (i % 2 != 0)
                        price = sr;
                    else
                        desc = sr;

                    if (!string.IsNullOrEmpty(price) && !string.IsNullOrEmpty(desc))
                    {
                        shit st = new shit();
                        st.price = Convert.ToDouble(price);
                        st.label = desc;
                        Color cl = Color.Gray;
                        if (desc.Contains("range") || desc.Contains("HV"))
                            cl = crange;
                        if (desc.ToLower().Contains("min") || desc.Contains("VAL"))
                            cl = cmin;
                        else if (desc.Contains("kvo1") || desc.Contains("RD"))
                            cl = ckvo1;
                        else if (desc.Contains("SD"))
                            cl = cSD;
                        else if (desc.Contains("kvo2") || desc.ToLower().Contains("support"))
                            cl = ckvo2;
                        else if (desc.ToLower().Contains("max") || desc.Contains("VAH"))
                            cl = cmax;
                        else if (desc.Contains("vix"))
                            cl = cvix;
                        st.color = cl;
                        lsL.Add(st);
                        //this.AddLineLevel(st.price, desc, cl, 1, LineStyle.Solid);
                        LineLevel ll = new LineLevel(st.price, desc, cl, 1, LineStyle.Solid);
                        if (!LinesLevels.Contains(ll))
                            this.AddLineLevel(ll);
                        price = string.Empty;
                        desc = string.Empty;
                    }
                    i++;
                }
                
            }
            catch (Exception ex)
            {
                //this.Log("Error fetching data from API: " + ex.Message);
            }

        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < 2)
                return;

            if (args.Reason == UpdateReason.NewBar)
            {
                bProcessing = true;
                ParseSymbols();
                bProcessing = false;
                bDrawn = true;
            }

        }
    }
}
