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
    public class GH_HeatFlux : GH_Component
    {

        public GH_HeatFlux()
          : base("HeatFlux", "F",
              "Multiple panel heat flux",
              "BrownBat", "Calculate")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Panel", "P", "Model Panels", GH_ParamAccess.list);
            pManager.AddGenericParameter("Wall", "W", "Wall", GH_ParamAccess.item);
            pManager.AddNumberParameter("dT", "dT", "Temperature Difference", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Wall", "W", "Wall", GH_ParamAccess.item);
            pManager.AddNumberParameter("Flux", "F", "Flux of each Pixel", GH_ParamAccess.tree);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Panel> inputPanel = new List<Panel>();
            double inputdT = default;
            Wall inputWall = new Wall();

            DA.GetDataList(0, inputPanel);
            DA.GetData(1, ref inputWall);
            DA.GetData(2, ref inputdT);
            
            List<Pixel[]> pixels = inputWall.Pixel;

            DataTree<double> pixelFlux = new DataTree<double>();

            for (int row = 0; row < pixels.Count; row++)
            {
                for (int col = 0; col < pixels[row].Count(); col++)
                {
                    if (pixels[row][col].OverlapPanels.Count != 0)
                    {
                        double coefficient = HeatTransfer.Coefficient(pixels[row][col], inputWall, inputPanel);
                        double wallPixelArea = Area.WallPixelArea(inputWall);
            
                        double flux = coefficient * wallPixelArea * inputdT;
                        Pixel.SetHeatFlux(pixels[row][col], flux);
                        pixelFlux.Add(flux);
                    }
                }
                
            }
            
            DA.SetData(0, inputWall);
            DA.SetDataTree(0, pixelFlux);
        }

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
            get { return new Guid("64B5BF84-79B1-4886-9A8B-DAB9C332B93B"); }
        }
    }
}