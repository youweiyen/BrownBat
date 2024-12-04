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
            pManager.AddNumberParameter("MinArea", "Min", "Minimum area of cluster", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Boundary", "B", "Boundaries of value area", GH_ParamAccess.list);
            pManager.AddPointParameter("p", "p", "Boundaries of value area", GH_ParamAccess.list);

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
            double inMinArea = default;
            DA.GetData(2, ref inMinArea);


            var pts = new List<Point3d>();
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
                            var pt = new Point3d(row, col, 0);
                            pts.Add(pt);
                            fitID.Add(id);
                        }
                    }
                }
                double pixelCount = inMinArea / (element.PixelShape.Item1 * element.PixelShape.Item2);
                int minPoints = (int)Math.Round(pixelCount);
                double epsilon = 20;
                var clusters = DbscanRBush.CalculateClusters(fitID, epsilon, minPoints);
                var a = clusters.Clusters[0];
                foreach (var cluster in clusters.Clusters)
                {
                    var p = cluster.Objects;
                }
            }
            DA.SetDataList(1, pts);
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