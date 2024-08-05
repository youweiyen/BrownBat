using System;
using System.Collections.Generic;
using System.Linq;
using BrownBat.Components;
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
              "Import Panel Block to Bat Object",
              "BrownBat", "Param")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Block Instance", "BI", "Block Instance", GH_ParamAccess.list);
            pManager.AddTransformParameter("Transform", "T", "Block Transform", GH_ParamAccess.list);
            pManager.AddBrepParameter("Model Panel", "P", "Model as Brep", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("BatPanel", "B", "Bat Panel Object", GH_ParamAccess.list);
            pManager.AddBrepParameter("ModelBrep", "M", "Model as Brep", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<GH_InstanceReference> inputBlock = new List<GH_InstanceReference>();
            List<Transform> inputTransform = new List<Transform>();
            List<Brep> inputBrep = new List<Brep>();

            DA.GetDataList(0, inputBlock);
            DA.GetDataList(1, inputTransform);
            DA.GetDataList(2, inputBrep);

            List<Panel> panelList = new List<Panel>();
            List<Brep> brepList = new List<Brep>();
            for (int i = 0; i < inputBlock.Count; i++)
            {
                string panelName = inputBlock[i].InstanceDefinition.Name;

                //Transform panelTransformation = inputModel[i].ModelTransform;
                Transform panelTransform = inputTransform[i];
                
                //string n = inputModel[i].TypeName;
                //inputModel.Where(b => b.TypeName == "Block Instance").ToList().First().CastTo(out Brep brep);
                //breps.Add(brep);
                Brep panelBrep = inputBrep[i];
                Panel panel = new Panel(panelName, panelTransform, panelBrep);

                Panel.TryGetInverseMatrix(panel, panelTransform);
                Panel.BaseCurve(panel);

                panelList.Add(panel);
                brepList.Add(panelBrep);
            }

            DA.SetDataList(0, panelList);
            DA.SetDataList(1, brepList);

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