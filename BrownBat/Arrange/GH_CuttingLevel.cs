using System;
using System.Collections.Generic;
using System.Linq;
using BrownBat.CalculateHelper;
using Grasshopper.Kernel;
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
            pManager.AddCurveParameter("Patttern", "P", "All defined pattern as curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimum", "Min", "Smallest dimension of pattern", GH_ParamAccess.item);
            pManager.AddNumberParameter("BlobDistance", "Dist", "Merge Curve smallest distance", GH_ParamAccess.item);
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
            double minDistance = default;
            DA.GetDataList(0, inCurve);
            DA.GetData(1, ref minLength);
            DA.GetData(2, ref minDistance);

            var sortedCurves = inCurve.OrderByDescending(crv => AreaMassProperties.Compute(crv).Area);
            double tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
           
            var trees = new List<CurveTree>();
            var roots = new List<CurveTree>();

            foreach (Curve crv in sortedCurves)
            {
                trees.Add(new CurveTree(crv, new List<CurveTree>(), new List<CurveTree>()));
            }

            for (int outer = 0; outer < trees.Count - 1; outer++)
            {
                for (int inner = outer + 1; inner < trees.Count; inner++)
                {

                    Point3d scaleCenter = AreaMassProperties.Compute(trees[inner].Shape).Centroid;

                    Curve offsetCurve = trees[inner].Shape.Offset(scaleCenter, Vector3d.ZAxis, 0.1, tolerance, CurveOffsetCornerStyle.Sharp)
                                                        .OrderByDescending(crv => crv.GetLength())
                                                        .First();

                    RegionContainment contains = Curve.PlanarClosedCurveRelationship(trees[outer].Shape, offsetCurve, Plane.WorldXY, tolerance);

                    if (contains == RegionContainment.BInsideA)
                    {
                        trees[outer].Children.Add(trees[inner]);
                        trees[inner].Parent.Add(trees[outer]);
                    }
                }
                //Add root curve(curves with no parent)

            }
            foreach(CurveTree root in trees)
            {
                if (root.Parent.Count == 0)
                {
                    roots.Add(root);
                }
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
            for (int i = 0; i < roots.Count - 1; i++)
            {
                roots[i].Shape.TryGetPolyline(out var root1Polyline);
                Point3d[] points1 = root1Polyline.ToArray();
                for (int j = i + 1; j < roots.Count; j++)
                {
                    roots[j].Shape.TryGetPolyline(out var root2Polyline);
                    Point3d[] points2 = root2Polyline.ToArray();

                    List<Point3d> point1Shift = points1.ToList();
                    List<Point3d> point2Shift = points2.ToList();

                    CompareCurve(points1, roots[j], minDistance, ref point1Shift, ref point2Shift);
                    CompareCurve(points2, roots[i], minDistance, ref point2Shift, ref point1Shift);


                    int[] points1Seq = SortPtsAlongCurve(point1Shift, roots[i].Shape);
                    Point3d[] sort1Points = new Point3d[point1Shift.Count];
                    for (int seq = 0; seq < point1Shift.Count; seq++)
                    {
                        sort1Points[seq] = point1Shift[seq];
                    }
                    int[] points2Seq = SortPtsAlongCurve(point2Shift, roots[j].Shape);
                    Point3d[] sort2Points = new Point3d[point2Shift.Count];
                    for (int seq = 0; seq < point2Shift.Count; seq++)
                    {
                        sort2Points[seq] = point2Shift[seq];
                    }

                    roots[i].ShiftPoints = point1Shift.ToArray();
                    roots[j].ShiftPoints = point2Shift.ToArray();
                }
            }
            #endregion
            Curve pCurve1 = new PolylineCurve(trees[0].ShiftPoints).ToNurbsCurve();
            Curve pCurve2 = new PolylineCurve(trees[1].ShiftPoints).ToNurbsCurve();

            patternCurves.Add(pCurve1);
            patternCurves.Add(pCurve2);

            foreach (CurveTree pattern1 in trees)
            {
                foreach(CurveTree pattern2 in trees)
                {
                    if (pattern1.Shape != pattern2.Shape)
                    {
                        //neighbor
                        //if (pattern1.Parent.First() != pattern2.Parent.First() && pattern1.Parent.First().Shape != pattern2.Shape)
                        //{

                        //}
                        
                    }
                }
                //List<CurveTree> parents = pattern.Parent;
                //for (int p = 0; p < parents.Count; p++)
                //{
                //    if (parents[p].NewShape == null)
                //    {
                //        parents[p].NewShape = Curve.CreateBooleanDifference(parents[p].Shape, grandchild.Shape, tolerance);
                //    }
                //    else
                //    {
                        
                //    }
                //}
                //grandchild.Shape.TryGetPolyline(out Polyline rootPolyline);
                //Point3d[] points = rootPolyline.ToArray();
                //Rectangle3d minBox = AreaHelper.MinBoundingBox(points, Plane.WorldXY);
                //if (minBox.X.Length > minLength && minBox.Y.Length > minLength)
                //{
                //    Curve[] trimmedCurves = Curve.CreateBooleanDifference(grandchild.Shape, grandchild.Shape, tolerance);
                //}
                //else 
                //{

                //}
            }
            DA.SetDataList(0, patternCurves);

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
                    point1Shift.Insert(index, midPoint);
                    
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
    }
}