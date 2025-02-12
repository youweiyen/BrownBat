using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BrownBat.Components;

namespace BrownBat.Arrange
{
    public class GH_NextLayerPlace : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_NextLayerPlace class.
        /// </summary>
        public GH_NextLayerPlace()
          : base("PlacePassive", "PP",
              "Place the passive Elements on the next layer",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element to place on passive positions", GH_ParamAccess.list);
            pManager.AddLineParameter("BoundaryAxis", "BA", "Boundary axis to place", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Placed Element", GH_ParamAccess.list);
            pManager.AddTransformParameter("Transformation", "T", "Element Transformation", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            GH_Structure<GH_Line> inAxis = new GH_Structure<GH_Line>();

            DA.GetDataList(0, inElement);
            DA.GetDataTree(1, out inAxis);
            for (int i = 0; i < inAxis.Branches.Count; i++)
            {
                
            }
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
            get { return new Guid("23DD3350-F6E2-47F5-A131-793EDF3DF76C"); }
        }
    }
}