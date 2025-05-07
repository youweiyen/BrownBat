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
        public Brep PossibleBounds { get; set; }
        public int UGroupId { get; set; }
        public int VGroupId { get; set; }
        public List<List<double>> ThermalData { get; private set; }
        CuttingBound(Brep possibleBounds, int uGroupId, int vGroupId)
        {
            PossibleBounds = possibleBounds;
            UGroupId = uGroupId;
            VGroupId = vGroupId;
        }

    }
}
