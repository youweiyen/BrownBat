using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.Components
{
    public class CuttingBound
    {
        public List<Brep> Bounds { get; set; }
        public Plane CutPlane { get; private set; }
        public List<List<double>> ThermalData { get; private set; }
        public double Mean { get; set; }
        public double TopFifthPercentile {  get; set; }
        public double LowFifthPercentile { get; set; }
        public CuttingBound(List<Brep> bounds)
        {
            Bounds = bounds;
        }
        public static void SetPlane(CuttingBound cuttingBound, Plane plane) => cuttingBound.CutPlane = plane;
        public static void SetBoundData(CuttingBound cuttingBound, List<List<double>> thermalData) => cuttingBound.ThermalData = thermalData;
        public static void SetMean(CuttingBound cuttingBound, double mean) => cuttingBound.Mean = mean;
        public static void SetTopFifth(CuttingBound cuttingBound, double topFifth) => cuttingBound.TopFifthPercentile = topFifth;
        public static void SetLowFifth(CuttingBound cuttingBound, double lowFifth) => cuttingBound.LowFifthPercentile = lowFifth;


        public static double Percentile(double[] sequence, double whichPercentile)
        {
            Array.Sort(sequence);
            int N = sequence.Length;
            double n = (N - 1) * whichPercentile + 1;
            // Another method: double n = (N + 1) * excelPercentile;
            if (n == 1d) return sequence[0];
            else if (n == N) return sequence[N - 1];
            else
            {
                int k = (int)n;
                double d = n - k;
                return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
            }
        }
    }
}
