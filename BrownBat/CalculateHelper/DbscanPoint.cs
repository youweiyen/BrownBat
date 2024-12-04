using Dbscan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.CalculateHelper
{
    public class DbscanPoint : IPointData
    {

        public DbscanPoint(double x, double y) =>
            Point = new Point(x, y);

        public Point Point { get; }
    }
}
