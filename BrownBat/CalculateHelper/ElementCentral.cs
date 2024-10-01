using BrownBat.Components;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.CalculateHelper
{
    public class ElementCentral
    {
        public static double Mean(IEnumerable<double> inValue)
        {
            double result = inValue.Sum() / inValue.Count();
            return result;
        }
        public static double Median(IEnumerable<double> inValue)
        {
            double[] sortedValue = inValue.ToArray();

            int count = sortedValue.Length;
            int middleIndex = count / 2;

            if (count % 2 == 0)
            {
                double median = (sortedValue[middleIndex - 1] + sortedValue[middleIndex]) / 2.0;
                return median;
            }
            else
            {
                return sortedValue[middleIndex];
            }
        }
        public static double Mode(IEnumerable<double> inValue)
        {
            double[] arrValue = inValue.ToArray();

            if (arrValue == null || arrValue.Length == 0)
                throw new ArgumentException("Array is empty.");

            // Convert the array to a Lookup
            var dictSource = arrValue.ToLookup(x => x);

            // Find the number of modes
            var numberOfModes = dictSource.Max(x => x.Count());

            // Get only the modes
            var mode = dictSource.Where(x => x.Count() == numberOfModes).Select(x => x.Key).First();

            return mode;
        }
    }
    public enum CentralTendency
    {
        Mean,
        Median,
        Mode,
    }

    public class SelectedElements 
    {
        public Element Elements { get;set; }
        public string Name { get; set; }
    }
}
