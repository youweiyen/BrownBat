using System;
using System.Collections.Generic;
using System.Linq;
using BrownBat.Components;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace BrownBat.Construct
{
    public class GH_StructureTemperature : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_StructureTemperature class.
        /// </summary>
        public GH_StructureTemperature()
          : base("StructureTemperature", "Nickname",
              "Description",
              "BrownBat", "Construct")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Structure", "S", "Constructed Bat Structure", GH_ParamAccess.item);
            pManager.AddGenericParameter("Temperature", "T", "Tempertature for each Structure Pixel", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Structure", "S", "Structure with Temperature", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Structure inStructure = default;
            GH_Structure<IGH_Goo> inTemperature = new GH_Structure<IGH_Goo>();

            DA.GetData(0, ref inStructure);
            DA.GetDataTree(1, out inTemperature);

            List<double[]> structureTemperature = new List<double[]>();
            for (int branch = 0; branch < inTemperature.Branches.Count; branch++)
            {
                var value = inTemperature.Branches[branch];
                double[] temperatureArray = new double[value.Count];
                for (int t = 0; t < value.Count; t++)
                {
                    value[t].CastTo<double>(out double temperature);
                    temperatureArray[t] = temperature;
                }
                structureTemperature.Add(temperatureArray);
            }
            Structure.SetTemperature(inStructure, structureTemperature);

            DA.SetData(0, inStructure);

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
            get { return new Guid("4C75D047-3381-4C0B-AAF1-03A9A173708C"); }
        }
    }
}