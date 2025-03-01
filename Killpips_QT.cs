
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using TradingPlatform.BusinessLayer;
using System.Diagnostics.Metrics;
using TradingPlatform.BusinessLayer.Utils;
using TradingPlatform.BusinessLayer.Chart;
using System.Drawing.Text;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Killpips_QT
{
    public class Killpips_QT : Indicator
    {
        private bool bDrawn = false;
        private bool bProcessing = false;
        private const int LineLengthPixels = 200;
        private Color LineColor = Color.Red;

        [InputParameter("Values")]
        public string sym1 = "$NQ1!: vix r1, 20897, vix r2, 20917, vix s1, 20314, vix s2, 20297, 1DexpMAX, 20925, 1DexpMIN, 20286, RD0, 20662, RD1, 20715, RD2, 20821, SD0, 20551, SD1, 20499, SD2, 20391, HV, 20608, VAH, 20991, VAL, 20178, range daily max, 21036, range daily min, 20178   setting 86k - 727k";
        [InputParameter("Symbol 2")]
        public string sym2 = "$ES1!: vix r1, 5932, vix r2, 5935, vix s1, 5820, vix s2, 5817, 1DexpMAX, 5938, 1DexpMIN, 5814, RD0, 5887, RD1, 5897, RD2, 5917, SD0, 5865, SD1, 5856, SD2, 5835, HV, 5876, VAH, 5950, VAL, 5802, range daily max, 5959, range daily min, 5793   setting 237k - 2M";
        [InputParameter("Symbol 3")]
        public string sym3 = "";
        [InputParameter("Symbol 4")]
        public string sym4 = "";

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
        public Color cSD = Color.Red;
        [InputParameter("RD Color")]
        public Color cRD = Color.Lime;

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

            SolidBrush semiTransparentRedBrush = new SolidBrush(Color.FromArgb(108, 255, 0, 0));

            foreach (shit st in lsL)
            {
                int iY = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(st.price));
                int iX = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2) + 100);
                gr.DrawString(st.label, new Font("Arial", iFontSize), txtBrush, iX, iY);
            }

            //bDrawn = true;
        }

        private void ParseSymbols()
        {
            string symbol = "$" + this.Symbol.Name.Replace("/", "").ToUpper().Trim() + "1!";
            if (sym1.Split(':')[0].Trim().ToUpper().Equals(symbol))
                FetchDataFromString(sym1.Split(':')[1].Trim());
            if (sym2.Split(':')[0].Trim().ToUpper().Equals(symbol))
                FetchDataFromString(sym2.Split(':')[1].Trim());
            if (sym3.Split(':')[0].Trim().ToUpper().Equals(symbol))
                FetchDataFromString(sym3.Split(':')[1].Trim());
            if (sym4.Split(':')[0].Trim().ToUpper().Equals(symbol))
                FetchDataFromString(sym4.Split(':')[1].Trim());
        }

        private async void FetchDataFromString(string sLine)
        {
            if (bDrawn)
                return;

            if (sLine.Contains("setting"))
            {
                int ix = sLine.IndexOf("setting");
                sLine = sLine.Substring(0, ix - 1).Trim();
            }

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
                        else if (desc.Contains("kvo1"))
                            cl = ckvo1;
                        else if (desc.Contains("SD"))
                            cl = cSD;
                        else if (desc.Contains("RD"))
                            cl = cRD;
                        else if (desc.Contains("kvo2") || desc.ToLower().Contains("support"))
                            cl = ckvo2;
                        else if (desc.ToLower().Contains("max") || desc.Contains("VAH"))
                            cl = cmax;
                        else if (desc.Contains("vix"))
                            cl = cvix;
                        st.color = cl;
                        lsL.Add(st);
                        int iWidth = desc.Contains("HV") ? 3 : 1;
                        LineLevel ll = new LineLevel(st.price, desc, cl, iWidth, LineStyle.DashDot);
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
