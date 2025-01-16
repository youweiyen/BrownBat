using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using static Rhino.Render.TextureGraphInfo;

namespace BrownBat.Components
{
    public class HeatCluster
    {
        public string ElementName { get; private set; }
        public int ClusterID { get; private set; }
        public Point3d Center { get; private set; }
        public Line XAxis { get; private set; }
        public Line YAxis { get; private set; }
        public HeatCluster() { }
        public HeatCluster(string elementName, int clusterID, Point3d center, Line xAxis, Line yAxis) 
        {
            ElementName = elementName;
            ClusterID = clusterID;
            Center = center;
            XAxis = xAxis;
            YAxis = yAxis;
        }

    }
}
