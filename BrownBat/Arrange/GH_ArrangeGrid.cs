using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using BrownBat.CalculateHelper;
using BrownBat.Components;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry.Collections;
using System.Xml.Linq;

namespace BrownBat.Arrange
{
    public class GH_ArrangeGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_ArrangeGrid class.
        /// </summary>
        public GH_ArrangeGrid()
          : base("ArrangeGrid", "AG",
              "Respect boundary and arranging elements as grid",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element with selected range of heat value", GH_ParamAccess.list);
            pManager.AddPointParameter("BoundaryPoint", "B", "Boundary point area to assign high conductivity values", GH_ParamAccess.tree);
            pManager.AddBrepParameter("PlacePosition", "P", "Outline to place the element", GH_ParamAccess.list);
            pManager.AddNumberParameter("Difference", "D",
                "Heat area axis size difference. Default set to 10",
                GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Transform element inside the grid structure", GH_ParamAccess.list);
            pManager.AddBrepParameter("Geometry", "G", "Transformed element geometry", GH_ParamAccess.tree);
            pManager.AddTextParameter("Name", "N", "Element Name", GH_ParamAccess.tree);
            pManager.AddTransformParameter("Transform", "T", "Rotation transformation of grid element", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            List<Brep> inPlacePosition = new List<Brep>();
            GH_Structure<GH_Point> inPlaceRegion = new GH_Structure<GH_Point>();
            double inDifference = 10;

            DA.GetDataList(0, inElement);
            DA.GetDataTree(1, out inPlaceRegion);
            DA.GetDataList(2, inPlacePosition);
            DA.GetData(3, ref inDifference);

            double tolerance = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            List<Transform> transformList = new List<Transform>();

            List<Element> transformElement = new List<Element>();
            HeatCluster largestCluster = new HeatCluster();

            for (int i = 0; i < inPlaceRegion.Branches.Count(); i++)
            {
                Transform normalTransform = Transform.PlaneToPlane(inElement[i].Origin, PlacePlane(inPlacePosition[i]));
                
                if (inPlaceRegion[i].Count != 0 && inElement[i].HeatClusterGroup != null)
                {
                    largestCluster = inElement[i]
                                        .HeatClusterGroup
                                        .OrderByDescending(hcg =>
                                        hcg.Value.XAxis.Length * hcg.Value.YAxis.Length)
                                        .First()
                                        .Value;

                }
                else
                {
                    //transform as orientation now
                    transformList.Add(normalTransform);

                    Element.TryGetInverseMatrix(inElement[i], normalTransform);
                    Element.BaseCurve(inElement[i]);

                    transformElement.Add(inElement[i]);
                    continue;
                }

                IEnumerable<Point3d> regionPoints = inPlaceRegion[i].Select(pt => pt.Value);
                Point3d centerPoint = AreaMassProperties.Compute(inPlacePosition[i]).Centroid;
                Rectangle3d placeBoundBox = AreaHelper.MinBoundingBox(regionPoints, PlacePlane(inPlacePosition[i]));
                
                //choose placement boundingbox long axis
                Line placeLongAxis = new Line();
                if (placeBoundBox.Height > placeBoundBox.Width)
                {
                    placeLongAxis = AreaHelper.AxisLine(regionPoints,
                                                    placeBoundBox.Plane.YAxis,
                                                    placeBoundBox.Height,
                                                    placeBoundBox.ToPolyline(),
                                                    tolerance);
                    
                }
                else
                {
                    placeLongAxis = AreaHelper.AxisLine(regionPoints,
                                                    placeBoundBox.Plane.XAxis,
                                                    placeBoundBox.Width,
                                                    placeBoundBox.ToPolyline(),
                                                    tolerance);
                }
                Vector3d placeLongVector = new Vector3d(placeLongAxis.To - placeLongAxis.From);

                //choose element long axis
                Line elementLongAxis = new Line();
                if (largestCluster.XAxis.Length > largestCluster.YAxis.Length)
                {
                    elementLongAxis = largestCluster.XAxis;
                }
                else
                { 
                    elementLongAxis = largestCluster.YAxis; 
                }
                double angleDifference = double.MaxValue;
                double centerDistance = double.MaxValue;

                Point3d transformClusterCenter = new Point3d(largestCluster.Center);
                transformClusterCenter.Transform(normalTransform);
                Transform multipleTransform = new Transform();

                for (int a = 0; a < 4; a++)
                {
                    double angle = Math.PI * a;
                    Vector3d transformAxis = new Vector3d(elementLongAxis.To - elementLongAxis.From);
                    transformAxis.Transform(normalTransform);

                    Transform orientTransform = Transform.Rotation(angle, centerPoint);
                    transformAxis.Transform(orientTransform);
                    double newAngle = Vector3d.VectorAngle(transformAxis, placeLongVector);
                    if (newAngle > Math.PI)
                    {
                        newAngle = Math.PI - newAngle;
                    }
                    if (newAngle < angleDifference)
                    {
                        Point3d orientClusterCenter = new Point3d(transformClusterCenter);
                        orientClusterCenter.Transform(orientTransform);
                        double dist = placeLongAxis.DistanceTo(orientClusterCenter, true);
                        if (dist < centerDistance)
                        {
                            centerDistance = dist;
                            angleDifference = newAngle;
                            multipleTransform = orientTransform * normalTransform;
                        }
                    }
                }
                Element.TryGetInverseMatrix(inElement[i], multipleTransform);
                Element.TransformBaseCurve(inElement[i], normalTransform);


                transformElement.Add(inElement[i]);

                transformList.Add(multipleTransform);

            }
            DA.SetDataList(0, transformElement);
            DA.SetDataList(3, transformList);
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
            get { return new Guid("020DB6E6-B69A-42B2-919C-BC2D25854014"); }
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