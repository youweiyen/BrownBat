using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using BrownBat.Components;
using Grasshopper;
using System.Linq;
using Rhino;

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
            pManager.AddCurveParameter("Boundary", "B", "Stock Boundary", GH_ParamAccess.item);
            pManager.AddPlaneParameter("CutPlane", "CP", "Cutting Rotaion Plane" +
                "Default set to WorldXY", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("CuttingCurves", "CC", "Recatangular Cutting Lines", GH_ParamAccess.list);
            pManager.AddBrepParameter("Stock", "S", "Result Stock as surface", GH_ParamAccess.list);
            pManager.AddNumberParameter("Homogenity", "H", "Stock Homogenity", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<ColorPattern> inPattern = new List<ColorPattern>();
            Curve inBound = default;
            Plane inPlane = Plane.WorldXY;
            DA.GetDataList(0, inPattern);
            DA.GetData(1, ref inBound);
            DA.GetData(2, ref inPlane);

            double tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Brep boundBrep = Brep.CreatePlanarBreps(inBound, tolerance).First();
            List<Curve> splits = new List<Curve>();

            for (int b = 0; b < inPattern.Count; b++)
            {
                var cuttingLines = new List<Curve>();
                var neighborBox = inPattern.Select(f => f.TrimBound).Where((v, i) => i != b);
                var flatNeighbor = neighborBox.SelectMany(i => i).Where(box => box != null).ToList();
                flatNeighbor.Add(boundBrep);

                Curve[] boundLines = inPattern[b].ShiftBound.DuplicateEdgeCurves();
                foreach (Curve line in boundLines)
                {
                    cuttingLines.Add(line.ToNurbsCurve().ExtendByLine(CurveEnd.Both, flatNeighbor));
                }
                splits.AddRange(cuttingLines);
                
            }
            //remove short curves that are close to longer parallel ones
            foreach (var crv in splits)
            {
                Vector3d neighborDirection = new Vector3d(crv.PointAtStart - crv.PointAtEnd);
                //u
                int parallel = inPlane.XAxis.IsParallelTo(neighborDirection, tolerance);
                //v
                
            }
            //group parallel geometries

            foreach (var pat in inPattern)
            {
                //pat.TrimBound;
            }

            DA.SetDataList(0, splits);
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