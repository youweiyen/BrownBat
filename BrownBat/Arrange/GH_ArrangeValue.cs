using System;
using System.Collections.Generic;
using BrownBat.Components;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace BrownBat.Arrange
{
    public class GH_ArrangeValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GH_ArrangeValue()
          : base("ArrangeValue", "AV",
              "Arrange all possible overlapping outcomes to find optimal conductivity value",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element with selected range of heat value", GH_ParamAccess.list);
            pManager.AddGenericParameter("Structure", "S", "Structure to build", GH_ParamAccess.item);
            pManager.AddCurveParameter("Area", "A", "Area to assign high conductivity values", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTransformParameter("Transform", "T", "Transformation of element on to the structure", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            Structure inStructure = new Structure();
            List<Curve> inRegion = new List<Curve>();

            DA.GetDataList(0, inElement);
            DA.GetData(1, ref inStructure);
            DA.GetDataList(2, inRegion);

            inRegion[0].TryGetPolyline(out Polyline plineRegion);
            HeatCluster cluster = inElement[0].HeatClusterGroup[0];
            //cluster.XAxis;
            
            
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
            get { return new Guid("C3641087-4F32-41BF-A831-84E1A3E48468"); }
        }
    }
}