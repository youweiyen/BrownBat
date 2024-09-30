using BrownBat.Components;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumSharp;

namespace BrownBat.CalculateHelper
{
    public class HeatTransfer
    {
        public static double ResistanceFromFile(Pixel pixel, List<Element> panels)
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
        public static double ResistanceFromSql(Pixel pixel, List<Element> panels, string database, NpgsqlConnection connection)
        {
            List<string> overlapNames = pixel.OverlapPanels;
            List<Element> overlapPanels = panels.Where(p => overlapNames.Contains(p.Name)).ToList();

            List<double> ratioList = new List<double>();
            foreach (Element panel in overlapPanels)
            {
                string name = panel.Name;
                (int, int) domain = pixel.PixelDomain[name];
                int row = domain.Item2;
                int col = domain.Item1;

                //TODO:save panel data in cache
                var sql = $"SELECT Conductivity FROM {database} where Name = '{name}'";
                NpgsqlCommand command = new NpgsqlCommand(sql, connection);
                var dataReader = command.ExecuteReader();

                while (dataReader.Read())
                    Console.Write("{0}\t{1} \n", dataReader[0], dataReader[1]);

                double conductivity = 0;
                double thickness = panel.GeometryThickness;
                double ratio = thickness / conductivity;
                ratioList.Add(ratio);
            }
            double ratioSum = ratioList.Sum();
            return ratioSum;
        }
    }
}
