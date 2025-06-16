using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.Components
{
    public class ShatterGroup
    {
        public List<Brep> Bounds { get; set; }
        //public double X1, Y1, X2, Y2;
        public int? UGroupId { get; set; }
        public int? VGroupId { get; set; }
        public static Plane CutPlane;

        public ShatterGroup(List<Brep> bounds, int? uGroupId, int? vGroupId)
        {
            Bounds = bounds;
            UGroupId = uGroupId;
            VGroupId = vGroupId;
        }

    }
}
