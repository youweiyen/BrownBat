using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using BrownBat.Components;
using Grasshopper;
using System.Linq;
using Rhino;
using Grasshopper.Kernel.Types.Transforms;
using System.Configuration;

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
            //pManager.AddNumberParameter("");
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("CuttingCurves", "CC", "Recatangular Cutting Lines", GH_ParamAccess.list);
            pManager.AddBrepParameter("Groups", "G", "Merge Groups", GH_ParamAccess.tree);
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
            //add stock boundary
            Curve[] boundSegments = inBound.DuplicateSegments();
            splits.AddRange(boundSegments);

            //move to XY plane to compare
            Transform compareTransform = Transform.PlaneToPlane(inPlane, Plane.WorldXY);
            bool inverse = compareTransform.TryGetInverse(out Transform inverseCompare);
            if (inverse == false)
            {
                throw new Exception("failed to get inverse matrix");
            }
            splits.ForEach(crv => crv.Transform(compareTransform));
            boundBrep.Transform(compareTransform);

            //remove short curves that are close to longer parallel ones
            //divide to u and v groups
            List<Curve> uDirection = new List<Curve>();
            List<Curve> vDirection = new List<Curve>();
            foreach (var crv in splits)
            {
                Vector3d crvDirection = new Vector3d(crv.PointAtStart - crv.PointAtEnd);
                int uParallel = Vector3d.YAxis.IsParallelTo(crvDirection, tolerance);

                if (uParallel == 1 || uParallel == -1)
                {
                    uDirection.Add(crv);
                }
                else
                {
                    int vParallel = Vector3d.XAxis.IsParallelTo(crvDirection, tolerance);
                    if (vParallel == 1 || vParallel == -1)
                    {
                        vDirection.Add(crv);
                    }
                }
            }
            List<Curve> uJoin = CleanSplitCurve(uDirection);
            //TODO change set direction
            List<Curve> vJoin = CleanSplitCurve(vDirection);
            

            //remove short lines
            //sort horizontal curve down top, sort vertical curve left to right

            Brep[] pieces = boundBrep.Split(splits, tolerance);
            //group by center point projected to same axis and overlapping
            var uBounds = pieces.GroupBy(p => AreaMassProperties.Compute(p).Centroid.Y).Select(grp => grp.ToList());
            var vBounds = pieces.GroupBy(p => AreaMassProperties.Compute(p).Centroid.X).Select(grp => grp.ToList());

            DataTree<Brep> groupBounds = new DataTree<Brep>();
            int path = 0;
            foreach (var l in uBounds)
            {
                groupBounds.AddRange(l, new GH_Path(path));
                path++;
            }

            //inverse transform

            DA.SetDataList(0, uJoin);
            DA.SetDataTree(1, groupBounds);
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
        public List<Curve> CleanSplitCurve(List<Curve> curvesToClean)
        {

                var uAxisGroup = curvesToClean.GroupBy(uPoints => uPoints.PointAtEnd.X,
                                                    uPoints => uPoints,
                                                    (key, g) => new { xId = key, crv = g.ToList() })
                                           .OrderBy(sets => sets.xId)
                                           .ToList();



            List<double> moveId = new List<double>();
            for (int i = 0; i < curvesToClean.Count - 1; i++)
            {
                if (!moveId.Contains(uAxisGroup[i].xId))
                {
                    for (int j = i + 1; j < uAxisGroup.Count; j++)
                    {
                        if (!moveId.Contains(uAxisGroup[j].xId))
                        {
                            double distance = Math.Abs(uAxisGroup[i].xId - uAxisGroup[j].xId);
                            if (distance < 12)
                            {
                                Transform move = Transform.Translation(-distance, 0, 0);
                                foreach (var crv in uAxisGroup[j].crv)
                                {
                                    crv.Transform(move);
                                    moveId.Add(uAxisGroup[j].xId);
                                }
                            }
                        }

                    }
                }
            }
            var mergeGroup = curvesToClean.GroupBy(uPoints => uPoints.PointAtEnd.X).Select(grp => grp.ToList());
            var uJoin = new List<Curve>();
            foreach (var overlaps in mergeGroup)
            {

                List<Point3d> axisPoints = new List<Point3d>();
                axisPoints.AddRange(overlaps.Select(o => o.PointAtEnd));
                axisPoints.AddRange(overlaps.Select(o => o.PointAtStart));
                IEnumerable<Point3d> orderedAxisPoint = axisPoints.OrderBy(points => points.Y);

                Curve plCurve = new PolylineCurve(orderedAxisPoint).ToNurbsCurve();
                uJoin.Add(plCurve);

            }
            return uJoin;
        }
        enum SetDirection
        {
            Horizontal,
            Vertical,
        }
    }
}