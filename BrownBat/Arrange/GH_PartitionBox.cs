using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace BrownBat.Arrange
{
    public class GH_PartitionBox : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SegmentByBox class.
        /// </summary>
        public GH_PartitionBox()
          : base("PartitionBox", "PB",
              "Partition by Thermal Value into box shape",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Thermal", "T", "Thermal Scan CSV file", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Geometry", "G", "Geometry Scan", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Segmented Elements", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //temperature segment to ranges
            //get all axis from all smallest bounding box
            //try all bounding box, and get the one axis for smallest overlapping boxes
            //
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
            get { return new Guid("BD2311F4-A225-4F4C-8ED0-2517A3DB9F12"); }
        }
    }
}