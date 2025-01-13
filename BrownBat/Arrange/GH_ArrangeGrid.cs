using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using BrownBat.CalculateHelper;
using BrownBat.Components;
using System.Linq;

namespace BrownBat.Arrange
{
    public class GH_ArrangeGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_ArrangeGrid class.
        /// </summary>
        public GH_ArrangeGrid()
          : base("ArrangeGrid", "AG",
              "Respect boundary and arranging elements as grid",
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
            pManager.AddPointParameter("BoundaryPoint", "B", "Boundary point area to assign high conductivity values", GH_ParamAccess.list);
            pManager.AddNumberParameter("Difference", "D",
                "Heat area axis size difference. Default set to 10",
                GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Transformed element geometry", GH_ParamAccess.tree);
            pManager.AddTextParameter("Name", "N", "Element Name", GH_ParamAccess.tree);
            pManager.AddTransformParameter("T", "T", "T", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            Structure inStructure = new Structure();
            List<Point3d> inRegion = new List<Point3d>();
            double inDifference = 10;

            DA.GetDataList(0, inElement);
            DA.GetData(1, ref inStructure);
            DA.GetDataList(2, inRegion);
            DA.GetData(3, ref inDifference);

            Plane boundingPlane = AreaHelper.BoundingPlane(inRegion, Plane.WorldXY);
            Point3d averagePoint = new Point3d(inRegion.Select(pt => pt.X).Average(),
                                   inRegion.Select(pt => pt.Y).Average(),
                                   inRegion.Select(pt => pt.Z).Average());

            var closePolyPoints = inRegion.Concat(new[] { inRegion.First() });
            Polyline convexBoundary = new Polyline(closePolyPoints);
            Curve boundaryCurve = convexBoundary.ToNurbsCurve();
            Line xAxis = AreaHelper.AxisLineFromCenter(averagePoint, boundingPlane.XAxis, boundaryCurve);
            Line yAxis = AreaHelper.AxisLineFromCenter(averagePoint, boundingPlane.YAxis, boundaryCurve);

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
            get { return new Guid("020DB6E6-B69A-42B2-919C-BC2D25854014"); }
        }
    }
}