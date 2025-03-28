﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System.Linq;
using BrownBat.Components;
using Grasshopper.Kernel.Data;
using Grasshopper;
using Eto.Forms;

namespace BrownBat.Construct
{
    public class GH_ConstructStructure : GH_Component
    {
        public GH_ConstructStructure()
          : base("ConstructStructure", "CS",
              "Construct Structure Pixels for calculation",
              "BrownBat", "Construct")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Structure Brep", GH_ParamAccess.item);
            pManager.AddIntegerParameter("UPixelCount", "N", "Number of Pixels in U", GH_ParamAccess.item);
            pManager.AddIntegerParameter("VPixelCount", "N", "Number of Pixels in V", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "N", "Structure Name", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Structure", "S", "Structure with pixel value", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "P", "Point", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep inputWall = new Brep();
            int inputUNumber = default;
            int inputVNumber = default;
            string inputName = default;
            DA.GetData(0, ref inputWall);
            DA.GetData(1, ref inputUNumber);
            DA.GetData(2, ref inputVNumber);
            DA.GetData(3, ref inputName);
            
            BrepFaceList faces = inputWall.Faces;
            Brep topSurface = faces.OrderByDescending(f => f.PointAt(0.5, 0.5).Z).First().ToBrep();

            BrepEdgeList edgeList = topSurface.Edges;

            List<Curve> sortedEdge = edgeList.Select(e => e.EdgeCurve)
                                    .OrderByDescending(edge => edge.PointAtNormalizedLength(0.5).Y)
                                    .ToList();
            double copyDistance = sortedEdge[1].GetLength() / inputVNumber;

            Curve topCurve = sortedEdge[0].DuplicateCurve();
            if (sortedEdge[0].PointAtStart.X > sortedEdge[0].PointAtEnd.X)
            {
                topCurve.Reverse();
            }

            double[] divideBool = topCurve.DivideByCount(inputUNumber, true, out Point3d[] firstRowPoints);
            Array.Resize(ref firstRowPoints, firstRowPoints.Length - 1);
            Pixel[] firstPixelRow = firstRowPoints.Select((p, index) => new Pixel(p, (0, index))).ToArray();

            Point3d topPoint = sortedEdge[0].PointAtNormalizedLength(0.5);
            Point3d bottomPoint = sortedEdge[3].PointAtNormalizedLength(0.5);
            Vector3d copyDirection = new Vector3d(bottomPoint.X - topPoint.X
                                                , bottomPoint.Y - topPoint.Y
                                                , 0);
            copyDirection.Unitize();
            List<Pixel[]> pointRowList = new List<Pixel[]> { firstPixelRow };

            DataTree<Point3d> wallPoints = new DataTree<Point3d>();
            wallPoints.AddRange(firstRowPoints, new GH_Path(0));

            Curve moveEdge = topCurve.DuplicateCurve();
            Transform moveTransform = Transform.Translation(copyDirection * copyDistance);
            for (int i = 0; i < inputVNumber - 1; i++)
            {
                moveEdge.Transform(moveTransform);

                moveEdge.DivideByCount(inputUNumber, true, out Point3d[] rowPoints);
                Array.Resize(ref rowPoints, rowPoints.Length - 1);
                Pixel[] pixelRow = rowPoints.Select((p, index) => new Pixel(p, (i+1, index))).ToArray();
                pointRowList.Add(pixelRow);
                wallPoints.AddRange(rowPoints, new GH_Path(i+1));
            }
            
            Structure wall = new Structure(inputName, pointRowList, inputWall, (inputUNumber, inputVNumber));

            DA.SetData(0, wall);
            DA.SetDataTree(1, wallPoints);
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
            get { return new Guid("6A9F3673-6D6B-407F-A8FE-20364B92569B"); }
        }
    }
}