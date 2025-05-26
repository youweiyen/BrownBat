using System;
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
            pManager.AddGenericParameter("Bounds", "B", "Bound Object with group data", GH_ParamAccess.list);
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
            List<ShatterBound> inBound = new List<ShatterBound>();
            Curve inStock = default; 
            GH_Structure<IGH_Goo> inConductivity = new GH_Structure<IGH_Goo>();
            List<int> inStart = new List<int>();
            List<int> inDirection = new List<int>();

            DA.GetDataList(0, inBound);
            DA.GetDataTree(1, out inConductivity);
            DA.GetData(2, ref inStock);
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
                    value[t].CastTo<double>(out double temperature);
                    valueToDouble[t] = temperature;
                }
                stockConductivity.Add(valueToDouble);
            }

            //join shatter
            List<CuttingBound> cuttingBounds = new List<CuttingBound>();
            //method
            var grouped = new ConcurrentBag<List<ShatterBound>>();
            var visited = new ConcurrentDictionary<ShatterBound, bool>();

            inBound.

            //Parallel.ForEach(inBound, rect =>
            //{
            //    if (visited.ContainsKey(rect)) return;

            //    var group = FloodFill(rect, inBound, visited);
            //    if (group.Count > 1)
            //        grouped.Add(group);
            //});

            List<ShatterBound> shatterGroup = new List<ShatterBound>();
            CuttingBound shatterToCut = new CuttingBound(shatterGroup);
            cuttingBounds.Add(shatterToCut);

            //calculate homogenity
            foreach (var bound in cuttingBounds)
            {
                Brep joinBrep = Brep.JoinBreps(bound.Bounds.Select(b => b.Bound), tolerance).First();
                
                //get topleft bottom right startend domain
                Curve referenceStock = inStock.DuplicateCurve();
                referenceStock.Transform(moveToWorld);
                referenceStock.TryGetPolyline(out var stockPoly);
                Point3d[] stockPoints = stockPoly.ToArray();
                stockPoints.RemoveAt(0);
                //0 = topleft; 1 = bottomleft; 2 = topright; 3 = bottomright
                var stockCorners = stockPoints.OrderBy(pts => pts.X).ThenBy(pts => pts.Y).ToList();
                
                //get bound domian in start end
                Brep moveBrep = joinBrep.DuplicateBrep();
                moveBrep.Transform(moveToWorld);
                Point3d[] boundVertices = moveBrep.DuplicateVertices();
                var boundCorners = boundVertices.OrderBy(pts => pts.X).ThenBy(pts => pts.Y).ToList();

                double stockXDistance = stockCorners[0].DistanceTo(stockCorners[2]);
                double stockYDistance = stockCorners[0].DistanceTo(stockCorners[1]);

                double xMin = boundCorners[0].X - stockCorners[0].X;
                double xMax = boundCorners[2].X - stockCorners[0].X;
                double xMinInStock = xMin < 0 ? 0: xMin;
                double xMaxInStock = xMax > stockXDistance ? stockXDistance : xMax ;
                int xMinInterval = (int) Math.Round(xMinInStock/stockXDistance);
                int xMaxInterval = (int) Math.Round(xMaxInStock/stockXDistance);

                double yMin = boundCorners[0].Y - stockCorners[0].Y;
                double yMax = boundCorners[1].Y - stockCorners[0].Y;
                double yMinInStock = yMin < 0 ? 0: yMin;
                double yMaxInStock = yMax > stockYDistance ? stockYDistance : yMax ;
                int yMinInterval = (int) Math.Round(yMinInStock/stockYDistance);
                int yMaxInterval = (int) Math.Round(yMaxInStock/stockYDistance);

                var dataInBound = stockConductivity.Select(list => list.GetRange(xMinInterval, xMaxInterval))
                                                   .ToList()
                                                   .GetRange(yMinInterval, yMaxInterval);


                CuttingBound.SetBoundData(bound, dataInBound);

                double[] flattenData = dataInBound.SelectMany(i => i).ToArray();
                double topFifth = CuttingBound.Percentile(flattenData, 5);
                double lowFifth = CuttingBound.Percentile(flattenData, 95);
                double mean = flattenData.Average();

                CuttingBound.SetMean(bound, mean);
                CuttingBound.SetTopFifth(bound, topFifth);
                CuttingBound.SetLowFifth(bound, lowFifth);

            }

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

        //public List<ShatterBound> FloodFill(ShatterBound start, List<ShatterBound> allRects, ConcurrentDictionary<ShatterBound, bool> visited)
        //{
        //    var group = new List<ShatterBound>();
        //    var queue = new Queue<ShatterBound>();
        //    queue.Enqueue(start);
        //    visited.TryAdd(start, true);

        //    while (queue.Count > 0)
        //    {
        //        var current = queue.Dequeue();
        //        group.Add(current);

        //        foreach (var other in allRects)
        //        {
        //            if (!visited.ContainsKey(other) && current.IsAdjacent(other))
        //            {
        //                var testGroup = new List<ShatterBound>(group) { other };

        //                visited.TryAdd(other, true);
        //                queue.Enqueue(other);
                        
        //            }
        //        }
        //    }

        //    return group;
        //}
        enum JoinDirection 
        {
            Left = 0, Right = 1, Top = 2, Bottom = 3
        }

    }
}