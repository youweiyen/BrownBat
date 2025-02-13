using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Dbscan.RBush;
using BrownBat.CalculateHelper;
using System.Linq;
using MIConvexHull;
using BrownBat.Components;
using Grasshopper;
using Grasshopper.Kernel.Data;

namespace BrownBat.Arrange
{
    public class GH_NextLayerBound : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_NextLayerBound class.
        /// </summary>
        public GH_NextLayerBound()
          : base("NextLayerBound", "NL",
              "Next layer placement with higher heiarchy",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("LowPoint", "LP", "Low temperature points to draw placement boundary", GH_ParamAccess.list);
            pManager.AddNumberParameter("PointValue", "PV", "Low temperature points value", GH_ParamAccess.list);            
            pManager.AddNumberParameter("Value", "V", "Value to cluster points for placing with higher heiarchy", GH_ParamAccess.item);
            pManager.AddNumberParameter("Epsilon",
                                        "E",
                                        "Distance to determine same area points. " +
                                        "Enter pixel divide distance",
                                        GH_ParamAccess.item);
            pManager.AddIntegerParameter("MinPoints",
                            "Min",
                            "Minimum points for DBSCAN calculation. Default set to 5",
                            GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("PassiveBoundary", "PB", "Boundary to place first for next layer Elements", GH_ParamAccess.list);
            pManager.AddLineParameter("BoundaryAxis", "BA", "Boundary axis for placement rotation", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> inPoint = new List<Point3d>();
            List<double>inPointValue = new List<double>();
            double inValue = default;
            int inMinPoints = 5;
            double inEpsilon = default;
            List<Element> inElement = new List<Element>();

            DA.GetDataList(0, inPoint);
            DA.GetDataList(1, inPointValue);
            DA.GetData(2, ref inValue);
            DA.GetData(3, ref inEpsilon);
            DA.GetData(4, ref inMinPoints);

            var lowerPointValueSet = inPointValue.Zip(inPoint, (pv, pt) => new { value = pv, point = pt })
                                                        .Where(pair => pair.value < inValue);
                                                        
            IEnumerable<Point3d> lowerPoints = lowerPointValueSet.Select(pair => pair.point);
            IEnumerable<double> lowerValues = lowerPointValueSet.Select(pair => pair.value);

            IEnumerable<DbscanPoint> pointsToCluster = lowerPoints.Select(p => new DbscanPoint(p.X, p.Y));
            
            var clusters = DbscanRBush.CalculateClusters(pointsToCluster, inEpsilon*2.2, inMinPoints);

            List<Polyline> polylines = new List<Polyline>();
            DataTree<Line> boundaryAxis = new DataTree<Line>();
            List<double> clusterAverage = new List<double>();
            List<(Line, Line)> axisGroup = new List<(Line, Line)>();

            for (int c = 0; c < clusters.Clusters.Count; c++)
            {
                var points = clusters.Clusters[c].Objects;
                var vpoint = points.Select(p => new ConvexVertex(p.Point.X, p.Point.Y)).ToArray();
                ConvexVertex[] hullResult = ConvexHull.Create2D(vpoint, 1e-10).Result.ToArray();
                var rhinoConvex = hullResult.Select(ver => new Point3d(ver.X, ver.Y, 0.0));
                var closePolyPoints = rhinoConvex.Concat(new[] { rhinoConvex.First() });
                Polyline convexBoundary = new Polyline(closePolyPoints);
                polylines.Add(convexBoundary);

                Curve boundaryCurve = convexBoundary.ToPolylineCurve();

                var rhinoCluster = points.Select(ver => new Point3d(ver.Point.X, ver.Point.Y, 0));
                Point3d averagePoint = new Point3d(rhinoCluster.Select(pt => pt.X).Average(),
                                    rhinoCluster.Select(pt => pt.Y).Average(),
                                    rhinoCluster.Select(pt => pt.Z).Average());

                Plane boundingPlane = AreaHelper.BoundingPlane(rhinoConvex, new Plane(averagePoint, Vector3d.XAxis, Vector3d.YAxis));

                Line xAxis = AreaHelper.AxisLineFromCenter(averagePoint, boundingPlane.XAxis, boundaryCurve);
                Line yAxis = AreaHelper.AxisLineFromCenter(averagePoint, boundingPlane.YAxis, boundaryCurve);

                (Line, Line) axisPair = (xAxis, yAxis);
                axisGroup.Add(axisPair);

                List<double> pointValues = new List<double>();
                foreach(var p in points)
                {
                    double value = lowerPointValueSet.FirstOrDefault(pair => pair.point.X == p.Point.X && pair.point.Y == p.Point.Y).value;
                    pointValues.Add(value);                
                }
                double averageValue = pointValues.Sum() / points.Count();
                clusterAverage.Add(averageValue);
            }
            var orderedAxis = clusterAverage.Zip(axisGroup, (av, ax) => new {average = av, axis = ax}).OrderBy(pair => pair.average).Select(pair => pair.axis).ToList();
            for (int i = 0; i < orderedAxis.Count; i++)
            { 
                GH_Path path = new GH_Path(i);
                boundaryAxis.Add(orderedAxis[i].Item1, path);
                boundaryAxis.Add(orderedAxis[i].Item2, path);
            }

            DA.SetDataList(0, polylines);
            DA.SetDataTree(1, boundaryAxis);

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
            get { return new Guid("CC0B0AFA-C94F-4809-BBA7-9E87590C1D4E"); }
        }
    }
}