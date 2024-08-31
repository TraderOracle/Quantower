
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

namespace Killpips_QT
{
    public class Killpips_QT : Indicator
    {
        [InputParameter("Service URL")]
        public string url = "https://lj4dbrseh1.execute-api.us-east-2.amazonaws.com/Killpips/";
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
            FetchDataFromAPI();
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);
            if (CurrentChart == null || this.HistoricalData == null)
                return;

            Graphics gr = args.Graphics;
            IChartWindow mainWindow = CurrentChart.MainWindow;

            HistoryItemBar bar1 = new HistoryItemBar();
            for (int i = (int)mainWindow.CoordinatesConverter.GetBarIndex(mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Left)); i <= (int)Math.Ceiling(mainWindow.CoordinatesConverter.GetBarIndex(mainWindow.CoordinatesConverter.GetTime(mainWindow.ClientRectangle.Right))); i++)
                if (i > 0 && i < this.HistoricalData.Count && this.HistoricalData[i, SeekOriginHistory.Begin] is HistoryItemBar bar)
                    bar1 = (HistoryItemBar)this.HistoricalData[i - 1, SeekOriginHistory.Begin];

            foreach (shit st in lsL)
                gr.DrawString(st.label, new Font("Arial", iFontSize), txtBrush, (int)Math.Round(mainWindow.CoordinatesConverter.GetChartX(bar1.TimeRight) - (CurrentChart.BarsWidth / 2)), (int)Math.Round(mainWindow.CoordinatesConverter.GetChartY(st.price)));
        }

        private async void FetchDataFromAPI()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var jsonResponse = await client.GetStringAsync(url);
                    JObject jo = JObject.Parse(jsonResponse);
                    JObject o = JObject.Parse(jo["body"].Value<string>());
                    string sLine = o["$" + this.Symbol.Name.Substring(1, 2) + "1!"].Value<string>();

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
            }
            catch (Exception ex)
            {
                //this.Log("Error fetching data from API: " + ex.Message);
            }
        }

        protected override void OnUpdate(UpdateArgs args)
        {

        }
    }
}
