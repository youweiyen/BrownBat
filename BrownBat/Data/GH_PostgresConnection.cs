using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Npgsql;
using System.Data.Entity;

namespace BrownBat.Data
{
    public class GH_PostgresConnection : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_PostgresConnection class.
        /// </summary>
        public GH_PostgresConnection()
          : base("PostgresConnection_WIP", "PC",
              "Postgres Connection",
              "BrownBat", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Host", "H", "Host, default set to local(127.0.0.1)", GH_ParamAccess.item, "localhost");
            pManager.AddTextParameter("Username", "U", "Username", GH_ParamAccess.item);
            pManager.AddTextParameter("Password", "P", "Password", GH_ParamAccess.item);
            pManager.AddTextParameter("Database", "DB", "Database Name", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("ConnectionString", "CS", "Connection String for Database", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string host = "localhost";
            string username = default;
            string password = default;
            string database = default;

            DA.GetData(0, ref host);
            DA.GetData(1, ref username);
            DA.GetData(2, ref password);
            DA.GetData(3, ref database);

            string connectionString = $"Host={host};Username={username};Password={password};Database={database}";
            
            DA.SetData(0, connectionString);
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
            get { return new Guid("DF8C4B74-A790-45C2-BCC5-F38A29785610"); }
        }
    }
}