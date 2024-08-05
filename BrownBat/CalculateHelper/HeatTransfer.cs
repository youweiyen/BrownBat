using BrownBat.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrownBat.CalculateHelper
{
    public static class HeatTransfer
    {
        public static double Coefficient(Pixel pixel, Wall wall, List<Panel> panels)
        {
            List<string> overlapNames = pixel.OverlapPanels;
            List<Panel> overlapPanels = panels.Where(p => overlapNames.Contains(p.Name)).ToList();
            double pixelArea = Area.WallPixelArea(wall);

            List<double> ratioList = new List<double>();
            foreach (Panel panel in overlapPanels)
            {
                (int,int) domain = pixel.PixelDomain[panel.Name];
                int row = domain.Item2;
                int col = domain.Item1;
                double conductivity = panel.PixelConductivity[row][col];
                double thickness = panel.GeometryThickness;
                double ratio = thickness / conductivity;
                ratioList.Add(ratio);
            }
            double overlapRatioSum = ratioList.Sum();
            double U = 1 / (1 / pixelArea) * overlapRatioSum;
            return U;
        }
    }
}
