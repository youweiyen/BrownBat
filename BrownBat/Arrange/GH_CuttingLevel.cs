using System;
using System.Collections.Generic;
using System.Linq;
using BrownBat.CalculateHelper;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Collections;
using Rhino.Geometry;

namespace BrownBat.Arrange
{
    public class GH_CuttingLevel : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CuttingLevel class.
        /// </summary>
        public GH_CuttingLevel()
          : base("CuttingLevel", "CL",
              "Pattern boundary amount to divide into smaller pieces ",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Patttern", "P", "All defined pattern as curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimum", "Min", "Smallest dimension of pattern", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Pattern", "P", "Pattern level", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> inCurve = new List<Curve>();
            double minLength = default;
            DA.GetDataList(0, inCurve);
            DA.GetData(1, ref minLength);

            double tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
           
            var trees = new List<CurveTree>();
            var root = new List<CurveTree>();

            for (int i = 0; i < inCurve.Count; i++)
            {
                trees.Add(new CurveTree(inCurve[i]));
            }

            foreach (CurveTree outerCurve in trees)
            {
                foreach (CurveTree innerCurve in trees)
                {
                    if (outerCurve.Shape != innerCurve.Shape)
                    {
                        RegionContainment contains = Curve.PlanarClosedCurveRelationship(outerCurve.Shape, innerCurve.Shape, Plane.WorldXY, tolerance);

                        if (contains == RegionContainment.BInsideA)
                        {
                            outerCurve.Children.Add(innerCurve);
                        }
                    }
                }

                // Add root curve (curves with no parent)
                if (outerCurve.Children.Count == 0)
                {
                    root.Add(outerCurve);
                }
            }

            root[0].Shape.TryGetPolyline(out Polyline rootPolyline);
            Point3d[] points = rootPolyline.ToArray();
            Rectangle3d minBox = AreaHelper.MinBoundingBox(points, Plane.WorldXY);
            if(minBox.X.Length < minLength || minBox.Y.Length < minLength)

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
            get { return new Guid("B7B08CFB-D059-41A2-B42A-CA8718FD45D1"); }
        }

        public class CurveTree 
        {
            public Curve Shape { get; set; }
            public List<CurveTree> Children { get; set; }
            public List<CurveTree> Parent { get; set; }

            public CurveTree(Curve shape)
            {
                Shape = shape;
            }
        }
    }
}