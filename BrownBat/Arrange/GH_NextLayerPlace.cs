using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BrownBat.Components;
using System.Xml.Linq;
using Rhino.Geometry.Intersect;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using BrownBat.CalculateHelper;

namespace BrownBat.Arrange
{
    public class GH_NextLayerPlace : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_NextLayerPlace class.
        /// </summary>
        public GH_NextLayerPlace()
          : base("PlacePassive", "PP",
              "Place the passive Elements on the next layer",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element to place on passive positions", GH_ParamAccess.list);
            pManager.AddLineParameter("BoundaryAxis", "BA", "Boundary axis to place", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Placed Element", GH_ParamAccess.list);
            pManager.AddTransformParameter("Transformation", "T", "Element Transformation", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            GH_Structure<GH_Line> inAxis = new GH_Structure<GH_Line>();

            DA.GetDataList(0, inElement);
            DA.GetDataTree(1, out inAxis);

            double tolerance = 10;
            var elementHasCluster = inElement.Where(e => e.HeatClusterGroup != null);

            double longestClusterLength = elementHasCluster.Max(e=> e.HeatClusterGroup
                                                    .Select(hc => new[] { hc.Value.XAxis.Length, hc.Value.YAxis.Length }
                                                            .ToList()
                                                            .Max())
                                                    .Max());
            Dictionary<int, List<Pair<string, List<Transform>>>> fitAreaPair = new Dictionary<int, List<Pair<string, List<Transform>>>>();

            for (int branch = 0; branch < inAxis.Branches.Count; branch++)
            {
                //place along long axis, get boundary long axis first
                Line xAxis = inAxis[branch][0].Value;
                Line yAxis = inAxis[branch][1].Value;
                Vector3d direction = default;
                Line mainAxis = default;

                Intersection.LineLine(xAxis, yAxis, out double a, out double b, 0.001, true);
                Transform transformPlane = new Transform();
                Plane axisPlane = new Plane();
                
                if (xAxis.Length > yAxis.Length)
                {
                    direction = xAxis.Direction;
                    mainAxis = xAxis;
                    axisPlane = new Plane(xAxis.PointAt(a), xAxis.Direction, yAxis.Direction);
                }
                else 
                {
                    direction = yAxis.Direction;
                    mainAxis = yAxis;
                    axisPlane = new Plane(xAxis.PointAt(a), yAxis.Direction, xAxis.Direction);
                }
                List<Pair<string, List<Transform>>> pairList = new List<Pair<string, List<Transform>>>();
                //if the boundary is small enough to use one element to fit
                if (mainAxis.Length < longestClusterLength + tolerance)
                {
                    for (int e = 0; e < inElement.Count; e++)
                    {
                        List<Transform>transformations = new List<Transform>();
                        for (int i = 0; i < inElement[e].HeatClusterGroup.Count; i++)
                        {
                            HeatCluster cluster = inElement[e].HeatClusterGroup[i];
                            Plane clusterPlane = new Plane();
                            if (Math.Abs(cluster.XAxis.Length - mainAxis.Length) < tolerance)
                            {
                                clusterPlane = new Plane(cluster.Center, cluster.XAxis.To, cluster.YAxis.To);
                                transformPlane = Transform.PlaneToPlane(clusterPlane, axisPlane);
                                transformations.Add(transformPlane);
                            }
                            else if (Math.Abs(cluster.YAxis.Length - mainAxis.Length) < tolerance)
                            {
                                clusterPlane = new Plane(cluster.Center, cluster.YAxis.To, cluster.XAxis.To);
                                transformPlane = Transform.PlaneToPlane(clusterPlane, axisPlane);
                                transformations.Add(transformPlane);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if (transformations.Count != 0)
                        {
                            Pair<string, List<Transform>> clusterTransformPair = new Pair<string, List<Transform>>(inElement[e].Name, transformations);
                            pairList.Add(clusterTransformPair);
                        }
                    }
                }
                //boundary too large, use multiple pieces
                else 
                {   
                    //TODO
                    continue;
                }
                fitAreaPair.Add(branch, pairList);
            }
            var testviewList = new List<Transform>();
            if (fitAreaPair.Count > 0)
            {
                List<int> keys = fitAreaPair.Keys.ToList();
                foreach (int key in keys)
                {
                    var transform = fitAreaPair[key].First().Second.First();
                    testviewList.Add(transform);
                }
            }
            else 
            {
                testviewList.Add(new Transform());
            }
            DA.SetDataList(1, testviewList);
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
            get { return new Guid("23DD3350-F6E2-47F5-A131-793EDF3DF76C"); }
        }
    }
}