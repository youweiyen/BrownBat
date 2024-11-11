using System;
using System.Collections.Generic;
using System.Linq;
using BrownBat.Components;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry.Delaunay;
using Rhino.Geometry;
using Rhino.UI;

namespace BrownBat.Calculate
{
    public class GH_Conductivity : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_Conductivity class.
        /// </summary>
        public GH_Conductivity()
          : base("Conductivity", "C",
              "Get Conductivity value from Element after arranging geometry",
              "BrownBat", "Calculate")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Constructed Element to view conductivity value", GH_ParamAccess.list);
            pManager.AddGenericParameter("Structure", "S", "Structure", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Conductivity", "C", "Conductivity Value", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inputPanel = new List<Element>();
            Structure inputWall = new Structure();

            DA.GetDataList(0, inputPanel);
            DA.GetData(1, ref inputWall);

            List<Pixel[]> pixels = inputWall.Pixel;
            double nonOverlapData = -1;

            DataTree<double> pixelConductivity = new DataTree<double>();
            Grasshopper.Kernel.Data.GH_Path path = new Grasshopper.Kernel.Data.GH_Path();

            for (int row = 0; row < pixels.Count; row++)
            {
                for (int col = 0; col < pixels[row].Count(); col++)
                {
                    Pixel pixel = pixels[row][col];
                    if (pixels[row][col].OverlapPanels.Count != 0)
                    {
                        
                        List<string> overlapNames = pixel.OverlapPanels;
                        List<Element> overlapPanels = inputPanel.Where(p => overlapNames.Contains(p.Name)).ToList();
                        
                        List<double> conductivityList = new List<double>();
                        foreach (Element panel in overlapPanels)
                        {
                            (int, int) domain = pixel.PixelDomain[panel.Name];
                            int pixelRow = domain.Item2;
                            int pixelColumn = domain.Item1;
                            double conductivity = panel.PixelConductivity[pixelRow][pixelColumn];
                            conductivityList.Add(conductivity);
                        }
                        double conductivitySum = conductivityList.Sum();
                        path = new Grasshopper.Kernel.Data.GH_Path(row);
                        pixelConductivity.Add(conductivitySum, path);
                    }
                    else
                    {
                        path = new Grasshopper.Kernel.Data.GH_Path(row);
                        pixelConductivity.Add(nonOverlapData, path);
                    }
                }

            }

            DA.SetDataTree(0, pixelConductivity);
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
            get { return new Guid("7328AC0D-1A83-426C-8B07-39C3B91CA711"); }
        }
    }
}