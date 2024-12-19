using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace BrownBat.Components
{
    public class HeatCluster
    {
        public string ElementName { get; private set; }
        public Polyline Boundary { get; private set; }
        public Point3d Center { get; private set; }
        public Line XAxis { get; private set; }
        public Line YAxis { get; private set; }

        public HeatCluster(string elementName, Polyline boundary, Point3d center, Line xAxis, Line yAxis) 
        {
            ElementName = elementName;
            Boundary = boundary;
            Center = center;
            XAxis = xAxis;
            YAxis = yAxis;
        }

    }
}
