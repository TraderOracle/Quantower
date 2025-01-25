using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Chart;
using TradingPlatform.BusinessLayer.Utils;

namespace QuantowerIndicators
{
    public class HullMovingAverage : Indicator
    {
        // Input parameter for the HMA period
        [InputParameter("Period", 0, 1, 999, 1, 0)]
        public int Period = 14;

        private int sqrtPeriod;

        // Working buffers to store intermediate results
        private double[] priceBuffer;
        private double[] wmaFull;
        private double[] wmaHalf;
        private double[] wmaDiff;
        private double[] wmaSmoothed;

        public HullMovingAverage()
        {
            // Indicator info
            this.Name = "Hull Moving Average";
            this.Description = "An implementation of the Hull Moving Average";
            this.SeparateWindow = false; // Plot on the same chart as the price

            // Adding one line series for the final HMA plot
            this.AddLineSeries("HMA", Color.Cyan, 2, LineStyle.Solid);
        }

        // Called once when the indicator is created
        protected override void OnInit()
        {
            // Calculate sqrtPeriod in advance
            sqrtPeriod = (int)Math.Sqrt(Period);

            // Prepare arrays based on the available historical data count
            int dataCount = this.HistoricalData.Count;
            priceBuffer = new double[dataCount];
            wmaFull = new double[dataCount];
            wmaHalf = new double[dataCount];
            wmaDiff = new double[dataCount];
            wmaSmoothed = new double[dataCount];
        }

        // Called on every bar update (historical or new bar)
        protected override void OnUpdate(UpdateArgs args)
        {
            // We only calculate values on new or historical bars
            // (Skip if something else triggered OnUpdate, like a visual change)
            if (args.Reason == UpdateReason.NewBar || args.Reason == UpdateReason.HistoricalBar)
            {
                int lastIndex = this.Count - 1;

                // Current bar's closing price (you can change this to open/high/low if you wish)
                double closePrice = this.GetPrice(PriceType.Close, 0);

                // Store in our price buffer
                priceBuffer[lastIndex] = closePrice;

                // Calculate WMA(Price, Period)
                wmaFull[lastIndex] = WeightedMovingAverage(priceBuffer, Period, lastIndex);

                // Calculate WMA(Price, Period/2)
                int halfPeriod = Math.Max(1, Period / 2); // ensure no zero
                wmaHalf[lastIndex] = WeightedMovingAverage(priceBuffer, halfPeriod, lastIndex);

                // Intermediate difference array: 2 * WMA(Price, n/2) - WMA(Price, n)
                wmaDiff[lastIndex] = 2.0d * wmaHalf[lastIndex] - wmaFull[lastIndex];

                // Now apply WMA on the difference array with length = sqrt(Period)
                wmaSmoothed[lastIndex] = WeightedMovingAverage(wmaDiff, sqrtPeriod, lastIndex);

                // Assign final HMA value to the indicator output
                this.SetValue(wmaSmoothed[lastIndex]);
            }
        }

        //-------------------------------------------------------------------------------------------
        //  HELPER: Weighted Moving Average
        //-------------------------------------------------------------------------------------------
        private double WeightedMovingAverage(double[] source, int length, int currentIndex)
        {
            // If the requested length or index range is invalid, return 0
            if (length <= 0 || currentIndex - length + 1 < 0)
                return 0d;

            double sum = 0d;
            double weightSum = 0d;
            int weight = length;

            // We iterate backward from currentIndex to (currentIndex - length + 1)
            for (int i = currentIndex; i > currentIndex - length; i--)
            {
                sum += source[i] * weight;
                weightSum += weight;
                weight--;
            }

            // Avoid division by zero, though weightSum shouldn’t be zero if length > 0
            if (weightSum == 0d)
                return 0d;

            return sum / weightSum;
        }
    }
}
