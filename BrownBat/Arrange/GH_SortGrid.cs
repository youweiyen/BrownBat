using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using BrownBat.Components;
using System.Linq;
using BrownBat.CalculateHelper;

namespace BrownBat.Arrange
{
    public class GH_SortGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_PlaceGrid class.
        /// </summary>
        public GH_SortGrid()
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
            pManager.AddNumberParameter("Difference", "D", "Area difference. Default set to 100", GH_ParamAccess.item);
            pManager.AddNumberParameter("Seed", "S", 
                                        "Seed number to set to see different options. The options are ranked by highest coverage of high temperature areas. " +
                                        "Default set to see top 10",
                                        GH_ParamAccess.item);
            pManager[4].Optional = true;
            pManager[5].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Sorted Element", GH_ParamAccess.list);
            pManager.AddTextParameter("ElementNames", "EN", "Sorted Element Names", GH_ParamAccess.list);
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
            double inDifference = 100;
            int inSeed = 10;


            DA.GetDataList(0, inElement);
            DA.GetDataTree(1, out inPlaceRegion);
            DA.GetDataList(2, inPlacePosition);
            DA.GetDataList(3, inOverArea);
            DA.GetData(4, ref inDifference);
            DA.GetData(5, ref inSeed);

            var elementHasCluster = inElement.Where(e => e.HeatClusterGroup != null);

            //sort by how many points are over temperature(area) *Place Position Sort
            List<IEnumerable<Point3d>> regionPoint = new List<IEnumerable<Point3d>>();
            List<int> branchNumber = new List<int>();
            for (int pts = 0; pts < inPlaceRegion.Branches.Count(); pts++)
            {
                var rhinoPoint = inPlaceRegion[pts].Select(gh => gh.Value);
                regionPoint.Add(rhinoPoint);
                branchNumber.Add(pts);
            }

            var sortPositionRegion = inPlacePosition.Zip(inOverArea, (place, over) => new { geo = place, area = over })
                                                    .Zip(branchNumber, (x, y) => new { position = x, branch = y })
                                                    .OrderByDescending(pair => pair.position.area);

            List<Brep> sortPlacePosition = sortPositionRegion.Select(pair => pair.position.geo).ToList();
            List<int> sortPlaceRegionBranch = sortPositionRegion.Select(pair => pair.branch).ToList();
            List<double> sortOverArea = sortPositionRegion.Select(pair => pair.position.area).ToList();

            //find fitting area
            Dictionary<int, IEnumerable<Element>> firAreaPair = new Dictionary<int, IEnumerable<Element>>();
            for (int i = 0; i < sortPlacePosition.Count; i++)
            {

                List<Element> similiarClusterElement = new List<Element>();
                int branchID = sortPlaceRegionBranch[i];
                if (inPlaceRegion[branchID].Count != 0)
                {
                    similiarClusterElement =
                    elementHasCluster.Where(e =>
                    Math.Abs(e.HeatClusterGroup.Sum(hcg =>
                    hcg.Value.XAxis.Length * hcg.Value.YAxis.Length) - sortOverArea[i]) < inDifference)
                    .ToList();
                }

                firAreaPair.Add(i, similiarClusterElement);
            }

            //order by shape difference *Element Sort
            Dictionary<int, IEnumerable<Element>> orderShapePair = new Dictionary<int, IEnumerable<Element>>();
            foreach (var pairs in firAreaPair)
            {
                int branchID = sortPlaceRegionBranch[pairs.Key];
                Rectangle3d placeBoundBox = AreaHelper.MinBoundingBox(regionPoint[branchID], AreaHelper.PlacePlane(sortPlacePosition[pairs.Key]));
                double height = placeBoundBox.Height;
                double width = placeBoundBox.Width;

                List<double> axisDifference = new List<double>();
                foreach (var elements in pairs.Value)
                {
                    var largestCluster = elements.HeatClusterGroup.OrderByDescending(hcg => hcg.Value.XAxis.Length * hcg.Value.YAxis.Length).First();

                    double heightAsX = Math.Abs(largestCluster.Value.XAxis.Length - height) + Math.Abs(largestCluster.Value.YAxis.Length - width);
                    double widthAsX = Math.Abs(largestCluster.Value.XAxis.Length - width) + Math.Abs(largestCluster.Value.YAxis.Length - height);

                    if (heightAsX < widthAsX) 
                    {
                        axisDifference.Add(heightAsX);
                    }
                    else
                    {
                        axisDifference.Add(widthAsX);
                    }
                }
                var similiarShape = pairs.Value.Zip(axisDifference, (e, diff) => new { element = e, difference = diff}).OrderByDescending(pair => pair.difference).Select(pair => pair.element).ToList();
                orderShapePair.Add(pairs.Key, similiarShape);
            }

            //show the top possibilities and reorder back to original position
            List<string> elementNames = new List<string>();
            BuildPossibleCombination(0, orderShapePair, new List<string>(), ref elementNames, inSeed);

            DA.SetDataList(1, elementNames);
            //if (inSeed > optionCount)
            //{ throw new Exception("ran out of optimized options"); }

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
        public static void BuildPossibleCombination(int level, Dictionary<int, IEnumerable<Element>> elementPlacePair, List<string> output, ref List<string> elementNames, int queryNum)
        {
            //if (level < elementPlacePair.Count)
            string a = default;
            if (level < elementPlacePair.Count)
            {
                foreach (var value in elementPlacePair[level])
                {
                    List<string> resultList = new List<string>();
                    resultList.AddRange(output);
                    resultList.Add(value.Name);
                    if (resultList.Count == elementPlacePair.Count)
                    {
                        a = string.Join(", ", resultList);
                    }
                    BuildPossibleCombination(level + 1, elementPlacePair, resultList, ref elementNames, queryNum);
                }
            }
            elementNames.Add(a);
        }
    }
}