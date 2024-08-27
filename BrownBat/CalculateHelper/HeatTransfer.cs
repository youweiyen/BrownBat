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
        public static double Resistance(Pixel pixel, List<Element> panels)
        {
            List<string> overlapNames = pixel.OverlapPanels;
            List<Element> overlapPanels = panels.Where(p => overlapNames.Contains(p.Name)).ToList();

            List<double> ratioList = new List<double>();
            foreach (Element panel in overlapPanels)
            {
                (int,int) domain = pixel.PixelDomain[panel.Name];
                int row = domain.Item2;
                int col = domain.Item1;
                double conductivity = panel.PixelConductivity[row][col];
                double thickness = panel.GeometryThickness;
                double ratio = thickness / conductivity;
                ratioList.Add(ratio);
            }
            double ratioSum = ratioList.Sum();
            return ratioSum;
        }
    }
}
