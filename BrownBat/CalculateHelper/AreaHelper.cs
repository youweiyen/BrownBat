using BrownBat.Components;
using Ed.Eto;
using Eto.Forms;
using Rhino.Collections;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using static Rhino.UI.Internal.OptionsPages.AppearanceViewModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

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
        public static Line AxisLineFromCenter(Point3d centerPoint, Vector3d axisDirection, Curve convexBoundary)
        {
            Point3d pointPositive = new Point3d();
            Point3d pointNegative = new Point3d();

            var tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Line lineX1 = new Line(centerPoint, axisDirection);
            var intersection = Rhino.Geometry.Intersect.Intersection.CurveLine(convexBoundary, lineX1, tolerance, tolerance);
            if (intersection.Count > 1)
            {
                pointPositive = intersection[0].PointA;
                pointNegative = intersection[1].PointA;
            }
            Line lineX = new Line(pointPositive, pointNegative);

            return lineX;
        }
        public static IEnumerable<Point3d> RemoveDuplicatePoints(IEnumerable<Point3d> points, double threshold)
        {
            
            Func<double, double, double, double, double> distance
                = (x0, y0, x1, y1) =>
                    Math.Sqrt(Math.Pow(x1 - x0, 2.0) + Math.Pow(y1 - y0, 2.0));

            var result = points.Skip(1).Aggregate(points.Take(1).ToList(), (xys, xy) =>
            {
                if (xys.All(xy2 => distance(xy.X, xy.Y, xy2.X, xy2.Y) >= threshold))
                {
                    xys.Add(xy);
                }
                return xys;
            });
            return result;
        }
       
        public static IEnumerable<DbscanPoint> ReduceDbscanGrid(List<DbscanPoint> points, int npartitions)
        {
            double max_x = 0, max_y = 0;
            double min_x = double.MaxValue, min_y = double.MaxValue;

            // Find the bounding box of the points
            foreach (var point in points)
            {
                if (point.Point.X > max_x)
                    max_x = point.Point.X;
                if (point.Point.Y < min_x)
                    min_x = point.Point.X;
                if (point.Point.Y > max_y)
                    max_y = point.Point.Y;
                if (point.Point.Y < min_y)
                    min_y = point.Point.Y;
            }

            // Get the X and Y axis lengths of the paritions
            double partition_length_x = (max_x - min_x) / npartitions;
            double partition_length_y = (max_y - min_y) / npartitions;

            List<DbscanPoint> result = new List<DbscanPoint>();
            // Reduce the points to one in each grid partition
            for (int n = 0; n < npartitions; n++)
            {
                // Get the boundary of this grid paritition
                double min_X = min_x + (n * partition_length_x);
                double min_Y = min_y + (n * partition_length_y);
                double max_X = min_x + ((n + 1) * partition_length_x);
                double max_Y = min_y + ((n + 1) * partition_length_y);

                bool reduce = false; // set to true after finding the first point in the partition
                foreach (var point in points)
                {
                    // the point is in the grid parition
                    if (point.Point.X >= min_X && point.Point.X < max_X &&
                            point.Point.Y >= min_Y && point.Point.Y < max_Y)
                    {
                        // first point found
                        if (reduce is false)
                        {
                            reduce = true;
                            result.Add(point);
                            continue;
                        }
                         // remove the point from the list
                    }
                }
            }
            return result;
        }
        public static List<Point3d> DouglasPeucker(List<Point3d> points, double tolerance)
        {
            if (points == null || points.Count < 3) { return points; }

            int firstPoint = 0;
            int lastPoint = points.Count - 1;
            List<int> pointIndexsToKeep = new List<int>
            {
                //Add the first and last index to the keepers
                firstPoint,
                lastPoint
            };

            //The first and the last point cannot be the same
            while (points[firstPoint].Equals(points[lastPoint]))
            {
                lastPoint--;
            }

            DouglasPeuckerReduction(points, firstPoint, lastPoint,
            tolerance, ref pointIndexsToKeep);

            List<Point3d> returnPoints = new List<Point3d>();
            pointIndexsToKeep.Sort();
            foreach (int index in pointIndexsToKeep)
            {
                returnPoints.Add(points[index]);
            }

            return returnPoints;
        }
        private static void DouglasPeuckerReduction(List<Point3d> points, int firstPoint, int lastPoint, double tolerance, ref List<int> pointIndexsToKeep)
        {
            double maxDistance = 0;
            int indexFarthest = 0;

            for (int index = firstPoint; index < lastPoint; index++)
            {
                double distance = PerpendicularDistance
                    (points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0)
            {
                //Add the largest point that exceeds the tolerance
                pointIndexsToKeep.Add(indexFarthest);

                DouglasPeuckerReduction(points, firstPoint,
                indexFarthest, tolerance, ref pointIndexsToKeep);
                DouglasPeuckerReduction(points, indexFarthest,
                lastPoint, tolerance, ref pointIndexsToKeep);
            }
        }
        public static double PerpendicularDistance(Point3d Point1, Point3d Point2, Point3d Point)
        {
            //Area = |(1/2)(x1y2 + x2y3 + x3y1 - x2y1 - x3y2 - x1y3)|   *Area of triangle
            //Base = v((x1-x2)²+(x1-x2)²)                               *Base of Triangle*
            //Area = .5*Base*H                                          *Solve for height
            //Height = Area/.5/Base

            double area = Math.Abs(.5 * (Point1.X * Point2.Y + Point2.X *
            Point.Y + Point.X * Point1.Y - Point2.X * Point1.Y - Point.X *
            Point2.Y - Point1.X * Point.Y));
            double bottom = Math.Sqrt(Math.Pow(Point1.X - Point2.X, 2) +
            Math.Pow(Point1.Y - Point2.Y, 2));
            double height = area / bottom * 2;

            return height;
        }
        public static Plane PlacePlane(Brep placeBound)
        {
            BrepVertexList profileVertexList = placeBound.Vertices;

            List<Point3d> profileVertices = new List<Point3d>();
            for (int i = 0; i < profileVertexList.Count; i++)
            {
                Point3d vertex = profileVertexList[i].Location;
                profileVertices.Add(vertex);
            }
            double xStartProfile = profileVertices.OrderBy(v => v.X).Select(v => v.X).First();
            double yStartProfile = profileVertices.OrderByDescending(v => v.Y).Select(v => v.Y).First();
            double ySmallest = profileVertices.OrderBy(v => v.Y).Select(v => v.Y).First();
            double xLargest = profileVertices.OrderByDescending(v => v.X).Select(v => v.X).First();

            Vector3d xDirection = new Vector3d(xLargest - xStartProfile, 0, 0);
            Vector3d yDirection = new Vector3d(0, yStartProfile - ySmallest, 0);

            Point3d profileStart = new Point3d(xStartProfile, yStartProfile, 0);
            Plane originPlane = new Plane(profileStart, xDirection, yDirection);
            return originPlane;
        }
    }
}
