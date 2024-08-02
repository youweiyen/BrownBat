using Eto.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.Components
{
    public class Panel: IGH_GeometricGoo, IGH_Goo
    {
        public string Name { get; }
        public Plane Origin { get; }
        public Brep Model { get; }
        public Transform Matrix { get; }
        public (double, double) GeometryShape { get; private set; }
        public double GeometryThickness { get; private set; }
        public Curve GeometryBaseCurve { get; private set; }

        public List<double[]> PixelTemperature { get; private set; }
        public List<double[]> PixelConductivity { get; private set; }

        public (int, int) PixelShape { get; private set; }
        public static void SetPanelTemperature(Panel panel, List<double[]> pixelTemperature) => panel.PixelTemperature = pixelTemperature;
        public static void SetPanelConductivity(Panel panel, List<double[]> pixelConductivity) => panel.PixelConductivity = pixelConductivity;




        public BoundingBox Boundingbox => throw new NotImplementedException();

        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsReferencedGeometry => true;

        public bool IsGeometryLoaded => true;

        public bool IsValid => true;

        public string IsValidWhyNot => throw new NotImplementedException();

        public string TypeName => throw new NotImplementedException();

        public string TypeDescription => throw new NotImplementedException();

 
        public Panel(string name, Plane origin, Brep panel)
        {
            Name = name;
            Origin = origin;
            Model = panel;
        }
        public Panel(string name, Transform matrix, Brep panel)
        {
            Name = name;
            Matrix = matrix;
            Model = panel;
        }
        public static void ModelShape(Panel panel) 
        {
            Brep model = panel.Model;
            BrepFaceList faces = model.Faces;
            BrepFace topSurface = faces.OrderByDescending(f => f.PointAt(0.5, 0.5).Z).First();

            double width;
            double height;
            bool sizeBool = topSurface.GetSurfaceSize(out width, out height);
            panel.GeometryShape = (width, height);
        }
        public static void ModelThickness(Panel panel)
        {
            Brep model = panel.Model;
            BrepFaceList faces = model.Faces;
            List<double> surfaceZ = faces.Select(f => f.PointAt(0.5, 0.5).Z)
                                            .OrderByDescending(p => p).ToList();
            double surfaceDistance = Math.Abs(surfaceZ.First() - surfaceZ.Last());
            panel.GeometryThickness = surfaceDistance;
        }
        public static void BaseCurve(Panel panel)
        {
            Brep model = panel.Model;
            BrepFaceList faces = model.Faces;
            BrepFace bottomFace = faces.OrderBy(f => f.PointAt(0.5, 0.5).Z).First();

            Curve[] edgeCurves = bottomFace.ToBrep().DuplicateEdgeCurves();
            Curve joinedEdge = Curve.JoinCurves(edgeCurves)[0];

            panel.GeometryBaseCurve = joinedEdge;
        }
        public static void CSVShape(Panel panel)
        {
            int columnLength = default;
            int rowLength = default;

            if (panel.PixelTemperature != null)
            {
                columnLength = panel.PixelTemperature.Count;
                rowLength = panel.PixelTemperature[0].Count();
            }
            else
            {
                columnLength = panel.PixelConductivity.Count;
                rowLength = panel.PixelConductivity[0].Count();
            }

            panel.PixelShape = (rowLength, columnLength);
        }

        public IGH_GeometricGoo DuplicateGeometry()
        {
            throw new NotImplementedException();
        }

        public BoundingBox GetBoundingBox(Transform xform)
        {
            throw new NotImplementedException();
        }

        public IGH_GeometricGoo Transform(Transform xform)
        {
            throw new NotImplementedException();
        }

        public IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            throw new NotImplementedException();
        }

        public bool LoadGeometry()
        {
            throw new NotImplementedException();
        }

        public bool LoadGeometry(RhinoDoc doc)
        {
            throw new NotImplementedException();
        }

        public void ClearCaches()
        {
            throw new NotImplementedException();
        }

        public IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public IGH_GooProxy EmitProxy()
        {
            throw new NotImplementedException();
        }

        public bool CastFrom(object source)
        {
            throw new NotImplementedException();
        }

        public bool CastTo<T>(out T target)
        {
            throw new NotImplementedException();
        }

        public object ScriptVariable()
        {
            throw new NotImplementedException();
        }

        public bool Write(GH_IWriter writer)
        {
            throw new NotImplementedException();
        }

        public bool Read(GH_IReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
