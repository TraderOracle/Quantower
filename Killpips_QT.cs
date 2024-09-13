
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

namespace Killpips_QT
{
    public class Killpips_QT : Indicator
    {
        private bool bDrawn = false;
        private bool bProcessing = false;
        private const int LineLengthPixels = 200;
        private Color LineColor = Color.Red;

        [InputParameter("Symbol 1")]
        public string sym3 = "$NQ1!: vix r1, 19091, vix r2, 19107, vix s1, 18510, vix s2, 18494, range k max, 19272, range k+50%, 19068, range k 0, 18865, range k-50%, 18659, range k min, 18457, kvo, 19170, kvo, 18966, kvo, 18763, kvo, 18557, kvo, 18842";
        [InputParameter("Symbol 2")]
        public string sym2 = "$ES1!: vix r1, 5545, vix r2, 5549, vix s1, 5439, vix s2, 5435, range k max, 5587, range k+50%, 5545, range k 0, 5503, range k-50%, 5462, range k min, 5420, kvo, 5466, kvo, 5524, kvo, 5482, kvo, 5441, kvo, 5452";
        [InputParameter("Symbol 3")]
        public string sym1 = "$YM1!: vix r1, 41499, vix r2, 41533, vix s1, 40636, vix s2, 40602, range k max, 41732, range k+50%, 41442, range k 0, 41144, range k-50%, 40854, range k min, 40560, kvo, 41587, kvo, 41297, kvo, 40999, kvo, 40710, kvo, 40879, kvo, 40602, kvo, 41126, BL 10, 39916.78, BL 5, 40367.97, BL 6, 40482.85, BL 3, 40588.06, BL 8, 40723.85, BL 9, 40823.96, BL 2, 41204.24, BL 1, 41437.22, BL 7, 41509.21, BL 4, 41804.94";
        [InputParameter("Symbol 4")]
        public string sym4 = "$GC1!: vix r1, 2566.7, vix r2, 2569, vix s1, 2518, vix s2, 2515.9, range k max, 2574.3, range k+50%, 2558.8, range k 0, 2543, range k-50%, 2527.6, range k min, 2512, kvo, 2566, kvo, 2550, kvo, 2535, kvo, 2519, kvo, 2530, kvo, 2549.2";
        [InputParameter("Symbol 5")]
        public string sym5 = "$CL1!: vix r1, 68.05, vix r2, 68.20, vix s1, 64.39, vix s2, 64.25, range k max, 68.47, range k+50%, 67.11, range k 0, 65.75, range k-50%, 64.38, range k min, 63.02, kvo, 67.70, kvo, 66.48, kvo, 64.35, kvo, 63.79, kvo, 67.66";
        [InputParameter("Symbol 6")]
        public string sym6 = "$SPX: vix r1, 5541, vix r2, 5545, vix s1, 5435, vix s2, 5431, range k max, 5583, range k+50%, 5541, range k 0, 5499, range k-50%, 5458, range k min, 5416, kvo, 5462, kvo, 5520, kvo, 5478, kvo, 5437, kvo, 5448";
        [InputParameter("Symbol 7")]
        public string sym7 = "$RTY1!: vix r1, 2117.1, vix r2, 2118.9, vix s1, 2076.9, vix s2, 2075.1, range k max, 2144.8, range k+50%, 2123.1, range k 0, 2101.1, range k-50%, 2079.2, range k min, 2057.4, kvo, 2133.8, kvo, 2111, kvo, 2090.3, kvo, 2068, kvo, 2076.4, kvo, 2124.1";
        [InputParameter("Symbol 8")]
        public string sym8 = "$NG1!: vix r1, 2.254, vix r2, 2.256, vix s1, 2.211, vix s2, 2.209, range k max, 2.357, range k+50%, 2.294, range k 0, 2.232, range k-50%, 2.169, range k min, 2.107, kvo, 2.325, kvo, 2.262, kvo, 2.200, kvo, 2.137, kvo, 2.280";
        [InputParameter("Symbol 9")]
        public string sym9 = "$NDX: vix r1, 19085, vix r2, 19101, vix s1, 18504, vix s2, 18388, range k max, 19266, range k+50%, 19062, range k 0, 18859, range k-50%, 18653, range k min, 18451, kvo, 19164, kvo, 18960, kvo, 18757, kvo, 18551, kvo, 18836";

        [InputParameter("Font Size")]
        public int iFontSize = 8;
        [InputParameter("Font Color")]
        public Brush txtBrush = Brushes.White;
        [InputParameter("kvo1 Color")]
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
        [InputParameter("BL Color")]
        public Color cBL = Color.Brown;

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
            Description = "Killpips FREE Support/Resistance Levels";
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
                gr.DrawString(st.label, new Font("Arial", iFontSize), txtBrush, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2)), (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(st.price)));

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
                        if (desc.Contains("range") || desc.Contains("HVL"))
                            cl = crange;
                        if (desc.Contains("min") || desc.Contains("Min"))
                            cl = cmin;
                        else if (desc.Contains("kvo1") || desc.Contains("GEX"))
                            cl = ckvo1;
                        else if (desc.Contains("BL"))
                            cl = cBL;
                        else if (desc.Contains("kvo2") || desc.Contains("Support"))
                            cl = ckvo2;
                        else if (desc.Contains("max") || desc.Contains("Max"))
                            cl = cmax;
                        else if (desc.Contains("vix"))
                            cl = cvix;
                        st.color = cl;
                        lsL.Add(st);
                        this.AddLineLevel(st.price, desc, cl, 1, LineStyle.Solid);
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
