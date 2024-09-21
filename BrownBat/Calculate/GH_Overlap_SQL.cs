using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Npgsql;
using BrownBat.Components;
using System.Diagnostics;
using System.Linq;

namespace BrownBat.Calculate
{
    public class GH_Overlap_SQL : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_OverlapElement_SQL class.
        /// </summary>
        public GH_Overlap_SQL()
          : base("Overlap_SQL", "OS",
              "Calculate overlapping pixels using SQL data",
              "BrownBat", "Calculate")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("ConnectionString", "CS", "Connection String for Database", GH_ParamAccess.item);
            pManager.AddBooleanParameter("ConnectDatabase", "CD", "Connect to Database", GH_ParamAccess.item);
            pManager.AddTextParameter("Database", "D", "Database Name", GH_ParamAccess.item);
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
            string connectionString = default;
            bool connect = false;
            string database = default;

            DA.GetData(0, ref connectionString);
            DA.GetData(1, ref connect);
            DA.GetData(2, ref database);

            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            if (connect)
            {
                connection.Open();
                var sql = $"Select * from {database}";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                var dataReader = command.ExecuteReader();
                while (dataReader.Read())
                    Console.Write("{0}\t{1} \n", dataReader[0], dataReader[1]);

                connection.Close();
                
            }

            List<Element> inputModelPanel = new List<Element>();
            Structure inputWall = new Structure();
            DA.GetDataList(0, inputModelPanel);
            DA.GetData(1, ref inputWall);

            List<Pixel[]> wallPixels = inputWall.Pixel;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            for (int rowPoint = 0; rowPoint < wallPixels.Count; rowPoint++)
            {
                foreach (Pixel pixel in wallPixels[rowPoint])
                {
                    List<string> intersectPanelNames = new List<string>();
                    Dictionary<string, (double, double)> panelToPosition = new Dictionary<string, (double, double)>();
                    Dictionary<string, (int, int)> panelToDomain = new Dictionary<string, (int, int)>();

                    for (int i = 0; i < inputModelPanel.Count(); i++)
                    {
                        //point in curve calculation
                        PointContainment containment = inputModelPanel[i].GeometryBaseCurve.Contains(pixel.PixelGeometry, Plane.WorldXY, 0.02);

                        if (containment == PointContainment.Unset)
                        {
                            throw new Exception("curve is not valid");
                        }
                        if (containment == PointContainment.Inside)
                        {

                            Element intersectPanel = inputModelPanel[i];
                            string intersectPanelName = intersectPanel.Name;
                            intersectPanelNames.Add(intersectPanelName);

                            Point3d orientPoint = new Point3d(pixel.PixelGeometry);
                            Transform matrix = intersectPanel.InverseMatrix;
                            orientPoint.Transform(matrix);

                            double xPosition = Math.Abs(0 - orientPoint.X);
                            double yPosition = Math.Abs(0 - orientPoint.Y);
                            int xDomain = (int)Math.Floor(xPosition * (intersectPanel.PixelShape.Item1 / intersectPanel.GeometryShape.Item1));
                            int yDomain = (int)Math.Floor(yPosition * (intersectPanel.PixelShape.Item2 / intersectPanel.GeometryShape.Item2));

                            (int, int) intersectPanelDomain = (xDomain, yDomain);

                            //panelToPosition.Add(intersectPanelName, intersectPanelPosition);
                            panelToDomain.Add(intersectPanelName, intersectPanelDomain);
                        }


                    }
                    Pixel.SetOverlapPanels(pixel, intersectPanelNames);
                    Pixel.SetPixelDomain(pixel, panelToDomain);
                }

            }

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