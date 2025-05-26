using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.Components
{
    public class ShatterBound
    {
        public List<Brep> Bounds { get; set; }
        //public double X1, Y1, X2, Y2;
        public int? UGroupId { get; set; }
        public int? VGroupId { get; set; }
        public Plane CutPlane { get; private set; }

        public ShatterBound(List<Brep> bounds, int? uGroupId, int? vGroupId)
        {
            Bounds = bounds;
            UGroupId = uGroupId;
            VGroupId = vGroupId;
        }

        //public bool IsAdjacent(ShatterBound other)
        //{
        //    // Check for vertical/horizontal touching
        //    bool vertical = X1 == other.X1 && X2 == other.X2 &&
        //                    (Y2 == other.Y1 || other.Y2 == Y1);
        //    bool horizontal = Y1 == other.Y1 && Y2 == other.Y2 &&
        //                      (X2 == other.X1 || other.X2 == X1);
        //    return vertical || horizontal;
        //}
        //public static void GetShatterEdges(ShatterBound shatterBound, Transform calculationPlane)
        //{
        //    Brep dupBrep = shatterBound.Bound.DuplicateBrep();
        //    dupBrep.Transform(calculationPlane);
        //    var shatterVertices = dupBrep.DuplicateVertices();

        //    var shatterCorners = shatterVertices.OrderBy(pts => pts.X).ThenBy(pts => pts.Y).ToList();
        //    shatterBound.X1 = shatterCorners[0].X;
        //    shatterBound.Y1 = shatterCorners[0].Y;
        //    shatterBound.X2 = shatterCorners[3].X;
        //    shatterBound.Y2 = shatterCorners[3].Y;
        //}
    }
}
