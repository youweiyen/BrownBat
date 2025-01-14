using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using BrownBat.CalculateHelper;
using BrownBat.Components;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace BrownBat.Arrange
{
    public class GH_ArrangeGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_ArrangeGrid class.
        /// </summary>
        public GH_ArrangeGrid()
          : base("ArrangeGrid", "AG",
              "Respect boundary and arranging elements as grid",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element with selected range of heat value", GH_ParamAccess.list);
            pManager.AddPointParameter("BoundaryPoint", "B", "Boundary point area to assign high conductivity values", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Difference", "D",
                "Heat area axis size difference. Default set to 10",
                GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "G", "Transformed element geometry", GH_ParamAccess.tree);
            pManager.AddTextParameter("Name", "N", "Element Name", GH_ParamAccess.tree);
            pManager.AddTransformParameter("Transform", "T", "Rotation transformation of grid element", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            Structure inStructure = new Structure();
            GH_Structure<IGH_Goo> inRegion = new GH_Structure<IGH_Goo>();
            double inDifference = 10;

            DA.GetDataList(0, inElement);
            DA.GetData(1, ref inStructure);
            DA.GetDataTree(2, out inRegion);
            DA.GetData(3, ref inDifference);

            for (int i = 0; i < inRegion.Branches.Count(); i++)
            {
                if (inRegion[i].Count < 0)
                {
                    continue;
                }
                List<Point3d> regionPoints = new List<Point3d>();
                foreach (var r in inRegion[i])
                {
                    r.CastTo<Point3d>(out Point3d pt);
                    regionPoints.Add(pt);
                }

                Rectangle3d boundingBox = AreaHelper.MinBoundingBox(regionPoints, inElement[i].Origin);

                if (boundingBox.Height - boundingBox.Width > 0)
                {
                    
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
            get { return new Guid("020DB6E6-B69A-42B2-919C-BC2D25854014"); }
        }
    }
}