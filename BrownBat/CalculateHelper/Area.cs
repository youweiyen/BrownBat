using BrownBat.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.CalculateHelper
{
    public static class Area
    {
        public static double PanelPixelArea(Element panel)
        {
            if (panel.GeometryShape == default)
            {
                Element.ModelShape(panel);
            }
            double width = panel.GeometryShape.Item1;
            double height = panel.GeometryShape.Item2;

            if (panel.PixelShape == default)
            {
                Element.CSVShape(panel);
            }
            int rowCount = panel.PixelShape.Item1;
            int columnCount = panel.PixelShape.Item2;

            double pixelWidthEdge = width / rowCount;
            double pixelHeightEdge = height / columnCount;

            double area = pixelWidthEdge*pixelHeightEdge;

            return area;
        }
        //public static double WallPixelArea(Structure wall)
        //{
        //    if (wall.GeometryShape == default)
        //    {
        //        Structure.WallShape(wall);
        //    }
        //    double width = wall.GeometryShape.Item1;
        //    double height = wall.GeometryShape.Item2;

        //    int segment = wall.PixelShape;

        //    double pixelWidthEdge = width / segment;
        //    double pixelHeightEdge = height / segment;

        //    double area = pixelWidthEdge * pixelHeightEdge;

        //    return area;
        //}


    }
}
