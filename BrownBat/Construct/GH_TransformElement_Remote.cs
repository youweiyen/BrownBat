using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BrownBat.Components;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Rhinoceros.Model;
using Rhino.Display;
using Rhino.Geometry;

namespace BrownBat.Construct
{
    public class GH_TransformElement_Remote : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_Param class.
        /// </summary>
        public GH_TransformElement_Remote()
          : base("TransformElement", "TE",
              "Transform model block and convert it to element block",
              "BrownBat", "Construct")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ModelBlockInstance", "MI", "Block Instance of design model", GH_ParamAccess.list);
            pManager.AddTransformParameter("Transform", "T", 
                "If you gave the block any transformation you can apply it here", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ElementBlock", "EB", "Element with block properties", GH_ParamAccess.list);
            //pManager.AddGenericParameter("brep", "B", "b", GH_ParamAccess.list);

        }

        List<Brep> brepList = new List<Brep>();
        private DisplayMaterial mat = new DisplayMaterial(Color.Blue);
        private Mesh previewMesh = new Mesh();
        //public override void DrawViewportMeshes(IGH_PreviewArgs args)
        //{
        //    if (!Locked && previewMesh != null && previewMesh.IsValid)
        //    {
        //        args.Display.DrawMeshShaded(previewMesh, mat);
        //    }

        //}
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

            List<Element> panelList = new List<Element>();
            Mesh meshes = new Mesh();

            for (int i = 0; i < inputBlock.Count; i++)
            {
                string panelName = inputBlock[i].InstanceDefinition.Name;

                Transform panelTransform = new Transform();
                if (inputTransform.Count == 0)
                {
                    panelTransform = Transform.ZeroTransformation;

                }
                else
                {
                    panelTransform = inputTransform[i];
                }
                Transform nonTransform = new Transform(1);


                BoundingBox panelBox = inputBlock[i].GetBoundingBox(nonTransform);
                Brep panelBrep = panelBox.ToBrep();
                Mesh panelMesh = Mesh.CreateFromBox(panelBox, 2, 2, 2);

                meshes.Append(panelMesh);


                //string n = inputModel[i].TypeName;
                //inputModel.Where(b => b.TypeName == "Block Instance").ToList().First().CastTo(out Brep brep);
                //breps.Add(brep);
                //Brep panelBrep = inputBrep[i];

                Element panel = new Element(panelName, panelTransform, panelBrep);
                Element.TryGetInverseMatrix(panel, panelTransform);
                Element.BaseCurve(panel);
                panelList.Add(panel);
            }

            previewMesh = meshes;

            DA.SetDataList(0, panelList);
            //DA.SetDataList(1, brepList);

                
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {

                //You can add image files to your project resources and access them like this:
                return Properties.Resources.baticon;
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