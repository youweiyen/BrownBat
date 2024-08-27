using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using GH_IO.Serialization;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.UI;

namespace BrownBat.Data
{
    public class GH_QueryElementName : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_QueryPanelName class.
        /// </summary>
        public GH_QueryElementName()
          : base("QueryElementName", "QN",
              "Query Element Name",
              "BrownBat", "Data")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("FilePath", "T", "File Path", GH_ParamAccess.list);
            pManager.AddTextParameter("FileType", "F", "File Type", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("ElementNames", "N", "Element Names", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> inputPaths = new List<string>();
            string inputValue = default;

            DA.GetDataList(0, inputPaths);
            DA.GetData(1, ref inputValue);


            GH_Document doc = new GH_Document();
            AddedToDocument(doc);

            List<string> pathNames = inputPaths.Select(path => Path.GetFileNameWithoutExtension(path)).ToList();
            
            string conductString = ((int)DataType.Conductivity).ToString();
            if (string.Equals(inputValue, conductString))
            {
                for (int n = 0; n < pathNames.Count; n++)
                {
                    pathNames[n] = pathNames[n].Remove(pathNames[n].Length - 2);
                }
            }

            DA.SetDataList(0, pathNames);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.baticon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("EECA1335-8DC6-4F3F-B6A9-DDC6986DD8F8"); }
        }
        public override void AddedToDocument(GH_Document document)
        {

            base.AddedToDocument(document);


            //Add Value List
            int[] paramID = new int[] { 1 };//second param

            for (int i = 0; i < paramID.Length; i++)
            {
                Grasshopper.Kernel.Parameters.Param_String inputNum = Params.Input[paramID[i]] as Grasshopper.Kernel.Parameters.Param_String;
                if (inputNum == null || inputNum.SourceCount > 0 || inputNum.PersistentDataCount > 0) return;
                Attributes.PerformLayout();
                int x = (int)inputNum.Attributes.Pivot.X - 250;
                int y = (int)inputNum.Attributes.Pivot.Y - 10;
                Grasshopper.Kernel.Special.GH_ValueList valList = new Grasshopper.Kernel.Special.GH_ValueList();
                valList.CreateAttributes();
                valList.Attributes.Pivot = new PointF(x, y);
                valList.Attributes.ExpireLayout();
                valList.ListItems.Clear();

                List<Grasshopper.Kernel.Special.GH_ValueListItem> materials = new List<Grasshopper.Kernel.Special.GH_ValueListItem>()
                {
                  new Grasshopper.Kernel.Special.GH_ValueListItem(nameof(DataType.Conductivity), ((int)DataType.Conductivity).ToString()),
                  new Grasshopper.Kernel.Special.GH_ValueListItem(nameof(DataType.Temperature), ((int)DataType.Temperature).ToString()),
                };

               valList.ListItems.AddRange(materials);
                document.AddObject(valList, false);
                inputNum.AddSource(valList);
            }
        }

        public enum DataType
        {
            Conductivity,
            Temperature
        }
    }
}