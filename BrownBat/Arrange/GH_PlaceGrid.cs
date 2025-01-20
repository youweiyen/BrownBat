using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BrownBat.Components;
using System.Linq;

namespace BrownBat.Arrange
{
    public class GH_PlaceGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_PlaceGrid class.
        /// </summary>
        public GH_PlaceGrid()
          : base("SortGrid", "SG",
              "Sort the similiar heat area in relation to element conductivity and background temperature",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element to sort", GH_ParamAccess.list);
            pManager.AddPointParameter("BoundaryPoint", "BP", "Background selected value boundary point", GH_ParamAccess.tree);
            pManager.AddBrepParameter("PlacePosition", "PP", "Element place position", GH_ParamAccess.list);
            pManager.AddNumberParameter("OverArea", "OA", "Area that is over selected value", GH_ParamAccess.list);
            pManager.AddNumberParameter("Difference", "D", "Seed number to change options. Default set to 10", GH_ParamAccess.item);
            pManager.AddNumberParameter("Seed", "S", "Seed number to change options, Default set to 0", GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager[5].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Sorted Element", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inElement = new List<Element>();
            GH_Structure<GH_Point> inPlaceRegion = new GH_Structure<GH_Point>();
            List<Brep> inPlacePosition = new List<Brep>();
            List<double> inOverArea = new List<double>();
            double inDifference = 10;
            double inSeed = 0;


            DA.GetDataList(0, inElement);
            DA.GetDataTree(1, out inPlaceRegion);
            DA.GetDataList(2, inPlacePosition);

            var elementHasCluster = inElement.Where(e => e.HeatClusterGroup != null);

            //sort by how many points are over temperature(area)
            //or sort by how high the tempwerature is

            var sortPosition = inPlacePosition.Join(inOverArea, place => place, over => over, (place, over) => new { geo = place, area = over });
                

            for (int i = 0; i < inPlacePosition.Count; i++)
            {
                IEnumerable<Element> similiarClusterElement;
                if (inPlaceRegion[i].Count != 0)
                {
                    similiarClusterElement =
                    elementHasCluster.Where(e =>
                    Math.Abs(e.HeatClusterGroup.Sum(hcg =>
                    hcg.Value.XAxis.Length * hcg.Value.YAxis.Length) - inOverArea[i]) < inDifference);
                }
                else 
                { 
                    //any
                }
                Dictionary<int, string> placeElementPair = new Dictionary<int, string>();
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
            get { return new Guid("BB136D72-FF45-41DB-AAD5-5CDE6E2FC9BD"); }
        }
    }
}