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
        public static double Median()
        {
            double result = 0;
            return result;
        }
        public double Mode()
        {
            double result = 0;
            return result;
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
