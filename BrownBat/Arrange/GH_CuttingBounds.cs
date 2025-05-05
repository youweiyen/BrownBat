using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

namespace BrownBat.Arrange
{
    public class GH_CuttingBounds : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_CuttingBounds class.
        /// </summary>
        public GH_CuttingBounds()
          : base("CuttingBounds", "CB",
              "Recatangular Cutting Lines",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Patttern", "P", "Pattern Object", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("CuttingCurves", "CC", "Recatangular Cutting Lines", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //int lay = 0;
            //for (int b = 0; b < trees.Count; b++)
            //{
            //    var cuttingLines = new List<Curve>();
            //    var neighborBox = trees.Select(f => f.TrimBound).Where((v, i) => i != b).ToList();

            //    var flatNeighbor = neighborBox.SelectMany(i => i).Where(box => box != null);
            //    Curve[] boundLines = trees[b].ShiftBound.DuplicateEdgeCurves();
            //    foreach (Curve line in boundLines)
            //    {
            //        cuttingLines.Add(line.ToNurbsCurve().ExtendByLine(CurveEnd.Both, flatNeighbor));
            //    }
            //    splits.AddRange(cuttingLines, new GH_Path(lay));
            //    lay++;
            //}
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("16B21534-A8C6-445F-852A-4423D470E61E"); }
        }
    }
}