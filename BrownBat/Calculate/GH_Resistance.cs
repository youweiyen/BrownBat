using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Runtime;
using BrownBat.Components;
using BrownBat.CalculateHelper;
using System.Linq;
using Grasshopper;

namespace BrownBat.Calculate
{
    public class GH_Resistance : GH_Component
    {

        public GH_Resistance()
          : base("Resistance", "R",
              "Calculate multiple overlapping panel Conductivity Resistance (Resistance = sum of thickness/ sum of conductivity)",
              "BrownBat", "Calculate")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element", GH_ParamAccess.list);
            pManager.AddGenericParameter("Structure", "S", "Structure", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Resistance", "R", "Conductivity Resistance of each pixel", GH_ParamAccess.tree);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inputPanel = new List<Element>();
            Structure inputWall = new Structure();

            DA.GetDataList(0, inputPanel);
            DA.GetData(1, ref inputWall);
            
            List<Pixel[]> pixels = inputWall.Pixel;
            double nonOverlapData = -1;

            DataTree<double> pixelResistance = new DataTree<double>();
            Grasshopper.Kernel.Data.GH_Path path = new Grasshopper.Kernel.Data.GH_Path();

            for (int row = 0; row < pixels.Count; row++)
            {
                for (int col = 0; col < pixels[row].Count(); col++)
                {
                    if (pixels[row][col].OverlapPanels.Count != 0)
                    {
                        double resistance = HeatTransfer.ConductiveResistanceFromFile(pixels[row][col], inputPanel);
                        
                        path = new Grasshopper.Kernel.Data.GH_Path(row);
                        pixelResistance.Add(resistance, path);
                    }
                    else 
                    {
                        path = new Grasshopper.Kernel.Data.GH_Path(row);
                        pixelResistance.Add(nonOverlapData, path);
                    }
                }
                
            }
            
            DA.SetDataTree(0, pixelResistance);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.baticon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("572d2bb4-e338-489f-9f6f-f905a6b93f15"); }
        }
    }
}