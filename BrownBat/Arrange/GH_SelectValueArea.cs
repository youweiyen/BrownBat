using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using BrownBat.Components;
using System.Linq;
using System.Data.Common.CommandTrees;
using BrownBat.CalculateHelper;
using Dbscan;
using Grasshopper.GUI;
using Dbscan.RBush;
using Grasshopper;
using Grasshopper.Kernel.Data;

namespace BrownBat.Arrange
{
    public class GH_SelectValueArea : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_SelectValueArea class.
        /// </summary>
        public GH_SelectValueArea()
          : base("SelectValueArea", "SA",
              "Selecting area with chosen value",
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
            pManager.AddNumberParameter("MinArea", "Min", "Minimum area of cluster. Default set to 1mm2", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Boundary", "B", "Boundaries of value area", GH_ParamAccess.list);
            pManager.AddPointParameter("ClusterPoints", "CP", "Clustered Points", GH_ParamAccess.tree);
            pManager.AddPointParameter("OverPoints", "P", "Over Points", GH_ParamAccess.list);
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
            double inMinArea = 1;
            DA.GetData(2, ref inMinArea);


            DataTree<Point3d> ClusteredPts = new DataTree<Point3d>();
            GH_Path path = new GH_Path();

            List<Point3d> op = new List<Point3d>();

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
                            Point3d pp = new Point3d(row, col, 0);
                            fitID.Add(id);
                            op.Add(pp);
                        }
                    }
                }
                double pixelCount = 1 / (element.PixelSize.Item1 * element.PixelSize.Item2);
                int minPoints = (int)Math.Round(pixelCount);
                double epsilon = 5;
                var clusters = DbscanRBush.CalculateClusters(fitID, epsilon, minPoints);
                for (int c = 0; c < clusters.Clusters.Count; c++)
                {
                    var points = clusters.Clusters[c].Objects;
                    foreach (var point in points)
                    {
                        Point3d rhinoPoint = new Point3d(point.Point.X, point.Point.Y, 0);
                        path = new GH_Path(c);
                        ClusteredPts.Add(rhinoPoint, path);
                    }

                }
            }
            DA.SetDataTree(1, ClusteredPts);
            DA.SetDataList(2, op);
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