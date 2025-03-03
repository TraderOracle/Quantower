
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
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;

namespace TraderSmarts_QT
{

    public class TraderSmarts_QT : Indicator
    {
        #region VARIABLES

        private const string sVersion = "1.1";
        private bool bUseAlerts = true;
        private bool bAlertWick = false;
        private bool bDrawn = false;
        private bool bProcessing = false;

        private struct days
        {
            public string label;
            public double price1;
            public double price2;
            public Color c;
        }
        private List<days> lsDays = new List<days>();

        #endregion

        #region SETTINGS

        [InputParameter("Show Line Labels")]
        public bool bShowLabels = true;
        [InputParameter("Font Size")]
        public int iFontSize = 8;
        [InputParameter("Show MTS")]
        public bool bShowMTS = true;
        [InputParameter("Show TS")]
        public bool bShowTS = true;
        [InputParameter("Input File")]
        public System.String txtFile = @"c:\temp\tradersmarts.txt";

        [InputParameter("Font Color")]
        public Brush txtBrush = Brushes.White;
        [InputParameter("Long Color")]
        public Color cLong = Color.Lime;
        [InputParameter("Short Color")]
        public Color cShort = Color.Red;
        [InputParameter("Line in Sand Color")]
        public Color cSand = Color.FromArgb(255, 143, 83, 237);
        [InputParameter("MTS Color")]
        public Color cMTS = Color.Beige;
        [InputParameter("TS Color")]
        public Color cTS = Color.DodgerBlue;

        #endregion

        public TraderSmarts_QT()
            : base()
        {
            Name = "TraderSmarts QT";
            Description = "TraderSmarts for Quantower";
            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            FetchData();
            bDrawn = false;
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
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

            SolidBrush redLarge = new SolidBrush(Color.FromArgb(122, 176, 74, 19));
            SolidBrush greenLarge = new SolidBrush(Color.FromArgb(128, 103, 224, 4));
            SolidBrush purple = new SolidBrush(Color.FromArgb(108, 123, 48, 242));

            foreach (days st in lsDays)
            {
                SolidBrush howdy = st.label.Contains("Short") ? redLarge :
                    st.label.Contains("Long") ? greenLarge : purple;

                int iY1 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(st.price1));
                int iY2 = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(st.price2));
                int iX = (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2) + 30);
                int iRight = mainWindow.ClientRectangle.Right - 20;

                if (iY1 != iY2)
                {
                    if (iY2 > iY1)
                    {
                        //if (! gr.IsVisible(new RectangleF(0, iY1, iRight, iY2 - iY1)))
                        {
                            gr.FillRegion(howdy, new Region(new RectangleF(0, iY1, iRight, iY2 - iY1)));
                            gr.DrawString(st.label, new Font("Arial", iFontSize), txtBrush, iX, iY1);
                        }
                    }
                    else
                    {
                        //if (!gr.IsVisible(new RectangleF(0, iY1, iRight, iY2 - iY1)))
                        {
                            gr.FillRegion(howdy, new Region(new RectangleF(0, iY2, iRight, iY1 - iY2)));
                            gr.DrawString(st.label, new Font("Arial", iFontSize), txtBrush, iX, iY2);
                        }
                    }
                }
                else
                    gr.DrawString(st.label, new Font("Arial", iFontSize), txtBrush, iX, iY1);

                //gr.DrawRectangle(new Pen(Brushes.Red), new Rectangle(iX, iY, 500, 550));
                //gr.FillRectangle(semiTransparentRedBrush, new Rectangle(0, iY, mainWindow.ClientRectangle.Width - 20, 20));
            }

            //bDrawn = true;
            base.OnPaintChart(args);
        }

        #region MISC

        private void AddRecord(string price, string s, string price2 = "")
        {
            Color c = new Color();
            if (price2 == "") price2 = price;

            if (s.Contains("Long")) c = cLong;
            else if (s.Contains("Short")) c = cShort;
            else if (s.Contains("Sand")) c = cSand;
            else if (s.Contains("MTS")) c = cMTS;
            else if (s.Contains("TS")) c = cTS;

            try
            {
                days a = new days();
                a.c = c;
                a.label = s;
                a.price1 = Convert.ToDouble(price);
                a.price2 = Convert.ToDouble(price2);
                lsDays.Add(a);
                int iWidth = s.Contains("Sand") ? 4 : 1;
                if (a.price1 == a.price2)
                {
                    LineLevel lll = new LineLevel(a.price2, s, c, iWidth, LineStyle.Solid);
                    if (!LinesLevels.Contains(lll))
                        this.AddLineLevel(lll);
                }
            }
            catch { }
        }

        protected bool IsDigit(string s)
        {
            try
            {
                double xx = Convert.ToDouble(s);
                return true;
            }
            catch { }
            return false;
        }

        #endregion

        #region PROCESS DATA

        protected bool FetchData()
        {
            try
            {
                string[] lines = File.ReadAllLines(txtFile);
                for (int iR = 0; iR < lines.Length; iR += 1)
                {
                    string s = lines[iR];
                    string s1, s2;
                    if (s.Contains("Numbers") && bShowMTS)
                    {
                        // NQ MTS Numbers: 20390.25, 19501, 19234.75, 18912, 18517, 18420
                        s = s.Replace("MTS Numbers: ", "").Replace(" ", "");
                        string[] price = s.Split(',');
                        foreach (string p in price)
                            AddRecord(p, "MTS");
                    }
                    else if (s.Length > 400 && bShowTS)
                    {
                        string[] ass = s.Replace(" ", "").Split(",");
                        foreach (string p in ass)
                            if (IsDigit(p))
                                AddRecord(p, "TS");
                    }
                    else if (s.Contains("Short") || s.Contains("Long") || s.Contains("Sand"))
                    {
                        if (s.Contains(" - "))
                        {
                            // 4610.75 - 4608.75 Range Short
                            s = s.Replace(" - ", "-");
                            int i = s.IndexOf(' ');
                            if (i >= 0)
                            {
                                s1 = s.Substring(0, i).Trim();
                                s2 = "    " + s.Substring(i, s.Length - i).Trim();
                                string[] price = s1.Split('-');
                                if (IsDigit(price[0]) && IsDigit(price[1]))
                                    AddRecord(price[0], s2, price[1]);
                            }
                        }
                        else
                        {
                            // 4597.75 Line in the Sand
                            int i = s.IndexOf(' ');
                            if (i >= 0)
                            {
                                s1 = s.Substring(0, i).Trim();
                                s2 = s.Substring(i, s.Length - i).Trim();
                                if (IsDigit(s1))
                                    AddRecord(s1, s2);
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        #endregion

        protected override void OnUpdate(UpdateArgs args)
        {
            if (this.Count < 2)
                return;

            if (args.Reason == UpdateReason.NewBar)
            {
                bProcessing = true;
                //FetchData();
                bProcessing = false;
                bDrawn = true;
            }

        }
    }
}
