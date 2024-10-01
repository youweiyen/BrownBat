using System;
using System.Collections.Generic;
using BrownBat.Components;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Render;

namespace BrownBat.View
{
    public class GH_MapMaterial : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MaterialMap class.
        /// </summary>
        public GH_MapMaterial()
          : base("MapMaterial", "M",
              "Map Bitmap to Material",
              "BrownBat", "View")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Bat Element", GH_ParamAccess.list);
            pManager.AddTextParameter("Path", "P", "Source Path", GH_ParamAccess.list);
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
            List<Element> inElement =  new List<Element>();
            List<string> inPath = new List<string>();


            var rhinoTexture = Rhino.Render.RenderTexture.NewBitmapTexture(inPath[0], Rhino.RhinoDoc.ActiveDoc);
            var texture = rhinoTexture.SimulatedTexture(RenderTexture.TextureGeneration.Allow).Texture();
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
            get { return new Guid("7B836BD2-3C43-4602-B079-49156121F37C"); }
        }
    }
}