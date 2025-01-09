using System;
using System.Collections.Generic;
using BrownBat.Components;
using Grasshopper.Kernel;
using Rhino.Geometry;
using BrownBat.CalculateHelper;
using Grasshopper;

namespace BrownBat.Arrange
{
    public class GH_ArrangeValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public GH_ArrangeValue()
          : base("ArrangeValue", "AV",
              "Arrange all possible overlapping outcomes to find optimal conductivity value",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element with selected range of heat value", GH_ParamAccess.list);
            pManager.AddGenericParameter("Structure", "S", "Structure to build", GH_ParamAccess.item);
            pManager.AddCurveParameter("Boundary", "B", "Boundary area to assign high conductivity values", GH_ParamAccess.list);
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
            pManager.AddBrepParameter("Geometry", "G", "Transformed element geometry", GH_ParamAccess.tree);
            pManager.AddTransformParameter("Transform", "T", "Transformation of element on to the structure", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            Structure inStructure = new Structure();
            List<Curve> inRegion = new List<Curve>();
            double inDifference = 10;

            DA.GetDataList(0, inElement);
            DA.GetData(1, ref inStructure);
            DA.GetDataList(2, inRegion);
            DA.GetData(3, ref inDifference);

            DataTree<Transform> transformOptions = new DataTree<Transform>();

            foreach(Curve boundaryCurve in inRegion)
            {
                Point3d boundaryCenter = AreaMassProperties.Compute(boundaryCurve).Centroid;
            
                Point3d[] boundaryPoints = boundaryCurve.MaxCurvaturePoints();
                Rectangle3d boundaryBox = AreaHelper.MinBoundingBox(boundaryPoints, inElement[0].Origin);
                Plane boundaryPlane = new Plane(boundaryCenter, boundaryBox.Plane.XAxis, boundaryBox.Plane.YAxis);

                double xLength = boundaryBox.Width;
                double yLength = boundaryBox.Height;
                List<Transform> transformPlanes = new List<Transform>();

                foreach (Element element in inElement)
                {
                    for (int i = 0; i < element.HeatClusterGroup.Count; i++)
                    {
                        HeatCluster cluster = element.HeatClusterGroup[i];

                        if (Math.Abs(cluster.XAxis.Length - xLength) < inDifference
                            && Math.Abs(cluster.YAxis.Length - yLength) < inDifference
                            || Math.Abs(cluster.XAxis.Length - yLength) < inDifference
                            && Math.Abs(cluster.YAxis.Length - xLength) < inDifference)
                        {
                            Plane clusterPlane = new Plane(cluster.Center, cluster.XAxis.To, cluster.YAxis.To);
                            Transform transformPlane = Transform.PlaneToPlane(clusterPlane, boundaryPlane);
                            transformPlanes.Add(transformPlane);
                        }
                    }
                }
                
            }

            DA.SetDataTree(0, transformPlanes);
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
            get { return new Guid("C3641087-4F32-41BF-A831-84E1A3E48468"); }
        }
    }
}