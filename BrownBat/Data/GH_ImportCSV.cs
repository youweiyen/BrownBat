using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using CsvHelper;
using System.IO;
using System.Globalization;
using CsvHelper.Configuration;
using BrownBat.Components;
using System.Linq;
using Rhino.Commands;
using System.Text.RegularExpressions;
using Rhino;
using Rhino.DocObjects;
using Rhino.UI;

namespace BrownBat.Data
{
    public class GH_ImportCSV : GH_Component
    {

        public GH_ImportCSV()
          : base("ImportCSV", "InCsv",
              "Add CSV data",
              "BrownBat", "Data")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("DataPath", "D", "CSV Source Path", GH_ParamAccess.list);
            pManager.AddTextParameter("ElementName", "N", "Input Element Name", GH_ParamAccess.list);

        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("BatPanel", "P", "Panel", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> inputPaths = new List<string>();
            List<string> inputNames = new List<string>();

            DA.GetDataList(0, inputPaths);
            DA.GetDataList(1, inputNames);


            List<Element> outputPanels = new List<Element>();

            for (int i = 0; i < inputPaths.Count; i++)
            {
                List<double[]> rowList = new List<double[]>();
                string name = Path.GetFileNameWithoutExtension(inputPaths[i]);
                using (StreamReader reader = new StreamReader(inputPaths[i]))
                {
                    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false
                    };
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        while (csv.Read())
                        {

                            List<double> rows = new List<double>();
                            for (int p = 0; csv.TryGetField(p, out double pixel); p++)
                            {
                                rows.Add(pixel);
                            }
                            double[] row = rows.ToArray();
                            rows.Clear();
                            rowList.Add(row);
                        }
                    }
                }
                //Panel panel = inputGeometry.Where(g => g.Name == name).FirstOrDefault();
                Element panel = new Element(inputNames[i]);

                Element.SetPanelConductivity(panel, rowList);
                Element.CSVShape(panel);

                outputPanels.Add(panel);
            }

            DA.SetDataList(0, outputPanels);
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
                return Properties.Resources.baticon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("56B17B3A-26E2-45F5-A351-CE15966A786A"); }
        }
    }
}