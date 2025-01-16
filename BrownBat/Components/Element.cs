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
using System.Windows.Forms;

namespace BrownBat.Components
{
    public class Element: IGH_GeometricGoo, IGH_Goo
    {
        public string Name { get; }
        public Plane Origin { get; private set; }
        public Brep Model { get; }
        public Mesh ModelMesh { get; }
        public Dictionary<int, HeatCluster> HeatClusterGroup { get; private set; }

        public Transform Matrix { get; }
        public Transform InverseMatrix { get; private set; }
        public (double, double) GeometryShape { get; private set; }
        public double GeometryThickness { get; private set; }
        public Curve GeometryBaseCurve { get; private set; }

        public List<double[]> PixelTemperature { get; private set; }
        public List<double[]> PixelConductivity { get; private set; }

        public (int, int) PixelShape { get; private set; }
        public (double, double) PixelSize { get; private set; }

        public static void SetPanelTemperature(Element panel, List<double[]> pixelTemperature) => panel.PixelTemperature = pixelTemperature;
        public static void SetPanelConductivity(Element panel, List<double[]> pixelConductivity) => panel.PixelConductivity = pixelConductivity;
        public static void SetOrigin(Element panel, Plane origin) => panel.Origin = origin;
        public void SetHeatCluster(Dictionary<int, HeatCluster> heatCluster)
        {
            HeatClusterGroup = heatCluster;
        } 


        public BoundingBox Boundingbox => throw new NotImplementedException();

        public Guid ReferenceID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsReferencedGeometry => true;

        public bool IsGeometryLoaded => true;

        public bool IsValid => true;

        public string IsValidWhyNot => throw new NotImplementedException();

        public string TypeName => throw new NotImplementedException();

        public string TypeDescription => throw new NotImplementedException();


        public Element(string name)
        {
            Name = name;
        }
        public Element(string name, Plane origin, Brep panel)
        {
            Name = name;
            Origin = origin;
            Model = panel;
        }
        public Element(string name, Transform matrix, Brep panel)
        {
            Name = name;
            Matrix = matrix;
            Model = panel;
        }
        public Element(string name, Transform inverseMatrix, Brep panel, Curve geometryBaseCurve, (int, int) pixelShape, List<double[]> pixelConductivity) 
        {
            Name = name;
            InverseMatrix = inverseMatrix;
            Model = panel;
            PixelShape = pixelShape;
            PixelConductivity = pixelConductivity;
            GeometryBaseCurve = geometryBaseCurve;
        }
        public static void ModelShape(Element panel) 
        {
            Brep model = panel.Model;
            BrepFaceList faces = model.Faces;
            BrepFace topSurface = faces.OrderByDescending(f => f.PointAt(0.5, 0.5).Z).First();

            double width;
            double height;
            bool sizeBool = topSurface.GetSurfaceSize(out width, out height);
            panel.GeometryShape = (width, height);
        }
        public static void ModelThickness(Element panel)
        {
            Brep model = panel.Model;
            BrepFaceList faces = model.Faces;
            List<double> surfaceZ = faces.Select(f => f.PointAt(0.5, 0.5).Z)
                                            .OrderByDescending(p => p).ToList();
            double surfaceDistance = Math.Abs(surfaceZ.First() - surfaceZ.Last());
            panel.GeometryThickness = surfaceDistance;
        }
        public static void BaseCurve(Element panel)
        {
            IEnumerable<BrepFace> bottomFace = BaseFace(panel);

            Curve[] edgeCurves = bottomFace.First().ToBrep().DuplicateEdgeCurves();
            Curve joinedEdge = Curve.JoinCurves(edgeCurves)[0];

            panel.GeometryBaseCurve = joinedEdge;
        }
        public static void TransformBaseCurve(Element panel, Transform transform)
        {
            IEnumerable<BrepFace> bottomFace = BaseFace(panel);

            Curve[] edgeCurves = bottomFace.First().ToBrep().DuplicateEdgeCurves();
            Curve joinedEdge = Curve.JoinCurves(edgeCurves)[0];
            joinedEdge.Transform(transform);

            panel.GeometryBaseCurve = joinedEdge;
        }
        public static IEnumerable<BrepFace> BaseFace(Element element) 
        {
            Brep model = element.Model;
            BrepFaceList faces = model.Faces;
            //BrepFace bottomFace = faces.OrderBy(f => f.PointAt(0.5, 0.5).Z).First();
            IEnumerable<BrepFace> bottomFaces = faces.Where(f => f.NormalAt(0.5, 0.5).Z < 0);
            return bottomFaces;
        }
        public static IEnumerable<BrepFace> TopFace(Element element)
        {
            Brep model = element.Model;
            BrepFaceList faces = model.Faces;
            //BrepFace topFace = faces.OrderBy(f => f.PointAt(0.5, 0.5).Z).Last();
            IEnumerable<BrepFace> topFaces = faces.Where(f => f.NormalAt(0.5, 0.5).Z > 0);
            return topFaces;
        }
        public static BrepFace TopMeshFaces(Element element)
        {
            Brep model = element.Model;
            BrepFaceList faces = model.Faces;
            BrepFace topFace = faces.OrderBy(f => f.PointAt(0.5, 0.5).Z).Last();
            return topFace;
        }
        public static void CSVShape(Element panel)
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
        public static void GetPixelSize(Element element)
        {
            double xSize= element.GeometryShape.Item1 / element.PixelShape.Item1;
            double ySize = element.GeometryShape.Item2 / element.PixelShape.Item2;
            element.PixelSize = (xSize, ySize);
        }
        public static void TryGetInverseMatrix(Element panel, Transform matrix) 
        {
            if (matrix.IsZeroTransformation)
            {
                panel.InverseMatrix = matrix;
            }
            else 
            {
                bool m = matrix.TryGetInverse(out Transform inverseMatrix);
                if (m == false) { throw new Exception(); }
                panel.InverseMatrix = inverseMatrix;
            }
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
