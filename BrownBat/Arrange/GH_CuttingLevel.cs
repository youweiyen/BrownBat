using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BrownBat.CalculateHelper;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Geometry.Delaunay;
using NumSharp.Utilities;
using Rhino;
using Rhino.Collections;
using Rhino.Commands;
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
            pManager.AddCurveParameter("Pattern", "P", "All defined pattern as curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimum", "Min", "Smallest dimension of pattern", GH_ParamAccess.item);
            pManager.AddNumberParameter("BlobDistance", "Dist", "Merge Curve smallest distance" +
                "Default set to 15", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Pattern", "P", "Pattern level", GH_ParamAccess.list);
            pManager.AddCurveParameter("trees", "t", "t", GH_ParamAccess.tree);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> inCurve = new List<Curve>();
            double minLength = default;
            double minDistance = 15;
            DA.GetDataList(0, inCurve);
            DA.GetData(1, ref minLength);
            DA.GetData(2, ref minDistance);

            //remove small curve
            List<Curve> validCurves = new List<Curve>();
            foreach (Curve curve in inCurve)
            {
                curve.TryGetPolyline(out var curvePolyline);
                Point3d[] points = curvePolyline.ToArray();
                points = points.RemoveAt(0);
                Point3d centerPoint = new Point3d(points.Average(po => po.X),points.Average(po => po.Y), 0);
                Plane plane = new Plane(centerPoint, Vector3d.ZAxis);
                Rectangle3d minBox = AreaHelper.MinBoundingBox(points, plane);
                if(minBox.X.Length > minLength || minBox.Y.Length > minLength)
                {
                    validCurves.Add(curve);
                }
            }
            var sortedCurves = validCurves.OrderByDescending(crv => AreaMassProperties.Compute(crv).Area);
            double tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            foreach (Curve curve in sortedCurves)
            {
                CurveOrientation orientation = curve.ClosedCurveOrientation();
                if (orientation == CurveOrientation.CounterClockwise)
                {
                    curve.Reverse();
                }

            }

            var trees = new List<CurveTree>();

            foreach (Curve crv in sortedCurves)
            {
                trees.Add(new CurveTree(crv, new List<CurveTree>(), new List<CurveTree>()));
            }

            for (int outer = 0; outer < trees.Count - 1; outer++)
            {
                for (int inner = outer + 1; inner < trees.Count; inner++)
                {

                    //Point3d scaleCenter = AreaMassProperties.Compute(trees[inner].Shape).Centroid;

                    //Curve offsetCurve = trees[inner].Shape.Offset(scaleCenter, Vector3d.ZAxis, 0.2, tolerance, CurveOffsetCornerStyle.Sharp)
                    //                                    .OrderByDescending(crv => crv.GetLength())
                    //                                    .First();
                    Curve offsetCurve = trees[inner].Shape.Offset(Plane.WorldXY, 2, tolerance, CurveOffsetCornerStyle.Sharp)
                                    .OrderByDescending(crv => crv.GetLength())
                                    .First();

                    RegionContainment contains = Curve.PlanarClosedCurveRelationship(trees[outer].Shape, offsetCurve, Plane.WorldXY, tolerance);

                    if (contains == RegionContainment.BInsideA)
                    {
                        trees[outer].Children.Add(trees[inner]);
                        trees[inner].Parent.Add(trees[outer]);
                    }
                }

            }
            //layer them out
            var layers = trees.GroupBy(branch => branch.Parent.Count)
                                .Select(grp => grp.ToList())
                                .ToList();

            DataTree<Curve> rhinoTree = new DataTree<Curve>();
            int p = 0;
            foreach (var l in layers)
            {
                var curvesList = l.Select(lay => lay.Shape).ToList();
                rhinoTree.AddRange(curvesList, new GH_Path(p));
                p++;
            }

            List<Curve> patternCurves = new List<Curve>();
            #region opt1
            //for (int i = 0; i < roots.Count - 1; i++)
            //{
            //    roots[i].Shape.TryGetPolyline(out var root1Polyline);
            //    Point3d[] points1 = root1Polyline.ToArray();
            //    for (int j = i + 1; j < roots.Count; j++)
            //    {
            //        roots[j].Shape.TryGetPolyline(out var root2Polyline);
            //        Point3d[] points2 = root2Polyline.ToArray();

            //        List<Point3d> point1Shift = points1.ToList();
            //        List<Point3d> point2Shift = points2.ToList();

            //        CompareCurve(points1, roots[j], minDistance, ref point1Shift, ref point2Shift);
            //        CompareCurve(points2, roots[i], minDistance, ref point1Shift, ref point2Shift);


            //        int[] points1Seq = SortPtsAlongCurve(point1Shift, roots[i].Shape);
            //        Point3d[] sort1Points = new Point3d[point1Shift.Count];
            //        for (int seq = 0; seq < point1Shift.Count; seq++)
            //        {
            //            sort1Points[seq] = point1Shift[seq];
            //        }
            //        int[] points2Seq = SortPtsAlongCurve(point2Shift, roots[j].Shape);
            //        Point3d[] sort2Points = new Point3d[point2Shift.Count];
            //        for (int seq = 0; seq < point2Shift.Count; seq++)
            //        {
            //            sort2Points[seq] = point2Shift[seq];
            //        }

            //        roots[i].ShiftPoints = sort1Points.ToArray();
            //        roots[j].ShiftPoints = sort2Points.ToArray();
            //    }
            //}
            #endregion
            #region opt2
            foreach (var group in layers)
            {

                for (int i = 0; i < group.Count - 1; i++)
                {
                    group[i].Shape.TryGetPolyline(out var root1Polyline);
                    Point3d[] points1 = root1Polyline.ToArray();
                    for (int j = i + 1; j < group.Count; j++)
                    {
                        group[j].Shape.TryGetPolyline(out var root2Polyline);
                        Point3d[] points2 = root2Polyline.ToArray();

                        List<Point3d> point1Shift = points1.ToList();
                        List<Point3d> point2Shift = points2.ToList();

                        //CompareCurve(points1, roots[j], minDistance, ref point1Shift, ref point2Shift);
                        CompareCurve(points2, group[i], minDistance, ref point2Shift, ref point1Shift);
                        int[] pointsSeq = SortPtsAlongCurve(point2Shift, group[j].Shape);
                        Point3d[] sortPoints = new Point3d[point2Shift.Count];
                        for (int seq = 0; seq < point2Shift.Count; seq++)
                        {
                            sortPoints[seq] = point2Shift[seq];
                        }
                        group[j].ShiftPoints = sortPoints.ToArray();
                        
                    }
                }
            }

            #endregion
            foreach (var tree in trees)
            {
                if (tree.ShiftPoints == null)
                {
                    patternCurves.Add(tree.Shape);
                }
                else
                {
                    //add starting point to close curve
                    patternCurves.Add(new PolylineCurve(tree.ShiftPoints).ToNurbsCurve());
                }

            }
            DA.SetDataList(0, patternCurves);
            DA.SetDataTree(1, rhinoTree);

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
            public Point3d[] ShiftPoints { get; set; }
            public List<CurveTree> Children { get; set; }
            public List<CurveTree> Parent { get; set; }

            public CurveTree(Curve shape, List<CurveTree> children, List<CurveTree> parent)
            {
                Shape = shape;
                Children = children;
                Parent = parent;
            }
        }
        public void CompareCurve(Point3d[] points, CurveTree compareRoot, double minDistance, ref List<Point3d> point1Shift, ref List<Point3d> point2Shift)
        {
            foreach (Point3d p in points)
            {
                Curve compareCurve = compareRoot.Shape;
                compareCurve.ClosestPoint(p, out double param);
                Point3d closestPoint = compareCurve.PointAt(param);
                if (p.DistanceTo(closestPoint) < minDistance)
                {
                    Point3d midPoint = new Point3d((p.X + closestPoint.X) / 2,
                                                    (p.Y + closestPoint.Y) / 2,
                                                    (p.Z + closestPoint.Z) / 2);
                    point1Shift.Remove(p);
                    int index = Array.FindIndex(points, po => po == p);
                    point1Shift.Insert(index, closestPoint);
                }
            }
        }
        public void IntersectingSurface(Point3d[] points, CurveTree compareRoot, double minDistance)
        {
            foreach (Point3d p in points)
            {
                Curve compareCurve = compareRoot.Shape;
                compareCurve.ClosestPoint(p, out double param);
                Point3d closestPoint = compareCurve.PointAt(param);
                if (p.DistanceTo(closestPoint) < minDistance)
                {



                }
            }
        }
        public int[] SortPtsAlongCurve(List<Point3d> pts, Curve crv)
        {
            int L = pts.Count;
            int[] iA = new int[L]; double[] tA = new double[L];
            for (int i = 0; i < L; i++)
            {
                double t;
                crv.ClosestPoint(pts[i], out t);
                iA[i] = i; tA[i] = t;
            }
            Array.Sort(tA, iA);
            return iA;
        }
        public class Edge
        {
            public Point3d A { get; set; }
            public Point3d B { get; set; }
        }
        public class AlphaShape
        {
            public List<Edge> BorderEdges { get; private set; }

            public AlphaShape(List<Point3d> points, double alpha)
            {
                // 0. error checking, init
                if (points == null || points.Count < 2) { throw new ArgumentException("AlphaShape needs at least 2 points"); }
                BorderEdges = new List<Edge>();
                var alpha_2 = alpha * alpha;

                // 1. run through all pairs of points
                for (int i = 0; i < points.Count - 1; i++)
                {
                    for (int j = i + 1; j < points.Count; j++)
                    {
                        if (points[i] == points[j]) { throw new ArgumentException("AlphaShape needs pairwise distinct points"); } // alternatively, continue
                        var dist = Dist(points[i], points[j]);
                        if (dist > 2 * alpha) { continue; } // circle fits between points ==> p_i, p_j can't be alpha-exposed                    

                        double x1 = points[i].X, x2 = points[j].X, y1 = points[i].Y, y2 = points[j].Y; // for clarity & brevity

                        var mid = new Point3d((x1 + x2) / 2, (y1 + y2) / 2, 0);

                        // find two circles that contain p_i and p_j; note that center1 == center2 if dist == 2*alpha
                        var center1 = new Point3d(
                            mid.X + (double)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (y1 - y2) / dist,
                            mid.Y + (double)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (x2 - x1) / dist,
                            0);

                        var center2 = new Point3d(
                            mid.X - (double)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (y1 - y2) / dist,
                            mid.Y - (double)Math.Sqrt(alpha_2 - (dist / 2) * (dist / 2)) * (x2 - x1) / dist,
                            0);

                        // check if one of the circles is alpha-exposed, i.e. no other point lies in it
                        bool c1_empty = true, c2_empty = true;
                        for (int k = 0; k < points.Count && (c1_empty || c2_empty); k++)
                        {
                            if (points[k] == points[i] || points[k] == points[j]) { continue; }

                            if ((center1.X - points[k].X) * (center1.X - points[k].X) + (center1.Y - points[k].Y) * (center1.Y - points[k].Y) < alpha_2)
                            {
                                c1_empty = false;
                            }

                            if ((center2.X - points[k].X) * (center2.X - points[k].X) + (center2.Y - points[k].Y) * (center2.Y - points[k].Y) < alpha_2)
                            {
                                c2_empty = false;
                            }
                        }

                        if (c1_empty || c2_empty)
                        {
                            // yup!
                            BorderEdges.Add(new Edge() { A = points[i], B = points[j] });
                        }
                    }
                }
            }

            // Euclidian distance between A and B
            public static double Dist(Point3d A, Point3d B)
            {
                return (double)Math.Sqrt((A.X - B.X) * (A.X - B.X) + (A.Y - B.Y) * (A.Y - B.Y));
            }
        }
    }
}