using BrownBat.Components;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.CalculateHelper
{
    public class AreaHelper
    {
        public static double PanelPixelArea(Element panel)
        {
            if (panel.GeometryShape == default)
            {
                Element.ModelShape(panel);
            }
            double width = panel.GeometryShape.Item1;
            double height = panel.GeometryShape.Item2;

            if (panel.PixelShape == default)
            {
                Element.CSVShape(panel);
            }
            int rowCount = panel.PixelShape.Item1;
            int columnCount = panel.PixelShape.Item2;

            double pixelWidthEdge = width / rowCount;
            double pixelHeightEdge = height / columnCount;

            double area = pixelWidthEdge*pixelHeightEdge;

            return area;
        }
        //public static double WallPixelArea(Structure wall)
        //{
        //    if (wall.GeometryShape == default)
        //    {
        //        Structure.WallShape(wall);
        //    }
        //    double width = wall.GeometryShape.Item1;
        //    double height = wall.GeometryShape.Item2;

        //    int segment = wall.PixelShape;

        //    double pixelWidthEdge = width / segment;
        //    double pixelHeightEdge = height / segment;

        //    double area = pixelWidthEdge * pixelHeightEdge;

        //    return area;
        //}
        public static Plane BoundingPlane(IEnumerable<Point3d> inputPoints, Plane inputPlane)
        {

            Rectangle3d minimumRectangle = MinBoundingBox(inputPoints, inputPlane);
            Plane boundingPlane = minimumRectangle.Plane;

            return boundingPlane;
        }
        public static Rectangle3d MinBoundingBox(IEnumerable<Point3d> inputPoints, Plane inputPlane) 
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

            return minimumRectangle;
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
        public static Line AxisLine(IEnumerable<Point3d> rhinoPointGroup, Vector3d direction, double length, Polyline convexBoundary, double tolerance)
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
        public static Line AxisLineFromCenter(Point3d centerPoint, Vector3d axisDirection, Polyline convexBoundary)
        {
            Point3d pointPositive = new Point3d();
            Point3d pointNegative = new Point3d();

            var tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Line lineX1 = new Line(centerPoint, axisDirection);
            var intersection = Rhino.Geometry.Intersect.Intersection.CurveLine(convexBoundary.ToNurbsCurve(), lineX1, tolerance, tolerance);
            if (intersection.Count > 1)
            {
                pointPositive = intersection[0].PointA;
                pointNegative = intersection[1].PointA;
            }
            Line lineX = new Line(pointPositive, pointNegative);

            return lineX;
        }

    }
}
