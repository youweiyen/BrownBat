using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using BrownBat.Components;
using System.Linq;
using BrownBat.CalculateHelper;
using Dbscan.RBush;
using Grasshopper;
using Grasshopper.Kernel.Data;
using MIConvexHull;
using System.Diagnostics;

namespace BrownBat.Arrange
{
    public class GH_SelectValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_SelectValueArea class.
        /// </summary>
        public GH_SelectValue()
          : base("SelectValue_View", "SV",
              "Selecting area with chosen value and view cluster",
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
            pManager.AddIntegerParameter("MinPoints",
                                        "Min", 
                                        "Minimum points for DBSCAN calculation. Default set to 5", 
                                        GH_ParamAccess.item);
            pManager.AddIntegerParameter("Epsilon", 
                                        "E", 
                                        "Distance to determine same area points. Default set to 5",
                                        GH_ParamAccess.item);
            pManager[2].Optional = true;
            pManager[3].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Elements with selected heat value sorted", GH_ParamAccess.list);
            pManager.AddPointParameter("ClusterPoints", "CP", "Clustered Points", GH_ParamAccess.tree);
            pManager.AddPointParameter("ConvexPoints", "P", "Convex Points", GH_ParamAccess.list);
            pManager.AddLineParameter("axis", "a", "axis", GH_ParamAccess.list);
            pManager.AddTextParameter("stopwatch", "s", "clustertime", GH_ParamAccess.list);

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
            int inMinPoints = 5;
            DA.GetData(2, ref inMinPoints);
            int inEpsilon = 5;
            DA.GetData(2, ref inEpsilon);


            DataTree<Point3d> ClusteredPts = new DataTree<Point3d>();
            GH_Path path = new GH_Path();

            DataTree<Point3d> boundaryPoints = new DataTree<Point3d>();//visual
            List<Line> axisListView = new List<Line>();//visualize
            List<string> times = new List<string>();


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

                //double pixelCount = inMinArea / (element.PixelSize.Item1 * element.PixelSize.Item2);
                //int minPoints = (int)Math.Round(pixelCount);
                //double epsilon = 10;

                //stopwatch
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var clusters = DbscanRBush.CalculateClusters(fitID, inEpsilon, inMinPoints);


                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
                times.Add(elapsedTime);

                double pixelXSize = element.GeometryShape.Item1 / element.PixelShape.Item1;
                double pixelYSize = element.GeometryShape.Item2 / element.PixelShape.Item2;

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
                        Point3d rhinoPoint = new Point3d(element.Origin.OriginX + (point.Point.Y * pixelXSize), 
                                                            element.Origin.OriginY - (point.Point.X * pixelYSize), 0);
                        path = new GH_Path(c);
                        ClusteredPts.Add(rhinoPoint, path);

                    }
                    foreach (var hull in hullResult)//visualize
                    {
                        //convert to rhino points
                        Point3d rhinoPoint = new Point3d(element.Origin.OriginX + (hull.Y * pixelXSize),
                                                            element.Origin.OriginY - (hull.X * pixelYSize), 0);
                        
                        boundaryPoints.Add(rhinoPoint, path);

                    }
                }

                for (int g = 0; g < convexGroups.Count; g++)
                {
                    var rhinoPointGroup = convexGroups[g].Select(ver => new Point3d(element.Origin.OriginX + (ver.Y * pixelXSize),
                                                                        element.Origin.OriginY - (ver.X * pixelYSize),
                                                                        0));
                    var closePolyPoints = rhinoPointGroup.Concat(new[] { rhinoPointGroup.First()});
                    Polyline convexBoundary = new Polyline(closePolyPoints);

                    Point3d averagePoint = new Point3d(ClusteredPts.Branch(g).Select(pt => pt.X).Average(),
                                                       ClusteredPts.Branch(g).Select(pt => pt.Y).Average(),
                                                       ClusteredPts.Branch(g).Select(pt => pt.Z).Average());

                    Plane boundingPlane = AreaHelper.BoundingPlane(rhinoPointGroup, element.Origin);

                    //Line xAxis = AxisLine(rhinoPointGroup, boundingPlane.XAxis, element.GeometryShape.Item1, convexBoundary, tol);
                    //Line yAxis = AxisLine(rhinoPointGroup, boundingPlane.YAxis, element.GeometryShape.Item2, convexBoundary, tol);
                    //Rhino.Geometry.Intersect.Intersection.LineLine(xAxis, yAxis, out double xParameter, out double yParameter);
                    //Point3d center = xAxis.PointAt(xParameter);

                    Line xAxis = AreaHelper.AxisLineFromCenter(averagePoint, boundingPlane.XAxis, convexBoundary);
                    Line yAxis = AreaHelper.AxisLineFromCenter(averagePoint, boundingPlane.YAxis, convexBoundary);

                    HeatCluster heatCluster = new HeatCluster(element.Name, g, averagePoint, xAxis, yAxis);
                    heatClusterGroup.Add(g, heatCluster);
                    elementCluster.Add(element);

                    //what happens if I change the value of heat? will the old cluster still be there? 
                    //since heatcluster is a property of element

                    axisListView.Add(xAxis);//visualize
                    axisListView.Add(yAxis);//visualize
                }
                Element.SetHeatCluster(element, heatClusterGroup);

            }
            DA.SetDataList(0, elementCluster);
            DA.SetDataTree(1, ClusteredPts);
            DA.SetDataTree(2, boundaryPoints);
            DA.SetDataList(3, axisListView);
            DA.SetDataList(4, times);

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
    }
}