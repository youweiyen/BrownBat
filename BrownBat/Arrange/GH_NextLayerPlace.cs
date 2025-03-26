//using System;
//using System.Collections.Generic;
//using Grasshopper;
//using Grasshopper.Kernel;
//using Grasshopper.Kernel.Data;
//using Grasshopper.Kernel.Types;
//using Rhino.Geometry;
//using BrownBat.Components;
//using System.Xml.Linq;
//using Rhino.Geometry.Intersect;
//using System.Diagnostics.Eventing.Reader;
//using System.Linq;
//using BrownBat.CalculateHelper;

//namespace BrownBat.Arrange
//{
//    public class GH_NextLayerPlace : GH_Component
//    {
//        /// <summary>
//        /// Initializes a new instance of the GH_NextLayerPlace class.
//        /// </summary>
//        public GH_NextLayerPlace()
//          : base("PlacePassive", "PP",
//              "Place the passive Elements on the next layer",
//              "BrownBat", "Arrange")
//        {
//        }

//        /// <summary>
//        /// Registers all the input parameters for this component.
//        /// </summary>
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddGenericParameter("Element", "E", "Element to place on passive positions", GH_ParamAccess.list);
//            pManager.AddLineParameter("BoundaryAxis", "BA", "Boundary axis to place", GH_ParamAccess.tree);
//        }

//        /// <summary>
//        /// Registers all the output parameters for this component.
//        /// </summary>
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddGenericParameter("Element", "E", "Placed Element", GH_ParamAccess.list);
//            pManager.AddTransformParameter("Transformation", "T", "Element Transformation", GH_ParamAccess.list);
//        }

//        /// <summary>
//        /// This is the method that actually does the work.
//        /// </summary>
//        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {
//            List<Element> inElement = new List<Element>();
//            GH_Structure<GH_Line> inAxis = new GH_Structure<GH_Line>();

//            DA.GetDataList(0, inElement);
//            DA.GetDataTree(1, out inAxis);

//            double tolerance = 10;
//            var elementHasCluster = inElement.Where(e => e.HeatClusterGroup != null);
//            var elementOrderClusterArea = elementHasCluster.OrderByDescending(e => e.HeatClusterGroup
//                                                                        .Sum(hc => hc.Value.XAxis.Length * hc.Value.YAxis.Length))
//                                                        .ToList();

//            double longestClusterLength = elementHasCluster.Max(e=> e.HeatClusterGroup
//                                                    .Select(hc => new[] { hc.Value.XAxis.Length, hc.Value.YAxis.Length }
//                                                            .ToList()
//                                                            .Max())
//                                                    .Max());
//            Dictionary<int, List<Pair<string, List<Transform>>>> fitAreaPair = new Dictionary<int, List<Pair<string, List<Transform>>>>();

//            List<PlaceAxis> axisList = new List<PlaceAxis>();
//            for (int branch = 0; branch < inAxis.Branches.Count; branch++)
//            {
//                //place along long axis, get boundary long axis first
//                Line xAxis = inAxis[branch][0].Value;
//                Line yAxis = inAxis[branch][1].Value;
//                Vector3d direction = default;
//                Line mainAxis = default;

//                PlaceAxis boundAxis = new PlaceAxis();

//                Intersection.LineLine(xAxis, yAxis, out double a, out double b, 0.001, true);
//                Plane axisPlane = new Plane();

//                if (xAxis.Length > yAxis.Length)
//                {
//                    direction = xAxis.Direction;
//                    mainAxis = xAxis;
//                    axisPlane = new Plane(xAxis.PointAt(a), xAxis.Direction, yAxis.Direction);
//                    boundAxis.Direction = direction;
//                    boundAxis.MainAxis = mainAxis;
//                    boundAxis.AxisPlane = axisPlane;
//                }
//                else
//                {
//                    direction = yAxis.Direction;
//                    mainAxis = yAxis;
//                    axisPlane = new Plane(xAxis.PointAt(a), yAxis.Direction, xAxis.Direction);
//                    boundAxis.Direction = direction;
//                    boundAxis.MainAxis = mainAxis;
//                    boundAxis.AxisPlane = axisPlane;
//                }
//                axisList.Add(boundAxis);
//            }
//            //place larger boundary axis first, it matters more, order boundary by axis length
//            List<PlaceAxis> orderAxisList = axisList.OrderByDescending(ax => ax.MainAxis.Length).ToList();

//            List<Pair<string, List<Transform>>> pairList = new List<Pair<string, List<Transform>>>();
//            List<string> usedNames = new List<string>();
//            for (int ax = 0; ax < orderAxisList.Count; ax++)
//            {
//                Line mainAxis = orderAxisList[ax].MainAxis;
//                Plane axisPlane = orderAxisList[ax].AxisPlane;

//                //if boundary too large, use multiple pieces
//                if (mainAxis.Length > longestClusterLength + tolerance)
//                {
//                    Element firstElement;
//                    //place center
//                    for (int e = 0; e < elementOrderClusterArea.Count; e++)
//                    {

//                        if (usedNames.Contains(elementOrderClusterArea[e].Name))
//                        {
//                            continue;
//                        }
//                        else
//                        {
//                            firstElement = elementOrderClusterArea[e];
//                            usedNames.Add(firstElement.Name);

//                            Point3d elementBaseCenter = AreaMassProperties.Compute(firstElement.GeometryBaseCurve).Centroid;

//                            Plane centerPlane = new Plane();
//                            //long cluster axis closer to X or Y axis on this element
//                            if (x)
//                            { 
//                                centerPlane = new Plane(elementBaseCenter, firstElement.Origin.XAxis, firstElement.Origin.YAxis);
//                            }
//                            if (y)
//                            {
//                                centerPlane = new Plane(elementBaseCenter, firstElement.Origin.YAxis, firstElement.Origin.XAxis);

//                            }
//                            Transform transformPlane = Transform.PlaneToPlane(centerPlane, axisPlane);
//                            break;
//                        }
//                    }
//                    //check to add left
//                    // if remaining length is smaller than half length of the largest cluster, continue
//                    double axisLeftLength;
//                    double elementLeftLength;
//                    //double remainLeftLength = axisLeftLength - elementLeftLength;

//                    if (remainLeftLength > longestClusterLength)
//                    { 
//                        for (int e = 0; e < elementOrderClusterArea.Count; e++)
//                        {
//                            Element element = elementOrderClusterArea[e];

//                            if (usedNames.Contains(element.Name))
//                            {
//                                continue;
//                            }
                        
//                            {
//                                usedNames.Add(element.Name);

//                                Point3d elementBaseCenter = AreaMassProperties.Compute(element.GeometryBaseCurve).Centroid;
//                                Plane centerPlane = new Plane(elementBaseCenter, element.Origin.XAxis, element.Origin.YAxis);
//                                Transform transformPlane = Transform.PlaneToPlane(centerPlane, axisPlane);
//                                break;
//                            }
//                        }
//                    }

//                    //check to add right
//                    // if remaining length is smaller than half length of the largest cluster, continue

//                }
//                //if the boundary is small enough to use one element to fit
//                else 
//                {
//                    for (int e = 0; e < inElement.Count; e++)
//                    {
//                        List<Transform> transformations = new List<Transform>();
//                        for (int i = 0; i < inElement[e].HeatClusterGroup.Count; i++)
//                        {
//                            HeatCluster cluster = inElement[e].HeatClusterGroup[i];
//                            Plane clusterPlane = new Plane();
//                            Transform transformPlane = new Transform();
//                            if (Math.Abs(cluster.XAxis.Length - mainAxis.Length) < tolerance)
//                            {
//                                clusterPlane = new Plane(cluster.Center, cluster.XAxis.To, cluster.YAxis.To);
//                                transformPlane = Transform.PlaneToPlane(clusterPlane, axisPlane);
//                                transformations.Add(transformPlane);
//                            }
//                            else if (Math.Abs(cluster.YAxis.Length - mainAxis.Length) < tolerance)
//                            {
//                                clusterPlane = new Plane(cluster.Center, cluster.YAxis.To, cluster.XAxis.To);
//                                transformPlane = Transform.PlaneToPlane(clusterPlane, axisPlane);
//                                transformations.Add(transformPlane);
//                            }
//                            else
//                            {
//                                continue;
//                            }
//                        }
//                        if (transformations.Count != 0)
//                        {
//                            Pair<string, List<Transform>> clusterTransformPair = new Pair<string, List<Transform>>(inElement[e].Name, transformations);
//                            pairList.Add(clusterTransformPair);
//                        }
//                    }
//                }
//                fitAreaPair.Add(ax, pairList);
//            }
            
//            var testviewList = new List<Transform>();
//            if (fitAreaPair.Count > 0)
//            {
//                List<int> keys = fitAreaPair.Keys.ToList();
//                foreach (int key in keys)
//                {
//                    var transform = fitAreaPair[key].First().Second.First();
//                    testviewList.Add(transform);
//                }
//            }
//            else 
//            {
//                testviewList.Add(new Transform());
//            }
//            DA.SetDataList(1, testviewList);
//        }
//        public struct PlaceAxis
//        {
//            public Line MainAxis { get; set; }
//            public Vector3d Direction { get; set; }
//            public Plane AxisPlane { get; set; }

//        }

//        /// <summary>
//        /// Provides an Icon for the component.
//        /// </summary>
//        protected override System.Drawing.Bitmap Icon
//        {
//            get
//            {
//                //You can add image files to your project resources and access them like this:
//                // return Resources.IconForThisComponent;
//                return null;
//            }
//        }

//        /// <summary>
//        /// Gets the unique ID for this component. Do not change this ID after release.
//        /// </summary>
//        public override Guid ComponentGuid
//        {
//            get { return new Guid("23DD3350-F6E2-47F5-A131-793EDF3DF76C"); }
//        }
//    }
//}