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
using System.Net;

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
            pManager.AddGenericParameter("ShatterBound", "SB", "Grouped Objects with group ID", GH_ParamAccess.list);
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

            //move to XY plane to compare
            Transform compareTransform = Transform.PlaneToPlane(inPlane, Plane.WorldXY);
            bool inverse = compareTransform.TryGetInverse(out Transform inverseCompare);
            if (inverse == false)
            {
                throw new Exception("failed to get inverse matrix");
            }
            splits.ForEach(crv => crv.Transform(compareTransform));
            boundBrep.Transform(compareTransform);

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

            //remove short lines
            //sort horizontal curve down top, sort vertical curve left to right
            List<Curve> uJoin = CleanSplitCurve(uDirection, SetDirection.Horizontal);
            List<Curve> vJoin = CleanSplitCurve(vDirection, SetDirection.Vertical);

            //move bound to original material size
            List<Curve> orderUJoin = uJoin.OrderBy(crv => crv.PointAtEnd.X).ToList();
            List<Curve> orderVJoin = vJoin.OrderBy(crv => crv.PointAtEnd.Y).ToList();

            Curve[] boundSegments = inBound.DuplicateSegments();
            Curve[] uSegment = new Curve[2];
            Curve[] vSegment = new Curve[2];
            int uCount = 0;
            int vCount = 0;
            foreach (var crv in boundSegments)
            {
                Vector3d crvDirection = new Vector3d(crv.PointAtStart - crv.PointAtEnd);
                int uParallel = Vector3d.YAxis.IsParallelTo(crvDirection, tolerance);
                if (uParallel == 1 || uParallel == -1)
                {
                    uSegment[uCount] = crv;
                    uCount++;
                }
                else
                {
                    int vParallel = Vector3d.XAxis.IsParallelTo(crvDirection, tolerance);
                    if (vParallel == 1 || vParallel == -1)
                    {
                        vSegment[vCount] = crv;
                        vCount++;
                    }
                }
            }
            uSegment.OrderBy(crv => crv.PointAtEnd.X);
            orderUJoin[0] = uSegment.First();
            orderUJoin[orderUJoin.Count - 1] = uSegment.Last();

            vSegment.OrderBy(crv => crv.PointAtEnd.Y);
            orderVJoin[0] = vSegment.First();
            orderVJoin[orderVJoin.Count - 1] = vSegment.Last();
            
            //move curve end to closest perpendicular line
            IEnumerable<double> xPartitions = orderUJoin.Select(crv => crv.PointAtEnd.X);
            IEnumerable<double> yPartitions = orderVJoin.Select(crv => crv.PointAtEnd.Y);

            IntersectPerpendicular(orderUJoin, yPartitions, SetDirection.Horizontal);
            IntersectPerpendicular(orderVJoin, xPartitions, SetDirection.Vertical);

            var allJoin = orderUJoin.Concat(orderVJoin);

            Brep[] pieces = boundBrep.Split(allJoin, tolerance);
            //group by center point projected to same axis and overlapping
            var uBounds = pieces.GroupBy(p => Math.Round(AreaMassProperties.Compute(p).Centroid.Y, 3)).Select(grp => grp.ToList()).ToList();
            var vBounds = pieces.GroupBy(p => Math.Round(AreaMassProperties.Compute(p).Centroid.X, 3)).Select(grp => grp.ToList()).ToList();

            uBounds.ForEach(grp => grp.OrderBy(b => Math.Round(AreaMassProperties.Compute(b).Centroid.X, 3)));
            vBounds.ForEach(grp => grp.OrderByDescending(b => Math.Round(AreaMassProperties.Compute(b).Centroid.Y, 3)));

            var allBounds = uBounds.Concat(vBounds);
            DataTree<Brep> groupBounds = new DataTree<Brep>();
            int path = 0;
            foreach (var list in allBounds)
            {
                groupBounds.AddRange(list, new GH_Path(path));
                path++;
            }

            //inverse transform
            foreach (var crv in allJoin)
            {
                crv.Transform(inverseCompare);
            }
            foreach (var group in allBounds)
            {
                foreach (var bound in group)
                {
                    bound.Transform(inverseCompare);
                }
            }

            //Convert to Bat Object
            int groupId = 0;
            List<ShatterBound> shatterBounds = new List<ShatterBound>();
            foreach (var group in uBounds)
            {
                ShatterBound uGroupObject = new ShatterBound(group, groupId, null);
                shatterBounds.Add(uGroupObject);
                groupId++;
            }
            foreach (var group in vBounds)
            {
                ShatterBound vGroupObject = new ShatterBound(group, null, groupId);
                shatterBounds.Add(vGroupObject);
                groupId++;
            }

            DA.SetDataList(0, allJoin);
            DA.SetDataTree(1, groupBounds);
            DA.SetDataList(1, shatterBounds);
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
        public List<Curve> CleanSplitCurve(List<Curve> curvesToClean, SetDirection direction)
        {
            switch (direction)
            {
                case SetDirection.Horizontal:
                    var uAxisGroup = curvesToClean.GroupBy(uPoints => uPoints.PointAtEnd.X,
                                                        uPoints => uPoints,
                                                        (key, g) => new { xId = key, crv = g.ToList() })
                                                .OrderBy(sets => sets.xId)
                                                .ToList();

                    List<double> moveId = new List<double>();
                    for (int i = 0; i < uAxisGroup.Count - 1; i++)
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
                    var mergeGroup = curvesToClean.GroupBy(uPoints => uPoints.PointAtEnd.X).Select(grp => grp.ToList()).ToList();
            
                    var uJoin = new List<Curve>();
                    foreach (var overlaps in mergeGroup)
                    {

                        List<Point3d> axisPoints = new List<Point3d>();
                        axisPoints.AddRange(overlaps.Select(o => o.PointAtEnd));
                        axisPoints.AddRange(overlaps.Select(o => o.PointAtStart));
                        IEnumerable<Point3d> orderedAxisPoint = axisPoints.OrderBy(points => points.Y);
                        List<Point3d> endPoints = new List<Point3d>
                        {
                            orderedAxisPoint.First(),
                            orderedAxisPoint.Last()
                        };
                        Curve plCurve = new PolylineCurve(endPoints).ToNurbsCurve();
                        uJoin.Add(plCurve);

                    }
                    return uJoin;
                    
                case SetDirection.Vertical:
                    var vAxisGroup = curvesToClean.GroupBy(uPoints => uPoints.PointAtEnd.Y,
                                                       uPoints => uPoints,
                                                       (key, g) => new { yId = key, crv = g.ToList() })
                                               .OrderBy(sets => sets.yId)
                                               .ToList();

                    List<double> moveYId = new List<double>();
                    for (int i = 0; i < vAxisGroup.Count - 1; i++)
                    {
                        if (!moveYId.Contains(vAxisGroup[i].yId))
                        {
                            for (int j = i + 1; j < vAxisGroup.Count; j++)
                            {
                                if (!moveYId.Contains(vAxisGroup[j].yId))
                                {
                                    double distance = Math.Abs(vAxisGroup[i].yId - vAxisGroup[j].yId);
                                    if (distance < 12)
                                    {
                                        Transform move = Transform.Translation(0, -distance, 0);
                                        foreach (var crv in vAxisGroup[j].crv)
                                        {
                                            crv.Transform(move);
                                            moveYId.Add(vAxisGroup[j].yId);
                                        }
                                    }
                                }

                            }
                        }
                    }
                    var mergeVGroup = curvesToClean.GroupBy(uPoints => uPoints.PointAtEnd.Y).Select(grp => grp.ToList()).ToList();

                    var vJoin = new List<Curve>();
                    foreach (var overlaps in mergeVGroup)
                    {

                        List<Point3d> axisPoints = new List<Point3d>();
                        axisPoints.AddRange(overlaps.Select(o => o.PointAtEnd));
                        axisPoints.AddRange(overlaps.Select(o => o.PointAtStart));
                        IEnumerable<Point3d> orderedAxisPoint = axisPoints.OrderBy(points => points.X);
                        List<Point3d> endPoints = new List<Point3d>
                        {
                            orderedAxisPoint.First(),
                            orderedAxisPoint.Last()
                        };
                        Curve plCurve = new PolylineCurve(endPoints).ToNurbsCurve();
                        vJoin.Add(plCurve);

                    }
                    return vJoin;
            }
            return null;
        }
        public void IntersectPerpendicular(List<Curve> curvetoIntersect, IEnumerable<double> partitionDistances, SetDirection direction)
        {
            for (int uCrv = 0; uCrv < curvetoIntersect.Count; uCrv++)
            {
                double start = default;
                double end = default;

                if (direction == SetDirection.Horizontal)
                {
                    start = curvetoIntersect[uCrv].PointAtStart.Y;
                    end = curvetoIntersect[uCrv].PointAtEnd.Y;

                }
                else 
                {
                    start = curvetoIntersect[uCrv].PointAtStart.X;
                    end = curvetoIntersect[uCrv].PointAtEnd.X;
                }
                List<Point3d> endPoints = new List<Point3d>();
                Point3d shiftStart;
                Point3d shiftEnd;
                if (!partitionDistances.Contains(start))
                {
                    double dimension = partitionDistances.OrderBy(dim => Math.Abs(dim - start)).First();
                    Vector3d move = default;
                    if (direction == SetDirection.Horizontal)
                    {
                        move = new Vector3d(0, dimension - start, 0);
                    }
                    else
                    {
                        move = new Vector3d(dimension - start, 0, 0);
                    }
                    shiftStart = move + curvetoIntersect[uCrv].PointAtStart;
                    endPoints.Add(shiftStart);
                }
                else
                {
                    shiftStart = curvetoIntersect[uCrv].PointAtStart;
                    endPoints.Add(shiftStart);
                }
                if (!partitionDistances.Contains(end))
                {
                    double dimension = partitionDistances.OrderBy(dim => Math.Abs(dim - end)).First();
                    Vector3d move = default;
                    if (direction == SetDirection.Horizontal)
                    {
                        move = new Vector3d(0, dimension - end, 0);
                    }
                    else
                    {
                        move = new Vector3d(dimension - end, 0, 0);
                    }
                    shiftEnd = move + curvetoIntersect[uCrv].PointAtEnd;
                    endPoints.Add(shiftEnd);
                }
                else
                {
                    shiftEnd = curvetoIntersect[uCrv].PointAtEnd;
                    endPoints.Add(shiftEnd);
                }
                if (!partitionDistances.Contains(start) || !partitionDistances.Contains(end))
                {
                    Curve plCurve = new PolylineCurve(endPoints).ToNurbsCurve();
                    curvetoIntersect[uCrv] = plCurve;
                }

            }
        }
        public enum SetDirection
        {
            Horizontal,
            Vertical,
        }
    }
}