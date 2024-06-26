using BrownBat.Components;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrownBat.Construct
{
    public class GH_ConstructOriginPanelGeometry : GH_Component
    {
        public GH_ConstructOriginPanelGeometry()
          : base("ConstructOriginPanelGeometry", "Panel",
            "Combine CSV data and Geometry to construct base panel",
            "BrownBat", "Construct")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("PanelBrep", "P", "Input Panel Brep", GH_ParamAccess.list);
            pManager.AddTextParameter("PanelName", "N", "Input Panel Name", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("PanelGeometry", "G", "Panel Geometry", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Brep> inputPanels = new List<Brep>();
            List<string> inputNames = new List<string>();
            DA.GetDataList(0, inputPanels);
            DA.GetDataList(1, inputNames);
            
            List<Panel> outputPanels = new List<Panel>();
            for (int p = 0;  p < inputPanels.Count; p++)
            {

                BrepVertexList profileVertexList = inputPanels[p].Vertices;

                List<Point3d> profileVertices = new List<Point3d>();
                for (int i = 0; i < profileVertexList.Count; i++)
                {
                    Point3d vertex = profileVertexList[i].Location;
                    profileVertices.Add(vertex);
                }
                double xStartProfile = profileVertices.OrderBy(v => v.X).Select(v => v.X).First();
                double yStartProfile = profileVertices.OrderByDescending(v => v.Y).Select(v => v.Y).First();
                double ySmallest = profileVertices.OrderBy(v => v.Y).Select(v => v.Y).First();
                double xLargest = profileVertices.OrderByDescending(v => v.X).Select(v => v.X).First();

                Vector3d xDirection = new Vector3d(xLargest - xStartProfile, 0, 0);
                Vector3d yDirection = new Vector3d(0, yStartProfile - ySmallest, 0);

                Point3d profileStart = new Point3d(xStartProfile, yStartProfile, 0);
                Plane originPlane = new Plane(profileStart, xDirection, yDirection);
                Panel panel = new Panel(inputNames[p], originPlane, inputPanels[p]);
                outputPanels.Add(panel);
            }

            DA.SetDataList(0, outputPanels);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("fce74e46-ad63-4d91-987a-99861de03e11");
    }
}