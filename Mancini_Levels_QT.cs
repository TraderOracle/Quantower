using System;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace Mancini_Levels_QT
{
	public class Mancini_Levels_QT : Indicator
    {
        bool bDone = false;

        [InputParameter("Support Values")]
        public string sSupport = "5648 (major), 5644, 5639, 5625-30 (major), 5615, 5610, 5605, 5598 (major), 5585-90 (major), 5576, 5566 (major), 5560, 5552, 5540-42 (major), 5534 , 5528 (major), 5519, 5506, 5502, 5492-97 (major), 5482 (major), 5474, 5461, 5451-56 (major), 5445, 5438, 5432 (major), 5423 (major), 5414, 5408 (major), 5400 (major), 5393, 5388, 5379 (major), 5372 (major)";
        [InputParameter("Resistance Values")]
        public string sResistance = "5662 (major), 5668, 5672, 5679-82 (major), 5690, 5698 (major), 5705, 5713 (major), 5721 (major), 5728, 5736-38 (major), 5750 (major), 5759, 5767, 5773, 5780 (major), 5789, 5795-5800 (major), 5808, 5813, 5823 (major), 5828, 5833, 5837, 5847, 5852, 5858 (major), 5865 (major), 5873, 5883, 5890, 5897, 5905 (major)";
        [InputParameter("Support Color")]
        public Color cSupport = Color.Lime;
        [InputParameter("Major Support Color")]
        public Color cMajorSupport = Color.Lime;
        [InputParameter("Resistance Color")]
        public Color cResistance = Color.Red;
        [InputParameter("Major Resistance Color")]
        public Color cMajorResistance = Color.Red;

        public Mancini_Levels_QT()
            : base()
        {
            Name = "Mancini Levels";
            Description = "Mancini Levels for Quantower";

            SeparateWindow = false;
        }

        protected override void OnInit()
        {
            bDone = false;
        }

        private async void FetchDataFromAPI()
        {
            try
            {
                string[] sb = sSupport.Split(", ");
                foreach (string sr in sb)
                {
                    string price = sr.Replace("(major)", "").Trim().Substring(0, 4);
                    if (sr.Contains("(major)"))
                        this.AddLineLevel(Convert.ToDouble(price), sr.Trim(), cMajorSupport, 2, LineStyle.Dash);
                    else
                        this.AddLineLevel(Convert.ToDouble(price), sr.Trim(), cSupport, 1, LineStyle.Solid);
                }

                string[] sb2 = sResistance.Split(", ");
                foreach (string sr in sb2)
                {
                    string price = sr.Replace("(major)", "").Trim().Substring(0, 4);
                    if (sr.Contains("(major)"))
                        this.AddLineLevel(Convert.ToDouble(price), sr.Trim(), cMajorResistance, 2, LineStyle.Dash);
                    else
                        this.AddLineLevel(Convert.ToDouble(price), sr.Trim(), cResistance, 1, LineStyle.Solid);
                }
                bDone = true;
            }
            catch (Exception ex)
            {

            }
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (!bDone)
                FetchDataFromAPI();
        }

    }
}
