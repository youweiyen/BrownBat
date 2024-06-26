using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace BrownBat
{
    public class BrownBatInfo : GH_AssemblyInfo
    {
        public override string Name => "BrownBat";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Designing with Thermal and Acoustic properties of Decay Wood";

        public override Guid Id => new Guid("70d66bf5-9b0d-46f7-9b72-a489ed31092b");

        //Return a string identifying you or your company.
        public override string AuthorName => "You-Wei Yen";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}