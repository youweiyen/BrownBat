using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.Geometry.Intersect;
using BrownBat.Components;
using System.Linq;
using System.Diagnostics;
using static Rhino.Render.ChangeQueue.Light;
using static System.Net.Mime.MediaTypeNames;

namespace BrownBat.Calculate
{
    public class GH_Overlap_CSV : GH_Component
    {
        public GH_Overlap_CSV()
          : base("Overlap_CSV", "OC",
              "Calculate overlapping pixels using CSV data",
              "BrownBat", "Calculate")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("BatElement", "B", "Bat object with geometry and csv data", GH_ParamAccess.list);
            pManager.AddGenericParameter("Structure", "S", "Structure with pixel points", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Structure", "S", "Structure", GH_ParamAccess.list);
            pManager.AddTextParameter("Stopwatch", "s", "Stopwatch", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inputModelPanel = new List<Element>();
            Structure inputWall = new Structure();
            DA.GetDataList(0, inputModelPanel);
            DA.GetData(1, ref inputWall);

            #region intersectCalculation
            ////intersect calculation
            //List<BrepFace> topSurfaces = new List<BrepFace>();
            //foreach (Brep profile in inputModelBrep)
            //{
            //    BrepFaceList faces = profile.Faces;
            //    BrepFace sortedSurface = faces
            //                    .OrderByDescending(f => f.PointAt(0.5, 0.5).Z)
            //                    .First();
            //    topSurfaces.Add(sortedSurface);
            //}
            //double projectLength = topSurfaces.Select(srf => srf.PointAt(0.5, 0.5).Z)
            //                                .OrderByDescending(p => p)
            //                                .First();
            #endregion
            List<Pixel[]> wallPixels = inputWall.Pixel;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            for (int rowPoint = 0; rowPoint < wallPixels.Count; rowPoint++)
            {
                foreach(Pixel pixel in wallPixels[rowPoint])
                {
                    List<string> intersectPanelNames = new List<string>();
                    Dictionary<string, (double, double)> panelToPosition = new Dictionary<string, (double, double)>();
                    Dictionary<string, (int, int)> panelToDomain = new Dictionary<string, (int, int)>();

                    for (int i = 0; i < inputModelPanel.Count(); i++)
                    {
                        //point in curve calculation
                        PointContainment containment = inputModelPanel[i].GeometryBaseCurve.Contains(pixel.PixelGeometry, Plane.WorldXY, 0.02);

                        if (containment == PointContainment.Unset)
                        {
                            throw new Exception("curve is not valid");
                        }
                        if (containment == PointContainment.Inside)
                        {

                            #region TransformationToStandardPanel
                            //Panel intersectModelPanel = inputModelPanel.Where(panel => panel.Name == intersectPanelName).First();
                            //Panel intersectOriginPanel = inputOriginPanel.Where(panel => panel.Name == intersectPanelName).First();
                            //Transform matrix = Transform.PlaneToPlane(intersectModelPanel.Origin, intersectOriginPanel.Origin);
                            //Point3d orientPoint = new Point3d(pixel.PixelGeometry);
                            //orientPoint.Transform(matrix);
                            //Point3d origin = intersectOriginPanel.Origin.Origin;
                            //double xPosition = Math.Abs(origin.X - orientPoint.X);
                            //double yPosition = Math.Abs(origin.Y - orientPoint.Y);
                            //(double, double) intersectPanelPosition = (xPosition, yPosition);

                            //int xDomain = (int)Math.Round(xPosition * (intersectOriginPanel.PixelShape.Item1 / intersectOriginPanel.GeometryShape.Item1));
                            //int yDomain = (int)Math.Round(yPosition * (intersectOriginPanel.PixelShape.Item2 / intersectOriginPanel.GeometryShape.Item2));

                            #endregion

                            Element intersectPanel = inputModelPanel[i];
                            string intersectPanelName = intersectPanel.Name;
                            intersectPanelNames.Add(intersectPanelName);

                            Point3d orientPoint = new Point3d(pixel.PixelGeometry);
                            Transform matrix = intersectPanel.InverseMatrix;
                            orientPoint.Transform(matrix);

                            double xPosition = Math.Abs(0 - orientPoint.X);
                            double yPosition = Math.Abs(0 - orientPoint.Y);
                            int xDomain = (int)Math.Floor(xPosition * (intersectPanel.PixelShape.Item1 / intersectPanel.GeometryShape.Item1));
                            int yDomain = (int)Math.Floor(yPosition * (intersectPanel.PixelShape.Item2 / intersectPanel.GeometryShape.Item2));

                            (int, int) intersectPanelDomain = (xDomain, yDomain);

                            //panelToPosition.Add(intersectPanelName, intersectPanelPosition);
                            panelToDomain.Add(intersectPanelName, intersectPanelDomain);
                        }
                        #region intersectCalculation
                        ////intersect calculation
                        //Line positionLine = new Line(pixel.PixelGeometry, Vector3d.ZAxis, projectLength + 10);
                        //twallpoints.Add(pixel.PixelGeometry);
                        //Curve positionCurve = positionLine.ToNurbsCurve();
                        //tcurves.Add(positionCurve);
                        //bool projectedPoint = Intersection.CurveBrepFace
                        //                                    (positionCurve,
                        //                                    topSurfaces[i],
                        //                                    0.01,
                        //                                    out Curve[] overlapCurves,
                        //                                    out Point3d[] intersectionPoints);

                        //if (projectedPoint == true && intersectionPoints.Count() != 0)
                        //{
                        //    Point3d intersectPoint = intersectionPoints[0];
                        //    string intersectPanelName = inputModelPanel[i].Name;
                        //    intersectPanelNames.Add(intersectPanelName);

                        //    Panel intersectModel = inputModelPanel.Where(panel => panel.Name == intersectPanelName).First();
                        //    Panel intersectPanel = inputModelPanel.Where(panel => panel.Name == intersectPanelName).First();
                        //    Transform matrix = Transform.PlaneToPlane(intersectModel.Origin, intersectPanel.Origin);
                        //    Point3d orientPoint = new Point3d(pixel.PixelGeometry);
                        //    orientPoint.Transform(matrix);
                        //    Point3d origin = intersectPanel.Origin.Origin;
                        //    double xPosition = Math.Abs(origin.X - orientPoint.X);
                        //    double yPosition = Math.Abs(origin.Y - orientPoint.Y);
                        //    (double, double) intersectPanelPosition = (xPosition, yPosition);

                        //    int xDomain = (int)Math.Round(xPosition * (inputWall.PixelShape / inputWall.GeometryShape.Item1));
                        //    int yDomain = (int)Math.Round(yPosition * (inputWall.PixelShape / inputWall.GeometryShape.Item2));

                        //    (int, int) intersectPanelDomain = (xDomain, yDomain);

                        //    panelToPosition.Add(intersectPanelName, intersectPanelPosition);
                        //    panelToDomain.Add(intersectPanelName, intersectPanelDomain);
                        //}
                        #endregion

                    }
                    Pixel.SetOverlapPanels(pixel, intersectPanelNames);
                    Pixel.SetPixelDomain(pixel, panelToDomain);
                    //if(intersectPanelNames.Any())
                    //{
                    //}
                    ////Pixel.SetPixelPosition(pixel, panelToPosition);
                    //if (intersectPanelNames.Any())
                    //{
                    //}
                }

            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);

            DA.SetData(0, inputWall);
            DA.SetData(1, elapsedTime);


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
                return Properties.Resources.baticon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0964DA8F-5345-467D-B581-9A5176D74DEE"); }
        }
    }
}