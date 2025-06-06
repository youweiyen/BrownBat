﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Dbscan.RBush;
using BrownBat.CalculateHelper;
using MIConvexHull;
using BrownBat.Components;
using Dbscan;
using System.IO;
using Rhino.Collections;

namespace BrownBat.Arrange
{
    public class GH_SelectWallValue : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_SelectWallValue class.
        /// </summary>
        public GH_SelectWallValue()
          : base("SelectWallValue", "SWV",
              "Select wall temperature value to place elements",
              "BrownBat", "Arrange")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("PointPosition", "P", "Postion of selected points", GH_ParamAccess.list);
            pManager.AddColourParameter("PointColor", "IC", "Image Color from selected points", GH_ParamAccess.list);
            pManager.AddPointParameter("PaletteColor", "PC", "Palette Color as points constructed from matplotlib", GH_ParamAccess.list);
            pManager.AddNumberParameter("MinTemperature", "MinT", "Select points with temperature over this value", GH_ParamAccess.item);
            pManager.AddNumberParameter("PointDistance", "PD", "Point subdivide distance", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("BoundaryPoint", "B", "Boundary point of area that is over the selected value", GH_ParamAccess.list);
            pManager.AddPointParameter("OverPoints", "P", "Points over temperature", GH_ParamAccess.list);
            pManager.AddNumberParameter("OverArea", "OA", "Area that is over temperature", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> inPoint = new List<Point3d>();
            List<Color> inPointColor = new List<Color>();
            List<Point3d> inPaletteColor = new List<Point3d>();
            double inTemperature = default;
            double inDistance = default;

            DA.GetDataList(0, inPoint);
            DA.GetDataList(1, inPointColor);
            DA.GetDataList(2, inPaletteColor);
            DA.GetData(3, ref inTemperature);
            DA.GetData(4, ref inDistance);

            int minTemp = 10;
            int maxTemp = 40;
            int numOfTemp = inPaletteColor.Count();

            List<double> pixelTemperature = new List<double>();

            for (int p = 0; p < inPointColor.Count; p++)
            {
                List<double> colorDistance = new List<double>();
                foreach (var color in inPaletteColor)
                {
                    colorDistance.Add(ValueDistance(inPointColor, color, p));
                }
                int smallestIndex = colorDistance.IndexOf(colorDistance.Min());
                double pixelTemp = (((maxTemp - minTemp) * smallestIndex) / numOfTemp) + minTemp;
                pixelTemperature.Add(pixelTemp);
            }
            var selectPoints = inPoint
                .Zip(pixelTemperature, (p, t) => new { p, t })
                .Where(pair => pair.t > inTemperature)
                .Select(pair => pair.p);

            //Remove outlier
            List<Point3d> cleanPoints = new List<Point3d>();
            if (selectPoints.Count() > 1)
            { 
                for (int i = 0; i < selectPoints.Count(); i++)
                {
                    List<Point3d> cloudList = selectPoints.ToList();
                    cloudList.RemoveAt(i);
                    PointCloud pointCloud = new PointCloud(cloudList);
                    Point3d currentPoint = selectPoints.ElementAt(i);
                    if (currentPoint
                        .DistanceTo(pointCloud
                        .PointAt(pointCloud.ClosestPoint(currentPoint))) < (inDistance + 1))
                    {
                        cleanPoints.Add(currentPoint);
                    }
                }
            }
            //var cleanPoints = selectPoints.Where(point => point
            //                    .DistanceTo(pointCloud.PointAt(pointCloud.ClosestPoint(point))) < (inDistance + 1));

            List<Point3d> convexPoints = new List<Point3d>();

            if (cleanPoints.Count() > 2)
            {
                //To convexhull points
                var vpoint = cleanPoints.Select(p => new ConvexVertex(p.X, p.Y)).ToArray();
                var hullResult = ConvexHull.Create2D(vpoint, 1e-10).Result;
                if (hullResult != null)
                {
                    //To Rhino points
                    convexPoints = hullResult.Select(h => new Point3d(h.X, h.Y, 0)).ToList();
                }
            }
            double overArea = inDistance * inDistance * selectPoints.Count();

            DA.SetDataList(0, convexPoints);
            DA.SetDataList(1, selectPoints);
            DA.SetData(2, overArea);

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
            get { return new Guid("35CA2BEE-BA30-490E-8805-F944525FE034"); }
        }
        public static double ValueDistance(List<Color> pixels, Point3d color, int num)
        {
            double rDist = Math.Pow(color.X - pixels[num].R, 2);
            double gDist = Math.Pow(color.Y - pixels[num].G, 2);
            double bDist = Math.Pow(color.Y - pixels[num].B, 2);
            double distance = Math.Sqrt(rDist + gDist + bDist);
            return distance;
        }
        public static double Median(IEnumerable<double> inValue)
        {
            double[] sortedValue = inValue.ToArray();

            int count = sortedValue.Length;
            int middleIndex = count / 2;

            if (count % 2 == 0)
            {
                double median = (sortedValue[middleIndex - 1] + sortedValue[middleIndex]) / 2.0;
                return median;
            }
            else
            {
                return sortedValue[middleIndex];
            }
        }
        public static double Mean(IEnumerable<double> inValue)
        {
            double result = inValue.Sum() / inValue.Count();
            return result;
        }
    }
}