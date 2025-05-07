using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace BrownBat.Arrange
{
    public class GH_MillBounds : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_MillBounds class.
        /// </summary>
        public GH_MillBounds()
          : base("MillBounds", "MB",
              "Bounds to join for milling",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Bounds", "B", "Bounds as groups", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Bitmap", "B", "Color as bitmap", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Stock", "S", "Result Stock as surface", GH_ParamAccess.list);
            pManager.AddNumberParameter("Homogenity", "H", "Stock Homogenity", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
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
            get { return new Guid("BE7A3EFA-5087-4FAA-B8D9-EDF812F5BC37"); }
        }
    }
}