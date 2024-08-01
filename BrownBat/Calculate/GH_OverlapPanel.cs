using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.Geometry.Intersect;
using BrownBat.Components;
using System.Linq;
using System.Diagnostics;

namespace BrownBat.Calculate
{
    public class GH_OverlapPanel : GH_Component
    {
        public GH_OverlapPanel()
          : base("OverlapPanel", "Nickname",
              "Description",
              "BrownBat", "Calculate")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ModelPanel", "MP", "Modeled Panel", GH_ParamAccess.list);
            pManager.AddGenericParameter("OriginPanel", "P", "Panel", GH_ParamAccess.list);
            pManager.AddGenericParameter("Wall", "W", "Wall", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Wall", "W", "Wall", GH_ParamAccess.list);
            pManager.AddGeometryParameter("point", "p", "wallpoint", GH_ParamAccess.list);
            pManager.AddGeometryParameter("curve", "c", "pointcurve", GH_ParamAccess.list);
            pManager.AddTextParameter("Stopwatch", "s", "stopwatch", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Panel> inputModelPanel = new List<Panel>();
            List<Panel> inputOriginPanel = new List<Panel>();
            Wall inputWall = new Wall();
            DA.GetDataList(0, inputModelPanel);
            DA.GetDataList(1, inputOriginPanel);
            DA.GetData(2, ref inputWall);

            IEnumerable<Brep> inputModelBrep = inputModelPanel.Select(m => m.Model);

            //point in curve calculation
            List<Curve> tcurves = new List<Curve>();
            foreach (Panel inputPanel in inputModelPanel)
            {
                Panel.BaseCurve(inputPanel);
                tcurves.Add(inputPanel.GeometryBaseCurve);
            }

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
            Wall.WallShape(inputWall);
            List<Point3d> twallpoints = new List<Point3d>();
            for (int rowPoint = 0; rowPoint < wallPixels.Count; rowPoint++)
            {
                foreach (Pixel pixel in wallPixels[rowPoint])
                {
                    twallpoints.Add(pixel.PixelGeometry);
                }
            }

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
                            string intersectPanelName = inputModelPanel[i].Name;
                            intersectPanelNames.Add(intersectPanelName);

                            Panel intersectModel = inputModelPanel.Where(panel => panel.Name == intersectPanelName).First();
                            Panel intersectPanel = inputOriginPanel.Where(panel => panel.Name == intersectPanelName).First();
                            Transform matrix = Transform.PlaneToPlane(intersectModel.Origin, intersectPanel.Origin);
                            Point3d orientPoint = new Point3d(pixel.PixelGeometry);
                            orientPoint.Transform(matrix);
                            Point3d origin = intersectPanel.Origin.Origin;
                            double xPosition = Math.Abs(origin.X - orientPoint.X);
                            double yPosition = Math.Abs(origin.Y - orientPoint.Y);
                            (double, double) intersectPanelPosition = (xPosition, yPosition);

                            int xDomain = (int)Math.Round(xPosition * (inputWall.PixelShape / inputWall.GeometryShape.Item1));
                            int yDomain = (int)Math.Round(yPosition * (inputWall.PixelShape / inputWall.GeometryShape.Item2));

                            (int, int) intersectPanelDomain = (xDomain, yDomain);

                            panelToPosition.Add(intersectPanelName, intersectPanelPosition);
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
                    Pixel.SetPixelPosition(pixel, panelToPosition);
                    Pixel.SetPixelDomain(pixel, panelToDomain);
                }

            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);

            DA.SetData(0, inputWall);
            DA.SetDataList(1, twallpoints);
            DA.SetDataList(2, tcurves);
            DA.SetData(3, elapsedTime);


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
            get { return new Guid("0964DA8F-5345-467D-B581-9A5176D74DEE"); }
        }
    }
}