
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
using System.Diagnostics;

namespace Killpips_QT
{
    public class Killpips_QT : Indicator
    {
        private bool bDrawn = false;
        private bool bProcessing = false;
        private double iR0 = 0;
        private double iR1 = 0;
        private double iR2 = 0;
        private double iS0 = 0;
        private double iS1 = 0;
        private double iS2 = 0;
        private double ivixS1 = 0;
        private double ivixR1 = 0;

        [InputParameter("Values")]
        public string sym1 = "$NQ1!: vix r1, 20897, vix r2, 20917, vix s1, 20314, vix s2, 20297, 1DexpMAX, 20925, 1DexpMIN, 20286, RD0, 20662, RD1, 20715, RD2, 20821, SD0, 20551, SD1, 20499, SD2, 20391, HV, 20608, VAH, 20991, VAL, 20178, range daily max, 21036, range daily min, 20178   setting 86k - 727k";
        [InputParameter("Symbol 2")]
        public string sym2 = "$ES1!: vix r1, 5932, vix r2, 5935, vix s1, 5820, vix s2, 5817, 1DexpMAX, 5938, 1DexpMIN, 5814, RD0, 5887, RD1, 5897, RD2, 5917, SD0, 5865, SD1, 5856, SD2, 5835, HV, 5876, VAH, 5950, VAL, 5802, range daily max, 5959, range daily min, 5793   setting 237k - 2M";
        [InputParameter("Symbol 3")]
        public string sym3 = "";
        [InputParameter("Symbol 4")]
        public string sym4 = "";

        //[InputParameter("Font Size")]
        //public int iFontSize = 8;
        //[InputParameter("Font Color")]
        //public Brush txtBrush = Brushes.White;
        //[InputParameter("kvo1 / RD Color")]
        //public Color ckvo1 = Color.CornflowerBlue;
        //[InputParameter("kvo2 Color")]
        //public Color ckvo2 = Color.LawnGreen;
        //[InputParameter("vix Color")]
        //public Color cvix = Color.Sienna;
        //[InputParameter("max Color")]
        //public Color cmax = Color.OrangeRed;
        //[InputParameter("min Color")]
        //public Color cmin = Color.Lime;
        //[InputParameter("HV / range Color")]
        //public Color crange = Color.Purple;
        //[InputParameter("SD Color")]
        //public Color cSD = Color.OrangeRed;
        //[InputParameter("RD Color")]
        //public Color cRD = Color.LawnGreen;

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
            Name = "Killpips Zones";
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

            SolidBrush redSmall = new SolidBrush(Color.FromArgb(84, 94, 15, 9));
            SolidBrush redMed = new SolidBrush(Color.FromArgb(71, 166, 22, 12));
            SolidBrush redLarge = new SolidBrush(Color.FromArgb(82, 176, 74, 19));
            SolidBrush greenSmall = new SolidBrush(Color.FromArgb(98, 9, 92, 35));
            SolidBrush greenMed = new SolidBrush(Color.FromArgb(71, 2, 179, 57));
            SolidBrush greenLarge = new SolidBrush(Color.FromArgb(68, 103, 224, 4));

            foreach (shit st in lsL)
            {
                int iRight = mainWindow.ClientRectangle.Right - 20;
                int iY = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(st.price));
                int sS0 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(iS0));
                int sS1 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(iS1));
                int sS2 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(iS2));
                int sR0 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(iR0));
                int sR1 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(iR1));
                int sR2 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(iR2));
                int svixS1 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(ivixS1)); 
                int svixR1 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(ivixR1)); 

                int iX = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2) + 100);
                gr.DrawString(st.label, new Font("Arial", 8), new SolidBrush(Color.White), iX, iY);

                if (st.label == "RD0")
                    gr.FillRegion(greenSmall, new Region(new RectangleF(0, sR1, iRight, sR0 - sR1)));
                if (st.label == "RD1")
                    gr.FillRegion(greenMed, new Region(new RectangleF(0, sR2, iRight, sR1 - sR2)));
                if (st.label == "RD2")
                    gr.FillRegion(greenLarge, new Region(new RectangleF(0, svixR1, iRight, sR2 - svixR1)));

                if (st.label == "SD0") 
                    gr.FillRegion(redSmall, new Region(new RectangleF(0, sS0, iRight, sS1 - sS0)));
                if (st.label == "SD1")
                    gr.FillRegion(redMed, new Region(new RectangleF(0, sS1, iRight, sS2 - sS1)));
                if (st.label == "SD2")
                    gr.FillRegion(redLarge, new Region(new RectangleF(0, sS2, iRight, svixS1 - sS2)));
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
                        if (desc == "RD0") iR0 = Convert.ToDouble(price);
                        if (desc == "RD1") iR1 = Convert.ToDouble(price);
                        if (desc == "RD2") iR2 = Convert.ToDouble(price);
                        if (desc == "SD0") iS0 = Convert.ToDouble(price);
                        if (desc == "SD1") iS1 = Convert.ToDouble(price);
                        if (desc == "SD2") iS2 = Convert.ToDouble(price);
                        if (desc == "vix r2") ivixR1 = Convert.ToDouble(price);
                        if (desc == "vix s2") ivixS1 = Convert.ToDouble(price);

                        shit st = new shit();
                        st.price = Convert.ToDouble(price);
                        st.label = desc;
                        Color cl = Color.Gray;
                        //if (desc.Contains("range") || desc.Contains("HV"))
                        //    cl = crange;
                        if (desc.ToLower().Contains("min") || desc.Contains("VAL"))
                            cl = Color.Lime;
                        //else if (desc.Contains("kvo1"))
                        //    cl = ckvo1;
                        //else if (desc.Contains("SD"))
                        //    cl = cSD;
                        //else if (desc.Contains("RD"))
                        //    cl = cRD;
                        //else if (desc.Contains("kvo2") || desc.ToLower().Contains("support"))
                        //    cl = ckvo2;
                        else if (desc.ToLower().Contains("max") || desc.Contains("VAH"))
                            cl = Color.OrangeRed;
                        else if (desc.Contains("HV"))
                            cl = Color.Purple;
                        //st.color = cl;
                        lsL.Add(st);
                        if (desc.Trim().Contains("HV") || cl != Color.Gray)
                        {
                            int iWidth = desc.Trim().Contains("HV") ? 3 : 1;
                            LineLevel ll = new LineLevel(st.price, desc, cl, iWidth, LineStyle.Dot);
                            if (!LinesLevels.Contains(ll))
                                this.AddLineLevel(ll);
                        }
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
