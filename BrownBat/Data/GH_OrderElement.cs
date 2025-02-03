using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BrownBat.Components;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace BrownBat.Data
{
    public class GH_OrderElement : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_OrderElement class.
        /// </summary>
        public GH_OrderElement()
          : base("OrderElement", "OE",
              "Order the Element in the sequence after calculation or construction",
              "BrownBat", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element to reorder", GH_ParamAccess.list);
            pManager.AddTextParameter("Name", "N", "Name sequence to reorder Element", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Reordered Element", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            List<string> inName = new List<string>();
            DA.GetDataList(0, inElement);
            DA.GetDataList(1, inName);

            List<Element> outElement = new List<Element>();
            foreach (string name in inName)
            {
                Element element = inElement.Find(e => e.Name == name);
                outElement.Add(element);
            }

            DA.SetDataList(0, outElement);
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
            get { return new Guid("2B6AF6FE-B61F-4570-B1BB-0294A6520975"); }
        }
    }
}