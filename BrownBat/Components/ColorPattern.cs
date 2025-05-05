using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.Components
{
    public class ColorPattern
    {
        public Curve Shape { get; set; }
        public Brep ShiftBound { get; set; }
        public List<ColorPattern> Children { get; set; }
        public List<ColorPattern> Parent { get; set; }
        public Plane MinBoundingPlane { get; set; }
        public Rectangle3d PlaneAlignedRect { get; set; }
        public Brep[] TrimBound { get; set; }
        public List<double> PoplarData { get; set; }

        public ColorPattern(Curve shape, Plane minPlane, List<ColorPattern> children, List<ColorPattern> parent)
        {
            Shape = shape;
            MinBoundingPlane = minPlane;
            Children = children;
            Parent = parent;
        }
    }
}
