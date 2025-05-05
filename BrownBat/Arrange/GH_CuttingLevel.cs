using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BrownBat.CalculateHelper;
using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Geometry.Delaunay;
using NumSharp.Utilities;
using Rhino;
using Rhino.Collections;
using Rhino.Commands;
using Rhino.Geometry;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using static BrownBat.Arrange.GH_CuttingLevel;
using BrownBat.Components;

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
            pManager.AddCurveParameter("PatternCurve", "PC", "All defined pattern as curves", GH_ParamAccess.list);
            pManager.AddNumberParameter("MinDimension", "Min", "Smallest dimension of pattern" + 
                "Default set to 12", GH_ParamAccess.item);
            pManager.AddNumberParameter("MillDistance", "MDist", "Merge cutting edges within distance. Cannot be larger than min dimension" +
                "Default set to 6", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("PatternObject", "PO", "Pattern Object", GH_ParamAccess.list);
            pManager.AddCurveParameter("PatternFamily", "PF", "Pattern seperated into family", GH_ParamAccess.tree);
            pManager.AddBrepParameter("RectBounds", "RB", "Pattern Bounds", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> inCurve = new List<Curve>();
            double minLength = 12;
            double minDistance = 6;
            DA.GetDataList(0, inCurve);
            DA.GetData(1, ref minLength);
            DA.GetData(2, ref minDistance);

            var sortedCurves = inCurve.OrderByDescending(crv => AreaMassProperties.Compute(crv).Area);

            //remove small curve, get min bounding box
            List<Curve> validCurves = new List<Curve>();
            List<Plane> validPlane = new List<Plane>();
            foreach (Curve curve in sortedCurves)
            {
                curve.TryGetPolyline(out var curvePolyline);
                Point3d[] points = curvePolyline.ToArray();
                points = points.RemoveAt(0);
                Point3d centerPoint = new Point3d(points.Average(po => po.X),points.Average(po => po.Y), 0);
                Plane plane = new Plane(centerPoint, Vector3d.ZAxis);
                Rectangle3d minBox = AreaHelper.MinBoundingBox(points, plane);
                if(minBox.X.Length > minLength && minBox.Y.Length > minLength)
                {
                    validCurves.Add(curve);
                    Plane minPlane = minBox.Plane;
                    validPlane.Add(minPlane);
                }
            }
            double tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            foreach (Curve curve in validCurves)
            {
                CurveOrientation orientation = curve.ClosedCurveOrientation();
                if (orientation == CurveOrientation.CounterClockwise)
                {
                    curve.Reverse();
                }

            }
            var trees = new List<ColorPattern>();

            for (int c = 0; c < validCurves.Count; c++)
            {
                trees.Add(new ColorPattern(validCurves[c], validPlane[c], new List<ColorPattern>(), new List<ColorPattern>()));
            }

            for (int outer = 0; outer < trees.Count - 1; outer++)
            {
                for (int inner = outer + 1; inner < trees.Count; inner++)
                {
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
            //foreach (var group in layers)
            //{

            //    for (int i = 0; i < group.Count - 1; i++)
            //    {
            //        group[i].Shape.TryGetPolyline(out var root1Polyline);
            //        Point3d[] points1 = root1Polyline.ToArray();
            //        for (int j = i + 1; j < group.Count; j++)
            //        {
            //            group[j].Shape.TryGetPolyline(out var root2Polyline);
            //            Point3d[] points2 = root2Polyline.ToArray();

            //            List<Point3d> point1Shift = points1.ToList();
            //            List<Point3d> point2Shift = points2.ToList();

            //            //CompareCurve(points1, roots[j], minDistance, ref point1Shift, ref point2Shift);
            //            CompareCurve(points2, group[i], minDistance, ref point2Shift, ref point1Shift);
            //            int[] pointsSeq = SortPtsAlongCurve(point2Shift, group[j].Shape);
            //            Point3d[] sortPoints = new Point3d[point2Shift.Count];
            //            for (int seq = 0; seq < point2Shift.Count; seq++)
            //            {
            //                sortPoints[seq] = point2Shift[seq];
            //            }
            //            group[j].ShiftPoints = sortPoints.ToArray();
                        
            //        }
            //    }
            //}

            #endregion

            #region basicsegmentation
            ////get one min bounding box
            //Plane selectedPlane = layers[0][0].MinBoundingPlane;
            ////create bounding box
            //BoundingBox childBox = layers[0][0].Shape.GetBoundingBox(selectedPlane);
            //Rectangle3d childRect = new Rectangle3d(Plane.WorldXY, childBox.Max, childBox.Min);
            //Transform backToPosition = Transform.PlaneToPlane(Plane.WorldXY, selectedPlane);
            //childRect.Transform(backToPosition);

            //BoundingBox parentBox = layers[0][0].Parent[4].Shape.GetBoundingBox(selectedPlane);
            //Rectangle3d parentRect = new Rectangle3d(Plane.WorldXY, parentBox.Max, parentBox.Min);
            //parentRect.Transform(backToPosition);

            //List<Curve> parentGeometry = new List<Curve>
            //{
            //    parentRect.ToNurbsCurve()
            //};
            //var cuttingLines = new List<Curve>();

            //Line[] boundLines = childRect.ToPolyline().GetSegments();
            //foreach (Line line in boundLines)
            //{
            //    cuttingLines.Add(line.ToNurbsCurve().ExtendByLine(CurveEnd.Both, parentGeometry));
            //}
            //Brep parentBrep = Brep.CreateFromCornerPoints(parentRect.Corner(0), 
            //                            parentRect.Corner(1), 
            //                            parentRect.Corner(2), 
            //                            parentRect.Corner(3), tolerance);
            //Brep[] pieces = parentBrep.Split(cuttingLines, tolerance);
            ////translate to origin, align xy
            ////split breps
            ////group parallel cuts
            #endregion

            #region multiplesegmentation
            ////get one min bounding box
            //Plane useMinPlane = layers[0][0].MinBoundingPlane;
            ////create bounding box
            //DataTree<Curve> splits = new DataTree<Curve>();

            //int lay = 0;
            //foreach (var layer in layers)
            //{
            //    List<Rectangle3d> allChildRect = new List<Rectangle3d>();
            //    Transform inverseTransform = Transform.PlaneToPlane(Plane.WorldXY, useMinPlane);
            //    for (int i = 0; i < layer.Count; i++)
            //    {
            //        BoundingBox childBox = layer[i].Shape.GetBoundingBox(useMinPlane);
            //        Rectangle3d childRect = new Rectangle3d(Plane.WorldXY, childBox.Max, childBox.Min);
            //        childRect.Transform(inverseTransform);
            //        allChildRect.Add(childRect);
            //    }
            //    //merge close distances for same layer pieces
            //    if (allChildRect.Count > 1)
            //    {
            //        for (int kid = 0; kid < allChildRect.Count - 1; kid++)
            //        {
            //            for (int neighbor = kid + 1; neighbor < allChildRect.Count; neighbor++)
            //            {
            //                allChildRect[kid]
            //            }
            //        }
            //    }
            //    Rectangle3d parentRect = new Rectangle3d();
            //    if (layer[0].Parent.Count != 0)
            //    {
            //        layer[0].Parent.Reverse();
            //        BoundingBox parentBox = layer[0].Parent[0].Shape.GetBoundingBox(useMinPlane);
            //        parentRect = new Rectangle3d(Plane.WorldXY, parentBox.Max, parentBox.Min);
            //        parentRect.Transform(inverseTransform);
            //    }

            //    var cuttingLines = new List<Curve>();
            //    for (int r = 0; r < allChildRect.Count; r++)
            //    {
            //        var parentGeometry = allChildRect.Where((v, i) => i != r).Select(rec => rec.ToNurbsCurve()).ToList();
            //        if (layer[0].Parent.Count != 0)
            //        {
            //            parentGeometry.Add(parentRect.ToNurbsCurve());
            //        }
            //        Line[] boundLines = allChildRect[r].ToPolyline().GetSegments();
            //        foreach (Line line in boundLines)
            //        {
            //            cuttingLines.Add(line.ToNurbsCurve().ExtendByLine(CurveEnd.Both, parentGeometry));
            //        }
            //    }
            //    splits.AddRange(cuttingLines, new GH_Path(lay));
            //    lay++;
            //}

            //Brep parentBrep = Brep.CreateFromCornerPoints(parentRect.Corner(0),
            //                            parentRect.Corner(1),
            //                            parentRect.Corner(2),
            //                            parentRect.Corner(3), tolerance);

            //Brep[] pieces = parentBrep.Split(cuttingLines, tolerance);

            //translate to origin, align xy
            //split breps
            //group parallel cuts
            #endregion

            #region mergeboxsegmentation
            //get one min bounding box
            //Plane useMinPlane = layers[0][0].MinBoundingPlane;
            Plane useMinPlane = Plane.WorldXY;

            Transform inverseTransform = Transform.PlaneToPlane(Plane.WorldXY, useMinPlane);
            //create bounding box
             
            foreach (var pattern in trees)
            {
                BoundingBox childBox = pattern.Shape.GetBoundingBox(useMinPlane);
                Rectangle3d childRect = new Rectangle3d(Plane.WorldXY, childBox.Max, childBox.Min);
                childRect.Transform(inverseTransform);
                pattern.PlaneAlignedRect = childRect;
            }
            //same dad/mom
            var parentGroup = trees.Where(children => children.Parent.Count > 0).GroupBy(branch => branch.Parent.Last())
                    .Select(grp => grp.ToList())
                    .ToList();
            List<ColorPattern> ancestor = trees.Where(children => children.Parent.Count == 0).ToList();
            parentGroup.Add(ancestor);

            //visualize family
            DataTree<Curve> familyTree = new DataTree<Curve>();
            int p = 0;
            foreach (var l in parentGroup)
            {
                var curvesList = l.Select(crv => crv.Shape).ToList();
                familyTree.AddRange(curvesList, new GH_Path(p));
                p++;
            }

            #region Merge Same Layer Box
            var viewclosepts = new List<Point3d>();
            var mergebound = new List<Brep>();

            foreach (var family in parentGroup)
            {
                if(family.Count == 1)
                {
                    List<Point3d> singlePoints = family[0].PlaneAlignedRect.ToPolyline().ToList();
                    singlePoints.RemoveAt(0);

                    var singleOrder = singlePoints.OrderBy(x => Math.Atan2(x.X - singlePoints.Average(np => np.X), x.Y - singlePoints.Average(np => np.Y))).ToList();

                    Brep singleBrep = Brep.CreateFromCornerPoints(singleOrder[0],
                                                               singleOrder[1],
                                                               singleOrder[2],
                                                               singleOrder[3],
                                                               tolerance);
                    family[0].ShiftBound = singleBrep;
                }
                for (int kid = 0; kid < family.Count - 1; kid++)
                {
                    List<Point3d> kidPoints = family[kid].PlaneAlignedRect.ToPolyline().ToList();
                    kidPoints.RemoveAt(0);

                    var kidOrder = kidPoints.OrderBy(x => Math.Atan2(x.X - kidPoints.Average(np => np.X), x.Y - kidPoints.Average(np => np.Y))).ToList();

                    //int[] pointSort = SortPtsAlongCurve(kidPoints, stockBound);
                    //List<Point3d> kidOrder = new List<Point3d>
                    //    {
                    //        kidPoints[pointSort[0]],
                    //        kidPoints[pointSort[1]],
                    //        kidPoints[pointSort[2]],
                    //        kidPoints[pointSort[3]]
                    //    };

                    Brep kidBrep = Brep.CreateFromCornerPoints(kidOrder[0],
                                                               kidOrder[1],
                                                               kidOrder[2],
                                                               kidOrder[3],
                                                               tolerance);

                    for (int neighbor = kid + 1; neighbor < family.Count; neighbor++)
                    {
                        //use shifted geometry if it was shifted before
                        Brep neighborBrep;
                        List<Point3d> neighborPoints = new List<Point3d>();
                        List<Point3d> neighborOrder = new List<Point3d>();
                        if (family[neighbor].ShiftBound != null)
                        {
                            neighborBrep = family[neighbor].ShiftBound;
                            neighborPoints = neighborBrep.Vertices.Select(v => v.Location).ToList();
                            //int[] neighborSort = SortPtsAlongCurve(neighborPoints, stockBound);
                            //neighborOrder = new List<Point3d>
                            //{
                            //    neighborPoints[neighborSort[0]],
                            //    neighborPoints[neighborSort[1]],
                            //    neighborPoints[neighborSort[2]],
                            //    neighborPoints[neighborSort[3]]
                            //};
                            neighborOrder = neighborPoints.OrderBy(x => Math.Atan2(x.X - neighborPoints.Average(np => np.X), x.Y - neighborPoints.Average(np => np.Y))).ToList();

                        }
                        else
                        {
                            neighborPoints = family[neighbor].PlaneAlignedRect.ToPolyline().ToList();
                            neighborPoints.RemoveAt(0);
                            //int[] neighborSort = SortPtsAlongCurve(neighborPoints, stockBound);
                            //neighborOrder = new List<Point3d> 
                            //{
                            //    neighborPoints[neighborSort[0]],
                            //    neighborPoints[neighborSort[1]],
                            //    neighborPoints[neighborSort[2]],
                            //    neighborPoints[neighborSort[3]]
                            //};
                            neighborOrder = neighborPoints.OrderBy(x => Math.Atan2(x.X - neighborPoints.Average(np => np.X), x.Y - neighborPoints.Average(np => np.Y))).ToList();
                            //var neighborOrder = neighborPoints.OrderBy(x => Math.Atan2(x.X, x.Y)).ToList();


                            neighborBrep = Brep.CreateFromCornerPoints(neighborOrder[0],
                                                                            neighborOrder[1],
                                                                            neighborOrder[2],
                                                                            neighborOrder[3],
                                                                            tolerance);
                        }

                        for (int kedge = 0; kedge < kidBrep.Edges.Count; kedge++)
                        {
                            Curve kidEdge = kidBrep.Edges[kedge];
                            Vector3d kidDirection = new Vector3d(kidEdge.PointAtStart - kidEdge.PointAtEnd);
                            for (int edge = 0; edge < neighborBrep.Edges.Count; edge++)
                            {
                                Curve neighborEdge = neighborBrep.Edges[edge];
                                Vector3d neighborDirection = new Vector3d(neighborEdge.PointAtStart - neighborEdge.PointAtEnd);
                                int parallel = kidDirection.IsParallelTo(neighborDirection, tolerance);
                                if (parallel == 1 || parallel == -1)
                                {
                                    kidEdge.ClosestPoints(neighborEdge, out Point3d p1, out Point3d p2);
                                    if (p1.DistanceTo(p2) < minDistance)
                                    {

                                        Vector3d move = p1 - p2;
                                        int pointEndIndex = Point3dList.ClosestIndexInList(neighborOrder, neighborEdge.PointAtEnd);
                                        int pointStartIndex = Point3dList.ClosestIndexInList(neighborOrder, neighborEdge.PointAtStart);

                                        neighborOrder[pointEndIndex] = move + neighborOrder[pointEndIndex];
                                        neighborOrder[pointStartIndex] = move + neighborOrder[pointStartIndex];
                                    }
                                }
                            }
                        }
                        Brep neighborShift = Brep.CreateFromCornerPoints(neighborOrder[0],
                                                                        neighborOrder[1],
                                                                        neighborOrder[2],
                                                                        neighborOrder[3],
                                                                        tolerance);
                        family[neighbor].ShiftBound = neighborShift;

                    }
                    if (family[kid].ShiftBound == null)
                    {
                        family[kid].ShiftBound = kidBrep;

                    }
                }
            }
            #endregion
            #region Merge Child Layer Box

            for (int familyID = 0; familyID < parentGroup.Count-1; familyID ++)
            {
                Brep parentBrep = parentGroup[familyID][0].Parent.Last().ShiftBound;
                if (parentBrep == null)
                {
                    parentBrep = Brep.CreatePlanarBreps(parentGroup[familyID][0].Parent.Last().PlaneAlignedRect.ToNurbsCurve(), tolerance).First();
                }
                //compare parent to each child
                foreach (var child in parentGroup[familyID])
                {
                    var childOrder = new List<Point3d>();
                    Brep childBrep = new Brep();

                    if (child.ShiftBound != null)
                    {
                        childBrep = child.ShiftBound;
                        var childPoints = childBrep.Vertices.Select(v => v.Location);
                        //sorted points clockwise
                        childOrder = childPoints.OrderBy(x => Math.Atan2(x.X - childPoints.Average(np => np.X), x.Y - childPoints.Average(np => np.Y))).ToList();
                    }
                    else 
                    {
                        List<Point3d> neighborPoints = child.PlaneAlignedRect.ToPolyline().ToList();
                        neighborPoints.RemoveAt(0);

                        childOrder = neighborPoints.OrderBy(x => Math.Atan2(x.X - neighborPoints.Average(np => np.X), x.Y - neighborPoints.Average(np => np.Y))).ToList();

                        childBrep = Brep.CreateFromCornerPoints(childOrder[0],
                                                                childOrder[1],
                                                                childOrder[2],
                                                                childOrder[3],
                                                                tolerance);
                    }
                    
                    for (int pedge = 0;  pedge < parentBrep.Edges.Count; pedge++)
                    {
                        Curve parentEdge = parentBrep.Edges[pedge];
                        //move child edges
                        for (int cedge = 0; cedge < childBrep.Edges.Count; cedge++)
                        {
                            Curve childEdge = childBrep.Edges[cedge];
                            Vector3d parentDirection = new Vector3d(parentEdge.PointAtStart - parentEdge.PointAtEnd);
                            Vector3d childDirection = new Vector3d(childEdge.PointAtStart - childEdge.PointAtEnd);

                            int parallel = parentDirection.IsParallelTo(childDirection, tolerance);
                            if (parallel == 1 || parallel == -1)
                            {
                                parentEdge.ClosestPoints(childEdge, out Point3d p1, out Point3d p2);
                                if (p1.DistanceTo(p2) < minDistance)
                                {
                                    Transform move = Transform.Translation(p1 - p2);
                                    int pointEndIndex = Point3dList.ClosestIndexInList(childOrder, childEdge.PointAtEnd);
                                    int pointStartIndex = Point3dList.ClosestIndexInList(childOrder, childEdge.PointAtStart);

                                    childOrder[pointEndIndex] = move * childOrder[pointEndIndex];
                                    childOrder[pointStartIndex] = move * childOrder[pointStartIndex];

                                }
                            }
                        }
                    }
                    Brep childShift = Brep.CreateFromCornerPoints(childOrder[0],
                                                                  childOrder[1],
                                                                  childOrder[2],
                                                                  childOrder[3],
                                                                  tolerance);
                    child.ShiftBound = childShift;
                }

            }
            //visualizeBounds
            foreach (var thing in trees)
            {
                 mergebound.Add(thing.ShiftBound);

            }
            //add shiftbound geometry for ones without parent
            foreach (var grand in ancestor)
            {
                List<Point3d> ancestorPoints = grand.PlaneAlignedRect.ToPolyline().ToList();
                ancestorPoints.RemoveAt(0);

                var ancestorOrder = ancestorPoints.OrderBy(x => Math.Atan2(x.X - ancestorPoints.Average(np => np.X), x.Y - ancestorPoints.Average(np => np.Y))).ToList();

                var ancestorBrep = Brep.CreateFromCornerPoints(ancestorOrder[0],
                                                                ancestorOrder[1],
                                                                ancestorOrder[2],
                                                                ancestorOrder[3],
                                                                tolerance);
            }

            #endregion
            //trim the parent with child
            List<Brep[]>trimPieces = new List<Brep[]>();
            foreach (var grand in ancestor)
            {
                RecursiveTrim(grand, tolerance);
            }

            foreach (var family in parentGroup)
            {
                foreach (var member in family)
                {
                    if (member.TrimBound.Length == 0)
                    {
                        Brep[] originalBound = new Brep[1]{ member.ShiftBound };
                        member.TrimBound = originalBound;
                    }
                }
            }
            List<ColorPattern> colorPattern = new List<ColorPattern>();
            foreach (var pattern in trees)
            {
                colorPattern.Add(pattern);

            }
            #endregion

            DA.SetDataList(0, colorPattern);
            DA.SetDataTree(1, familyTree);
            DA.SetDataList(3, mergebound);


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

        public void CompareCurve(Point3d[] points, ColorPattern compareRoot, double minDistance, ref List<Point3d> point1Shift, ref List<Point3d> point2Shift)
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
       
        public Brep[] TrimBounds(ColorPattern parent, double tolerance)
        {
            //get direct children
            var cuttingBound = parent.Children.Where(c => c.Parent.Last() == parent).Select(t => t.ShiftBound);
            var unionCut = Brep.CreateBooleanUnion(cuttingBound, tolerance);
            List<Brep> trimStock = new List<Brep>
                {
                    parent.ShiftBound
                };
            Brep[] trimmedParent = Brep.CreateBooleanDifference(trimStock, unionCut, tolerance);
            parent.TrimBound = trimmedParent;
            return trimmedParent;
        }
        public void RecursiveTrim(ColorPattern parent, double tolerance)
        {
            parent.TrimBound = TrimBounds(parent, tolerance);
            if (parent.Children.Count != 0)
            {
                foreach (var child in parent.Children)
                {
                    if (child.TrimBound == null)
                    {
                        RecursiveTrim(child, tolerance);
                    }
                }
            }
        }
    }
}