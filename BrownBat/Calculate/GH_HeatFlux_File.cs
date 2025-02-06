using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Runtime;
using BrownBat.Components;
using BrownBat.CalculateHelper;
using System.Linq;
using Grasshopper;
using Rhino;

namespace BrownBat.Calculate
{
    public class GH_HeatFlux_File : GH_Component
    {

        public GH_HeatFlux_File()
          : base("HeatFlux_File", "FC",
              "Calculate multiple panel heat flux with CSV data",
              "BrownBat", "Calculate")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element", GH_ParamAccess.list);
            pManager.AddGenericParameter("Structure", "S", "Structure", GH_ParamAccess.item);
            pManager.AddNumberParameter("ComfyTemp", "T", "Temperature Difference", GH_ParamAccess.item);
            pManager.AddBooleanParameter("HeatDirection", "D", "Reverse heat travel direction, default is outdoor to indoor", GH_ParamAccess.item);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Structure", "S", "Bat Structure", GH_ParamAccess.item);
            pManager.AddNumberParameter("Flux", "F", "Heat Flux of each pixel", GH_ParamAccess.tree);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inputPanel = new List<Element>();
            double inputComfy = default;
            Structure inputWall = new Structure();
            bool reverseDirection = false;

            DA.GetDataList(0, inputPanel);
            DA.GetData(1, ref inputWall);
            DA.GetData(2, ref inputComfy);
            DA.GetData(3, ref reverseDirection);


            List<Pixel[]> pixels = inputWall.Pixel;
            double nonOverlapData = -1;

            DataTree<double> pixelFlux = new DataTree<double>();
            Grasshopper.Kernel.Data.GH_Path path = new Grasshopper.Kernel.Data.GH_Path();

            UnitSystem unitSystem = RhinoDoc.ActiveDoc.ModelUnitSystem;
            double unitToMeter = 1;
            switch (unitSystem)
            {
                case UnitSystem.Meters:
                    unitToMeter = 1;
                    break;
                case UnitSystem.Centimeters:
                    unitToMeter = 0.01;
                    break;
                case UnitSystem.Millimeters:
                    unitToMeter = 0.001;
                    break;
            }

            for (int row = 0; row < pixels.Count; row++)
            {
                for (int col = 0; col < pixels[row].Count(); col++)
                {
                    if (pixels[row][col].OverlapPanels.Count != 0)
                    {
                        double resistance = HeatTransfer.ConductiveResistanceFromFile(pixels[row][col], inputPanel) * unitToMeter;
                        double backgroundTemperature = inputWall.Temperature[row][col];
                        double dT = backgroundTemperature - inputComfy;
                        //add direction
                        if (reverseDirection)
                        {
                            dT = -dT;
                        }
                        double flux = dT / resistance;
                        Pixel.SetHeatFlux(pixels[row][col], flux);
                        path = new Grasshopper.Kernel.Data.GH_Path(row);
                        pixelFlux.Add(flux, path);
                    }
                    else 
                    {
                        path = new Grasshopper.Kernel.Data.GH_Path(row);
                        pixelFlux.Add(nonOverlapData, path);
                    }
                }
                
            }
            
            DA.SetData(0, inputWall);
            DA.SetDataTree(1, pixelFlux);
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
            get { return new Guid("64B5BF84-79B1-4886-9A8B-DAB9C332B93B"); }
        }
    }
}