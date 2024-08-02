using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace BrownBat.Construct
{
    public class GH_ConstructPanel : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_ImportPanel class.
        /// </summary>
        public GH_ConstructPanel()
          : base("ConstructPanel", "Nickname",
              "Construct Bat Panel Object",
              "BrownBat", "Construct")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("BatBlock", "D", "Import the geometrical data to Object", GH_ParamAccess.list);
            pManager.AddGenericParameter("BatData", "D", "Import the data to Object", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Bat Object", "O", "Bat Object wiht all the panel data", GH_ParamAccess.list);
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
            get { return new Guid("EF6963EA-F90F-4C91-9041-B7636DAF4030"); }
        }
    }
}