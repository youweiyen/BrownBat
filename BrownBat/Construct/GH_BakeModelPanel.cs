using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using BrownBat.Components;

namespace BrownBat.Construct
{
    public class GH_BakeModelPanel : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_BakeModelingPanel class.
        /// </summary>
        public GH_BakeModelPanel()
          : base("BakeModelPanel", "B",
              "Bake Model and origin plane to modify manually",
              "BrownBat", "Construct")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("PanelGeometry", "G", "Panel Geometry", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Bake", "B", "Bake", GH_ParamAccess.item); 
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
            List<Panel> inputPanel = new List<Panel>();
            DA.GetDataList(0, inputPanel);

            Rhino.DocObjects.ObjectAttributes objectAttributes = new Rhino.DocObjects.ObjectAttributes();
            objectAttributes.Name = inputPanel[0].Name;
            Plane origin = inputPanel[0].Origin;
            Brep brep = inputPanel[0].Model;
            RhinoDoc.ActiveDoc.Objects.AddBrep(brep, objectAttributes);

            //if (colour.GetHashCode() == 0) colour = System.Drawing.Color.Black;

            //if (toggle)
            //{

            //    int index = doc.Layers.FindByFullPath(parent, -1);
            //    if (index < 0) doc.Layers.Add(parent, System.Drawing.Color.Black);
            //    index = doc.Layers.FindByFullPath(parent, -1);
            //    Rhino.DocObjects.Layer parent_layer = doc.Layers[index];


            //    // Create a child layer

            //    string child_name = child;
            //    Rhino.DocObjects.Layer childlayer = new Rhino.DocObjects.Layer();
            //    childlayer.ParentLayerId = parent_layer.Id;
            //    childlayer.Name = child_name;
            //    childlayer.Color = colour;

            //    string children_name = parent + "::" + child;

            //    index = doc.Layers.FindByFullPath(children_name, -1);
            //    if (index < 0) index = doc.Layers.Add(childlayer);

            //    Rhino.DocObjects.ObjectAttributes att = new Rhino.DocObjects.ObjectAttributes();

            //    att.LayerIndex = index;
            //    doc.Objects.Add(Geo, att);
            //}
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
            get { return new Guid("31E7FA91-FB24-4B1E-A350-ED57040903FA"); }
        }
    }
}