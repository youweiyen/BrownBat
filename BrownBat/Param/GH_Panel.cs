using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Rhinoceros.Model;
using Rhino.Geometry;

namespace BrownBat.Param
{
    public class GH_Panel : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_Param class.
        /// </summary>
        public GH_Panel()
          : base("Panel", "P",
              "Import Model Panel to Bat Object",
              "BrownBat", "Param")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Model Panel", "MP", "Panel and its Origin Plane", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Bat Panel", "BP", "Bat Panel Object", GH_ParamAccess.list);
            pManager.AddBrepParameter("b", "BP", "Bat Panel Object", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_ObjectWrapper> tinputModel;
            GH_Structure<IGH_GeometricGoo> inputModel;

            DA.GetDataTree(0, out inputModel);
            List<Brep> breps = new List<Brep>();
            Dictionary<string, Brep> blankPanel = new Dictionary<string, Brep>();
            Dictionary<string, List<Point3d>> blankPlane = new Dictionary<string, List<Point3d>>();

            for (int i = 0; i < inputModel.Branches.Count; i++)
            {
                string n = inputModel[i][0].TypeName;
                inputModel[i].Where(b => b.TypeName == "Brep").ToList().First().CastTo(out Brep brep);
                breps.Add(brep);

                List<IGH_GeometricGoo> pointGoo = inputModel[i].Where(b => b.TypeName == "Point").ToList();
                List<Point3d> points = new List<Point3d>();
                foreach (IGH_GeometricGoo goo in pointGoo)
                {
                    Point3d point = new Point3d();
                    goo.CastTo<Point3d>(out point);
                    points.Add(point);
                }

            }
            DA.SetDataList(0, breps);
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
            get { return new Guid("E9EC48BE-B3B2-4EE1-B8CB-328EA61BC8C0"); }
        }
    }
}