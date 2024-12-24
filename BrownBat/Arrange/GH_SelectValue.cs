using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using BrownBat.Components;
using System.Linq;
using System.Data.Common.CommandTrees;
using BrownBat.CalculateHelper;
using Dbscan;
using Grasshopper.GUI;
using Dbscan.RBush;
using Grasshopper;
using Grasshopper.Kernel.Data;
using MIConvexHull;
using Eto.Forms;
using System.Configuration;
using System.Xml.Linq;

namespace BrownBat.Arrange
{
    public class GH_SelectValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_SelectValueArea class.
        /// </summary>
        public GH_SelectValue()
          : base("SelectValue", "SV",
              "Selecting area with chosen value",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Imported Element geometry with data", GH_ParamAccess.list);
            pManager.AddNumberParameter("Value", "V", "Value to draw out area", GH_ParamAccess.item);
            pManager.AddNumberParameter("MinArea", "Min", "Minimum area of cluster. Default set to 1mm2", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Elements with selected heat value sorted", GH_ParamAccess.list);
            pManager.AddPointParameter("ClusterPoints", "CP", "Clustered Points", GH_ParamAccess.tree);
            pManager.AddPointParameter("OverPoints", "P", "Over Points", GH_ParamAccess.list);
            pManager.AddLineParameter("axis", "a", "axis", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inData = new List<Element>();
            DA.GetDataList(0, inData);
            double inValue = default;
            DA.GetData(1, ref inValue);
            double inMinArea = 1;
            DA.GetData(2, ref inMinArea);


            DataTree<Point3d> ClusteredPts = new DataTree<Point3d>();
            GH_Path path = new GH_Path();

            DataTree<Point3d> boundaryPoints = new DataTree<Point3d>();//visual
            List<Line> axisListView = new List<Line>();//visualize


            var tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            List<Element> elementCluster = new List<Element>();

            foreach (Element element in inData)
            {
                List<DbscanPoint> fitID = new List<DbscanPoint>();

                var data = element.PixelConductivity;
                for (int row = 0; row < data.Count; row++)
                {
                    for (int col = 0; col < data[row].Count(); col++)
                    {
                        if (data[row][col] > inValue)
                        {
                            var id = new DbscanPoint(row, col);
                            fitID.Add(id);
                        }
                    }
                }
                double pixelCount = 1 / (element.PixelSize.Item1 * element.PixelSize.Item2);
                int minPoints = (int)Math.Round(pixelCount);
                double epsilon = 5;
                var clusters = DbscanRBush.CalculateClusters(fitID, epsilon, minPoints);

                double pixelXSize = element.PixelShape.Item1 / element.GeometryShape.Item1;
                double pixelYSize = element.PixelShape.Item2 / element.GeometryShape.Item2;

                List<ConvexVertex[]> convexGroups = new List<ConvexVertex[]>();
                Dictionary<int, HeatCluster> heatClusterGroup = new Dictionary<int, HeatCluster>();

                for (int c = 0; c < clusters.Clusters.Count; c++)
                {
                    var points = clusters.Clusters[c].Objects;
                    var vpoint = points.Select(p => new ConvexVertex(p.Point.X, p.Point.Y)).ToArray();
                    //var vpoint = new ConvexVertex[] { new ConvexVertex(point.Point.X, point.Point.Y) };
                    var hullResult = ConvexHull.Create2D(vpoint, 1e-10).Result.ToArray();

                    convexGroups.Add(hullResult);

                    foreach (var point in points)//visualize
                    {
                        //convert to rhino points
                        Point3d rhinoPoint = new Point3d(element.Origin.OriginX + (point.Point.X * pixelXSize), 
                                                            element.Origin.OriginY - (point.Point.Y * pixelYSize), 0);
                        path = new GH_Path(c);
                        ClusteredPts.Add(rhinoPoint, path);

                    }
                    foreach (var hull in hullResult)//visualize
                    {
                        //convert to rhino points
                        Point3d rhinoPoint = new Point3d(element.Origin.OriginX + (hull.X * pixelXSize),
                                                            element.Origin.OriginY - (hull.Y * pixelYSize), 0);
                        path = new GH_Path(c);
                        boundaryPoints.Add(rhinoPoint, path);

                    }
                }

                Vector3d uDirection = element.Origin.XAxis;
                Vector3d vDirection = element.Origin.YAxis;

                for (int g = 0; g < convexGroups.Count; g++)
                {
                    var rhinoPointGroup = convexGroups[g].Select(ver => new Point3d(element.Origin.OriginX + (ver.X * pixelXSize),
                                                                        element.Origin.OriginY - (ver.Y * pixelYSize),
                                                                        0));

                    Polyline convexBoundary = new Polyline(rhinoPointGroup);
                    
                    //Min boundingbox to find axis vector direction
                    Plane boundingPlane = BoundingPlane(rhinoPointGroup, element.Origin);

                    Line xAxis = AxisLine(rhinoPointGroup, boundingPlane.XAxis, element.GeometryShape.Item1, convexBoundary, tol);
                    Line yAxis = AxisLine(rhinoPointGroup, boundingPlane.YAxis, element.GeometryShape.Item2, convexBoundary, tol);
                    Rhino.Geometry.Intersect.Intersection.LineLine(xAxis, yAxis, out double xParameter, out double yParameter);
                    Point3d center = xAxis.PointAt(xParameter);
                    HeatCluster heatCluster = new HeatCluster(element.Name, g, center, xAxis, yAxis);
                    heatClusterGroup.Add(g, heatCluster);
                    elementCluster.Add(element);
                    //what happens if I change the value of heat? will the old cluster still be there? 
                    //since heatcluster is a property of element
                    axisListView.Add(xAxis);
                    axisListView.Add(yAxis);
                }
                Element.SetHeatCluster(element, heatClusterGroup);

            }
            DA.SetDataList(0, elementCluster);
            DA.SetDataTree(1, ClusteredPts);
            DA.SetDataTree(2, boundaryPoints);
            DA.SetDataList(3, axisListView);
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
            get { return new Guid("3A58AB18-598B-4A2C-B57A-CCA47E079470"); }
        }

        public Line AxisLine(IEnumerable<Point3d> rhinoPointGroup, Vector3d direction, double length, Polyline convexBoundary,double tolerance)
        {
            var points = new List<(Point3d, Point3d)>();
            foreach (Point3d pt in rhinoPointGroup)
            {
                Line line = new Line(pt, direction);
                var intersection = Rhino.Geometry.Intersect.Intersection.CurveLine(convexBoundary.ToNurbsCurve(), line, tolerance, tolerance);
                if (intersection.Count > 0)
                {
                    if (intersection.Count > 1) { points.Add((pt, intersection[1].PointA)); }
                    else { points.Add((pt, intersection[0].PointA)); }
                }
                else
                {
                    line = new Line(pt, -direction);
                    intersection = Rhino.Geometry.Intersect.Intersection.CurveLine(convexBoundary.ToNurbsCurve(), line, tolerance, tolerance);
                    if (intersection.Count > 0)
                    {
                        if (intersection.Count > 1) { points.Add((pt, intersection[1].PointA)); }
                        else { points.Add((pt, intersection[0].PointA)); }
                    }
                    else
                    {
                        points.Add((pt, pt));
                    }
                }
            }
            var furthestPoints = points.OrderByDescending(p => p.Item1.DistanceTo(p.Item2)).First();
            Line axis = new Line(furthestPoints.Item1, furthestPoints.Item2);
            return axis;
        }
        public Plane BoundingPlane(IEnumerable<Point3d> inputPoints, Plane inputPlane)
        {

            // We transform the geometry to the XY for the purpose of calculation
            Transform toXY = Transform.PlaneToPlane(inputPlane, Plane.WorldXY);
            Transform toInputPlane;
            if (!toXY.TryGetInverse(out toInputPlane))
                throw new Exception("Something went wrong with the projection to XY plane!");


            // Converting input points to a 2D Vector and applying the transformation
            List<Vector2d> inputVectors = new List<Vector2d>();
            foreach (Point3d pt in inputPoints)
            {
                pt.Transform(toXY);
                inputVectors.Add(new Vector2d(pt.X, pt.Y));
            }

            Rectangle3d minimumRectangle = new Rectangle3d();
            double minimumAngle = 0;

            // For each edge of the convex hull
            for (int i = 0; i < inputVectors.Count; i++)
            {
                int nextIndex = i + 1;
                Vector2d current = inputVectors[i];
                Vector2d next = inputVectors[nextIndex % inputVectors.Count];


                Point3d start = new Point3d(current.X, current.Y, 0);
                Point3d end = new Point3d(next.X, next.Y, 0);
                Line segment = new Line(start, end);

                // getting limits
                double top = double.MinValue;
                double bottom = double.MaxValue;
                double left = double.MaxValue;
                double right = double.MinValue;

                // Angle to X Axis
                double angle = AngleToXAxis(segment);

                // Rotate every point and get min and max values for each direction
                foreach (Vector2d v in inputVectors)
                {
                    Vector2d rotatedVec = RotateToXAxis(v, angle);

                    top = Math.Max(top, rotatedVec.Y);
                    bottom = Math.Min(bottom, rotatedVec.Y);

                    left = Math.Min(left, rotatedVec.X);
                    right = Math.Max(right, rotatedVec.X);
                }

                // Create axis aligned bounding box
                Rectangle3d rec = new Rectangle3d(Plane.WorldXY, new Point3d(left, bottom, 0), new Point3d(right, top, 0));

                if (minimumRectangle.IsValid == false || minimumRectangle.Area > rec.Area)
                {
                    minimumRectangle = rec;
                    minimumAngle = angle;
                }
            }

            // Rotate the rectangle to fit the points
            Transform finalRotation = Transform.Rotation(-minimumAngle, Point3d.Origin);
            minimumRectangle.Transform(finalRotation);
            
            // Transform the rectangle to it's initial orientation
            minimumRectangle.Transform(toInputPlane);
            Plane boundingPlane = minimumRectangle.Plane;

            return boundingPlane;
        }
        static double AngleToXAxis(Line s)
        {
            Vector3d delta = s.From - s.To;
            return -Math.Atan(delta.Y / delta.X);
        }
        static Vector2d RotateToXAxis(Vector2d v, double angle)
        {
            var newX = v.X * Math.Cos(angle) - v.Y * Math.Sin(angle);
            var newY = v.X * Math.Sin(angle) + v.Y * Math.Cos(angle);

            return new Vector2d(newX, newY);
        }
    }
}