using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

using CsvHelper;
using Grasshopper.Kernel.Types;
using System.Linq;
using System.IO;
using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace BrownBat.Data
{
    public class GH_ExportFile : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_ExportCSV class.
        /// </summary>
        public GH_ExportFile()
          : base("ExportFile", "Ex",
              "Export Data to CSV File",
              "BrownBat", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Data", "D", "Data to export as CSV", GH_ParamAccess.tree);
            pManager.AddTextParameter("Location", "L", "Export location and name", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Save", "S", "Export the CSV", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> inputData = new GH_Structure<GH_Number>();
            string inputLocation = default;
            bool inputSave = false;

            DA.GetDataTree(0, out inputData);
            DA.GetData(1, ref inputLocation);
            DA.GetData(2, ref inputSave);

            if (inputSave)
            {
                using (var writer = new StreamWriter($"{inputLocation}"))
                {
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false,
                    };
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        for (int branch = 0; branch < inputData.Branches.Count(); branch++)
                        {
                            foreach (var item in inputData[branch])
                            {
                                double pixelData = item.Value;
                                csv.WriteRecord(pixelData);
                            }
                            csv.NextRecord();
                        }
                    
                    }
                }
                GH_RuntimeMessageLevel level = GH_RuntimeMessageLevel.Remark;
                this.AddRuntimeMessage(level, "File Saved");
            }

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
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
            get { return new Guid("123FC4CD-7CCD-486F-A2D1-8EC55498B2F1"); }
        }
    }
}