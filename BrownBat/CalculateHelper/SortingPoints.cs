using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.CalculateHelper
{
    public class SortingPoints
    {
        public IEnumerable<Point3d> SortClockwise(IEnumerable<Point3d> insertPoints)
        {
            var singleOrder = insertPoints.OrderBy(x => Math.Atan2(x.X - insertPoints.Average(np => np.X), x.Y - insertPoints.Average(np => np.Y))).ToList();
            return singleOrder;
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
