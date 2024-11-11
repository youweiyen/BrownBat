using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Npgsql;
using BrownBat.Components;
using System.Diagnostics;
using System.Linq;
using BrownBat.CalculateHelper;
using Grasshopper;

namespace BrownBat.Calculate
{
    public class GH_HeatFlux_Sql : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_OverlapElement_SQL class.
        /// </summary>
        public GH_HeatFlux_Sql()
          : base("HeatFlux_Sql", "OS",
              "Calculate overlapping pixels using SQL data",
              "BrownBat", "Calculate")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Connect", "C", "Connect to Database", GH_ParamAccess.item);
            pManager.AddTextParameter("ConnectionString", "CS", "Connection String for Database", GH_ParamAccess.item);
            pManager.AddTextParameter("Database", "D", "Database Name", GH_ParamAccess.item);
            pManager.AddGenericParameter("Element", "E", "Bat Element", GH_ParamAccess.list);
            pManager.AddGenericParameter("Structure", "S", "Bat Structure", GH_ParamAccess.item);
            pManager.AddNumberParameter("dT", "dT", "Temperature Difference", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Structure", "S", "Bat Structure", GH_ParamAccess.item);
            pManager.AddNumberParameter("Flux", "F", "Heat Flux of each pixel", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool connect = false;
            string connectionString = default;
            string database = default;
            List<Element> inputElement = new List<Element>();
            Structure inputStructure = new Structure();
            double inputdT = default;


            DA.GetData(0, ref connect);
            DA.GetData(1, ref connectionString);
            DA.GetData(2, ref database);
            DA.GetDataList(3, inputElement);
            DA.GetData(4, ref inputStructure);
            DA.GetData(5, ref inputdT);

            List<Pixel[]> pixels = inputStructure.Pixel;
            double nonOverlapData = -1;

            DataTree<double> pixelFlux = new DataTree<double>();
            Grasshopper.Kernel.Data.GH_Path path = new Grasshopper.Kernel.Data.GH_Path();

            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            if (connect)
            {
                connection.Open();
                for (int row = 0; row < pixels.Count; row++)
                {
                    for (int col = 0; col < pixels[row].Count(); col++)
                    {
                        if (pixels[row][col].OverlapPanels.Count != 0)
                        {
                            double resistance = HeatTransfer.ConductiveResistanceFromSql(pixels[row][col], inputElement, database, connection);

                            double flux = inputdT / resistance;
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
                connection.Close();
                
            }



            DA.SetData(0, inputStructure);
            DA.SetDataTree(1, pixelFlux);

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
            get { return new Guid("BCB2A6ED-27F7-4E76-BC86-CCFA1763B176"); }
        }
    }
}