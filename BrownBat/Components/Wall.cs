using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace BrownBat.Components
{
    public class Wall
    {
        public string Name { get;}
        public List<Pixel[]> Pixel { get; }
        public Brep Model { get; }

        public int PixelShape {  get; }
        public (double, double) GeometryShape { get; private set; }


        public Wall() { }
        public Wall(string name, List<Pixel[]> pixel, Brep model, int pixelShape)
        {
            Name = name;
            Pixel = pixel;
            Model = model;
            PixelShape = pixelShape;
        }
        public static void WallShape(Wall wall)
        {
            Brep model = wall.Model;
            BrepFaceList faces = model.Faces;
            BrepFace topSurface = faces.OrderByDescending(f => f.PointAt(0.5, 0.5)).First();

            double width;
            double height;
            bool sizeBool = topSurface.GetSurfaceSize(out width, out height);
            wall.GeometryShape = (width, height);
        }

    }
}
