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

namespace BrownBat.Construct
{
    public class GH_ConstructOriginPanel : GH_Component
    {

        public GH_ConstructOriginPanel()
          : base("ConstructOriginalPanel", "OP",
              "Add the data to the panel geometry",
              "BrownBat", "Construct")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("DataPath", "D", "Data Source Path", GH_ParamAccess.list);
            pManager.AddGenericParameter("PanelGeometry", "G", "Panel Geometry", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Bake", "B", "Bake Brep, Name, Origin plane", GH_ParamAccess.item, false);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Panel", "P", "Panel", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> inputPaths = new List<string>();
            List<Panel> inputGeometry = new List<Panel>();
            bool inputBake = false;

            DA.GetDataList(0, inputPaths);
            DA.GetDataList(1, inputGeometry);
            DA.GetData(2,  ref inputBake);

            List<Panel> outputPanels = new List<Panel>();

            for (int i = 0; i < inputPaths.Count; i++)
            {
                List<double[]>rowList = new List<double[]>();
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
                            for (int p = 0; csv.TryGetField<double>(p, out double pixel); p++)
                            {
                                rows.Add(pixel);
                            }
                            double[] row = rows.ToArray();
                            rows.Clear();
                            rowList.Add(row);
                        }
                    }
                }
                Panel panel = inputGeometry.Where(g => g.Name == name).FirstOrDefault();

                Panel.SetPanelConductivity(panel, rowList);
                Panel.CSVShape(panel);
                Panel.ModelShape(panel);

                outputPanels.Add(panel);
            }
            if (inputBake)
            {
                //Paraent Layer
                Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
                Panel panel = outputPanels[0];
                string panelName = panel.Name;
                System.Drawing.Color color = new System.Drawing.Color();
                color = System.Drawing.Color.FromArgb(255, 0, 0, 255);

                int index = doc.Layers.FindByFullPath(panelName, -1);
                if (index < 0)
                {
                    doc.Layers.Add(panelName, color);
                }
                index = doc.Layers.FindByFullPath(panelName, -1);
                Rhino.DocObjects.Layer parentLayer = doc.Layers[index];

                //set attributes
                Rhino.DocObjects.ObjectAttributes attBrep = new Rhino.DocObjects.ObjectAttributes();
                Rhino.DocObjects.ObjectAttributes attOrigin = new Rhino.DocObjects.ObjectAttributes();
                Rhino.DocObjects.ObjectAttributes attX = new Rhino.DocObjects.ObjectAttributes();
                Rhino.DocObjects.ObjectAttributes attY = new Rhino.DocObjects.ObjectAttributes();

                attBrep.Name = "brep";
                attBrep.LayerIndex = index;

                attOrigin.Name = "0";
                attOrigin.LayerIndex = index;
                attX.Name = "1";
                attX.LayerIndex = index;
                attY.Name = "2";
                attY.LayerIndex = index;

                Plane originPlane = outputPanels[0].Origin;
                Point3d originPoint = originPlane.Origin;

                Vector3d vectorX = originPlane.XAxis;
                Vector3d vectorY = originPlane.YAxis;
                Point3d originX = originPoint + vectorX;
                Point3d originY = originPoint + vectorY;

                //bake with attributes
                doc.Objects.AddBrep(panel.Model, attBrep);

                doc.Objects.AddPoint(originPoint, attOrigin);
                doc.Objects.AddPoint(originX, attX);
                doc.Objects.AddPoint(originY, attY);
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
                return null;
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