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
          : base("PostgresConnection", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Connect", "Con", "Connect to Postgres", GH_ParamAccess.item);
            pManager.AddTextParameter("Host", "H", "Host, default set to local(127.0.0.1)", GH_ParamAccess.item);
            pManager.AddTextParameter("")
            pManager.AddTextParameter("Database", "DB", "Database Name", GH_ParamAccess.item);
            pManager[1].Optional = true;
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
            bool connect = false;
            string host = "localhost";
            string username = default;
            string password = default;
            string database = default;

            DA.GetData(0, ref connect);
            DA.GetData(1, ref database);

            var connectionString = $"Host={host};Username={username};Password={password};Database={database}";
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);

            if(connect)
            {
                connection.Open();
            }

            else if (connect == false)
            {
                connection.Close();
            }
            var sql = "Select * from Employees";
            NpgsqlCommand command = new NpgsqlCommand(sql, connection);

            var dataReader = command.ExecuteReader();
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