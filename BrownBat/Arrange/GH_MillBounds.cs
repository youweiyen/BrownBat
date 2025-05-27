using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BrownBat.Components;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using NumSharp.Utilities;
using Rhino;
using Rhino.Geometry;
using BrownBat.CalculateHelper;

namespace BrownBat.Arrange
{
    public class GH_MillBounds : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_MillBounds class.
        /// </summary>
        public GH_MillBounds()
          : base("MillBounds", "MB",
              "Bounds to join for milling",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ShatterGroups", "G", "Shatter Objects with group data", GH_ParamAccess.list);
            pManager.AddCurveParameter("Stock", "S", "Stock Boundary Curve", GH_ParamAccess.item);
            pManager.AddNumberParameter("Conductivity", "C", "Conductivity in pixel array position", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("JoinStart", "JO", "Start of Shatter Group. 2 parameters, true false for starting position.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("JoinDirection", "JD", "UV Direction. 4 Parameters, left = 0, right = 1, top = 2, bottom = 3.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Stock", "S", "Result Stock as surface", GH_ParamAccess.list);
            pManager.AddNumberParameter("Mean", "M", "Cutting Bound Mean", GH_ParamAccess.list);
            pManager.AddNumberParameter("TopFifth", "T", "Cutting Bound Top fifth percentile", GH_ParamAccess.list);
            pManager.AddNumberParameter("LowFifth", "L", "Cutting Bound Low fifth percentile", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<ShatterGroup> inBound = new List<ShatterGroup>();
            Curve inStock = default; 
            GH_Structure<GH_Number> inConductivity = new GH_Structure<GH_Number>();
            List<int> inStart = new List<int>();
            List<int> inDirection = new List<int>();

            DA.GetDataList(0, inBound);
            DA.GetData(1, ref inStock);
            DA.GetDataTree(2, out inConductivity);
            DA.GetDataList(3, inStart);
            DA.GetDataList(4, inDirection);

            Transform moveToWorld = Transform.PlaneToPlane(inBound[0].CutPlane, Plane.WorldXY);
            try { moveToWorld.TryGetInverse(out Transform moveBack); }
            catch { throw new Exception("could not get inverse matrix"); }

            double tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            List<List<double>> stockConductivity = new List<List<double>>();
            for (int branch = 0; branch < inConductivity.Branches.Count; branch++)
            {
                var value = inConductivity.Branches[branch];
                List<double> valueToDouble = new List<double>();
                for (int t = 0; t < value.Count; t++)
                {
                    double temperature = value[t].Value;
                    valueToDouble.Add(temperature);
                }
                stockConductivity.Add(valueToDouble);
            }


            //join shatter
            List<CuttingBound> cuttingBounds = new List<CuttingBound>();
            //method
            var grouped = new ConcurrentBag<List<Brep>>();
            var visited = new ConcurrentDictionary<Brep, bool>();

            var allBounds = inBound.Select(bound => bound.Bounds).SelectMany(br => br).Distinct(new BrepComparer()).ToList();

            if (allBounds.Count() != inStart.Count)
            { throw new Exception("Start parameter does not match object count!"); }

            var startBounds = allBounds.Zip(inStart, (br, st) => (br, st)).Where(grp => grp.st == 1).Select(grp => grp.br).ToList();
            var startDirection = inDirection.Zip(inStart, (dir, st) => (dir, st)).Where(grp => grp.st == 1).Select(grp => grp.dir).ToList();

            List<Brep> startBoundsDup = new List<Brep>();
            foreach (Brep b in startBounds)
            {
                startBoundsDup.Add(b.DuplicateBrep());
            }
            var boundWithDirection = startBoundsDup.Zip(startDirection, (brep, dir) => (brep, dir)).ToList();

            Parallel.ForEach(boundWithDirection, rect =>
            {
                if (visited.ContainsKey(rect.brep)) return;

                var neighbors = new List<Brep>();
                var allNeighbor = new List<Brep>();
                int rectIndex = 0;
                switch (rect.dir)
                {
                    case (int)JoinDirection.Left:

                        allNeighbor = inBound.Where(grp => CustomExtensions.Contains(grp.Bounds, rect.brep))
                                           .Where(nbr => nbr.UGroupId != null)
                                           .First()
                                           .Bounds;
                        allNeighbor.Reverse();
                        rectIndex = allNeighbor.IndexOf(rect.brep);
                        neighbors = allNeighbor.GetRange(rectIndex, allNeighbor.Count - 1);
                        break;
                    case (int)JoinDirection.Right:

                        allNeighbor = inBound.Where(grp => CustomExtensions.Contains(grp.Bounds, rect.brep))
                                           .Where(nbr => nbr.UGroupId != null)
                                           .First()
                                           .Bounds;
                        rectIndex = allNeighbor.IndexOf(rect.brep);
                        neighbors = allNeighbor.GetRange(rectIndex, allNeighbor.Count - 1);
                        break;
                    case (int)JoinDirection.Top:
                        allNeighbor = inBound.Where(grp => CustomExtensions.Contains(grp.Bounds, rect.brep))
                                             .Where(nbr => nbr.VGroupId != null)
                                             .First()
                                             .Bounds;
                        allNeighbor.Reverse();
                        rectIndex = allNeighbor.IndexOf(rect.brep);
                        neighbors = allNeighbor.GetRange(rectIndex, allNeighbor.Count - 1);
                        break;
                    case (int)JoinDirection.Bottom:
                        neighbors = inBound.Where(grp => CustomExtensions.Contains(grp.Bounds, rect.brep))
                                           .Where(nbr => nbr.VGroupId != null)
                                           .First()
                                           .Bounds;
                        rectIndex = allNeighbor.IndexOf(rect.brep);
                        neighbors = allNeighbor.GetRange(rectIndex, allNeighbor.Count - 1);
                        break;
                }

                var group = FloodFillParallel(rect.brep, neighbors, visited);
                grouped.Add(group);
            });

            //var shatterGroup = grouped.ToList();
            //foreach (var group in shatterGroup)
            //{
            //    CuttingBound shatterToCut = new CuttingBound(group);
            //    cuttingBounds.Add(shatterToCut);
            //}
            //var shatter = shatterGroup.Select(grp => Brep.JoinBreps(grp, tolerance)).ToList();

            //calculate homogenity
            //foreach (var bound in cuttingBounds)
            //{
            //    Brep joinBrep = Brep.JoinBreps(bound.Bounds, tolerance).First();

            //    //get topleft bottom right startend domain
            //    Curve referenceStock = inStock.DuplicateCurve();
            //    referenceStock.Transform(moveToWorld);
            //    referenceStock.TryGetPolyline(out var stockPoly);
            //    List<Point3d> stockPoints = stockPoly.ToList();
            //    stockPoints.RemoveAt(0);
            //    //0 = topleft; 1 = bottomleft; 2 = topright; 3 = bottomright
            //    var stockCorners = stockPoints.OrderBy(pts => pts.X).ThenBy(pts => pts.Y).ToList();

            //    //get bound domian in start end
            //    Brep moveBrep = joinBrep.DuplicateBrep();
            //    moveBrep.Transform(moveToWorld);
            //    Point3d[] boundVertices = moveBrep.DuplicateVertices();
            //    var boundCorners = boundVertices.OrderBy(pts => pts.X).ThenBy(pts => pts.Y).ToList();

            //    double stockXDistance = stockCorners[0].DistanceTo(stockCorners[2]);
            //    double stockYDistance = stockCorners[0].DistanceTo(stockCorners[1]);

            //    double xMin = boundCorners[0].X - stockCorners[0].X;
            //    double xMax = boundCorners[2].X - stockCorners[0].X;
            //    double xMinInStock = xMin < 0 ? 0: xMin;
            //    double xMaxInStock = xMax > stockXDistance ? stockXDistance : xMax ;
            //    int xMinInterval = (int) Math.Round(xMinInStock/stockXDistance);
            //    int xMaxInterval = (int) Math.Round(xMaxInStock/stockXDistance);

            //    double yMin = boundCorners[0].Y - stockCorners[0].Y;
            //    double yMax = boundCorners[1].Y - stockCorners[0].Y;
            //    double yMinInStock = yMin < 0 ? 0: yMin;
            //    double yMaxInStock = yMax > stockYDistance ? stockYDistance : yMax ;
            //    int yMinInterval = (int) Math.Round(yMinInStock/stockYDistance);
            //    int yMaxInterval = (int) Math.Round(yMaxInStock/stockYDistance);

            //    var dataInBound = stockConductivity.Select(list => list.GetRange(xMinInterval, xMaxInterval))
            //                                       .ToList()
            //                                       .GetRange(yMinInterval, yMaxInterval);


            //    CuttingBound.SetBoundData(bound, dataInBound);

            //    double[] flattenData = dataInBound.SelectMany(i => i).ToArray();
            //    double topFifth = CuttingBound.Percentile(flattenData, 5);
            //    double lowFifth = CuttingBound.Percentile(flattenData, 95);
            //    double mean = flattenData.Average();

            //    CuttingBound.SetMean(bound, mean);
            //    CuttingBound.SetTopFifth(bound, topFifth);
            //    CuttingBound.SetLowFifth(bound, lowFifth);

            //}

            DA.SetDataList(0, allBounds);
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
            get { return new Guid("BE7A3EFA-5087-4FAA-B8D9-EDF812F5BC37"); }
        }

        public List<Brep> FloodFillParallel(Brep start, List<Brep> groupRects, ConcurrentDictionary<Brep, bool> visited)
        {
            var group = new List<Brep>();
            var queue = new Queue<Brep>();
            queue.Enqueue(start);
            visited.TryAdd(start, true);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                group.Add(current);

                foreach (var other in groupRects)
                {
                    if (!visited.ContainsKey(other))
                    {
                        var testGroup = new List<Brep>(group) { other };

                        visited.TryAdd(other, true);
                        queue.Enqueue(other);

                    }
                    else { break; }
                }
            }

            return group;
        }
        public List<Brep> FloodFill(Brep start, List<Brep> groupRects, ConcurrentDictionary<Brep, bool> visited)
        {
            var group = new List<Brep>();
            var queue = new Queue<Brep>();
            queue.Enqueue(start);
            visited.TryAdd(start, true);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                group.Add(current);

                foreach (var other in groupRects)
                {
                    if (!visited.ContainsKey(other))
                    {
                        var testGroup = new List<Brep>(group) { other };

                        visited.TryAdd(other, true);
                        queue.Enqueue(other);

                    }
                    else { break; }
                }
            }

            return group;
        }
        enum JoinDirection
        {
            Left = 0, Right = 1, Top = 2, Bottom = 3
        }
        enum JoinStart
        {
            NotStart = 0, Start = 1
        }

        class BrepComparer : IEqualityComparer<Brep>
        {
            // Products are equal if their names and product numbers are equal.
            public bool Equals(Brep x, Brep y)
            {

                //Check whether the compared objects reference the same data.
                if (Object.ReferenceEquals(x, y)) return true;

                //Check whether any of the compared objects is null.
                if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                    return false;

                //Check whether the products' properties are equal.
                return x == y && x == y;
            }

            // If Equals() returns true for a pair of objects
            // then GetHashCode() must return the same value for these objects.

            public int GetHashCode(Brep product)
            {
                //Check whether the object is null
                if (Object.ReferenceEquals(product, null)) return 0;

                //Get hash code for the Name field if it is not null.
                int hashProductName = product == null ? 0 : product.GetHashCode();

                //Get hash code for the Code field.
                int hashProductCode = product.GetHashCode();

                //Calculate the hash code for the product.
                return hashProductName ^ hashProductCode;
            }

        }

    }
}