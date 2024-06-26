using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using Rhino.Geometry.Intersect;
using BrownBat.Components;
using System.Linq;

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
            pManager.AddGeometryParameter("brep", "b", "interbrep", GH_ParamAccess.list);


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

            List<BrepFace> topSurfaces = new List<BrepFace>();
            foreach (Brep profile in inputModelBrep)
            {
                BrepFaceList faces = profile.Faces;
                BrepFace sortedSurface = faces
                                .OrderByDescending(f => f.PointAt(0.5, 0.5).Z)
                                .First();
                topSurfaces.Add(sortedSurface);
            }
            double projectLength = topSurfaces.Select(srf => srf.PointAt(0.5, 0.5).Z)
                                            .OrderByDescending(p => p)
                                            .First();

            List<Pixel[]> wallPixels = inputWall.Pixel;
            List<Brep> tBrep = new List<Brep> { topSurfaces[0].ToBrep(), topSurfaces[1].ToBrep() };

            List<Point3d> twallpoints = new List<Point3d>();
            List<Curve> tcurves = new List<Curve>();

            for (int rowPoint = 0; rowPoint < wallPixels.Count; rowPoint++)
            {
                foreach(Pixel pixel in wallPixels[rowPoint])
                {
                    List<string> intersectPanelNames = new List<string>();
                    Dictionary<string, (double, double)> panelToPosition = new Dictionary<string, (double, double)>();
                    Dictionary<string, (int, int)> panelToDomain = new Dictionary<string, (int, int)>();

                    for (int i = 0; i < topSurfaces.Count(); i++)
                    {
                        Line positionLine = new Line(pixel.PixelGeometry, Vector3d.ZAxis, projectLength + 10);
                        twallpoints.Add(pixel.PixelGeometry);
                        Curve positionCurve = positionLine.ToNurbsCurve();
                        tcurves.Add(positionCurve);
                        bool projectedPoint = Intersection.CurveBrepFace
                                                            (positionCurve,
                                                            topSurfaces[i],
                                                            0.01,
                                                            out Curve[] overlapCurves,
                                                            out Point3d[] intersectionPoints);
                        if (projectedPoint == true && intersectionPoints.Count() != 0)
                        {
                            Point3d intersectPoint = intersectionPoints[0];
                            string intersectPanelName = inputModelPanel[i].Name;
                            intersectPanelNames.Add(intersectPanelName);

                            Panel intersectModel = inputModelPanel.Where(panel => panel.Name == intersectPanelName).First();
                            Panel intersectPanel = inputModelPanel.Where(panel => panel.Name == intersectPanelName).First();
                            Transform matrix = Transform.PlaneToPlane(intersectModel.Origin, intersectPanel.Origin);
                            Point3d orientPoint = new Point3d(pixel.PixelGeometry);
                            orientPoint.Transform(matrix);
                            Point3d origin = intersectPanel.Origin.Origin;
                            double xPosition = Math.Abs(origin.X - orientPoint.X);
                            double yPosition = Math.Abs(origin.Y - orientPoint.Y);
                            (double, double) intersectPanelPosition = (xPosition, yPosition);

                            //int xDomain = (int) Math.Round(xPosition * (inputWall.PixelShape / inputWall.GeometryShape.Item1));
                            //int yDomain = (int) Math.Round(yPosition * (inputWall.PixelShape / inputWall.GeometryShape.Item2));
                            int xDomain = 1;
                            int yDomain = 1;
                            (int, int) intersectPanelDomain = (xDomain, yDomain);

                            panelToPosition.Add(intersectPanelName, intersectPanelPosition);
                            panelToDomain.Add(intersectPanelName, intersectPanelDomain);
                        }
                    }
                    Pixel.SetOverlapPanels(pixel, intersectPanelNames);
                    Pixel.SetPixelPosition(pixel, panelToPosition);
                    Pixel.SetPixelDomain(pixel, panelToDomain);
                }

            }
            DA.SetData(0, inputWall);
            DA.SetDataList(1, twallpoints);
            DA.SetDataList(2, tcurves);
            DA.SetDataList(3, tBrep);


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