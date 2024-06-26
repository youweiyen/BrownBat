using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.Components
{
    public class Pixel
    {
        public Point3d PixelGeometry { get; }
        public (int,int) PixelSequence { get; }
        public List<string> OverlapPanels { get; private set; }
        public Dictionary<string, (double, double)> PixelPosition {  get; private set; }
        public Dictionary<string, (int, int)> PixelDomain { get; private set; }
        public double HeatFlux { get; private set; }

        public Pixel(Point3d pixelGeometry, (int,int) pixelSequence)
        {
            PixelGeometry = pixelGeometry;
            PixelSequence = pixelSequence;
        }
        public static void SetOverlapPanels(Pixel pixel, List<string> overlapPanels) => pixel.OverlapPanels = overlapPanels;
        public static void SetPixelPosition(Pixel pixel, Dictionary<string, (double, double)> pixelPosition) => pixel.PixelPosition = pixelPosition;
        public static void SetPixelDomain(Pixel pixel, Dictionary<string, (int, int)> pixelDomain) => pixel.PixelDomain = pixelDomain;
        public static void SetHeatFlux(Pixel pixel, double heatFlux) => pixel.HeatFlux = heatFlux;


    }
}
