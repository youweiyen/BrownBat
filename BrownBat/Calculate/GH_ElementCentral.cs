using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BrownBat.CalculateHelper;
using BrownBat.Components;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.UI;

namespace BrownBat.Calculate
{
    public class GH_ElementCentral : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_ElementAverage class.
        /// </summary>
        public GH_ElementCentral()
          : base("ElementCentral", "C",
              "Central Data for each Element",
              "BrownBat", "Calculate")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ElementData", "E", "Bat Element", GH_ParamAccess.list);
            pManager.AddTextParameter("SelectName", "N", "Bat Element Name", GH_ParamAccess.list);
            pManager.AddIntegerParameter("CentralType", "C", "Central Tendency", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Result", "R", "Result Conductivity", GH_ParamAccess.list);
            pManager.AddTextParameter("Name", "N", "Element Name", GH_ParamAccess.list);
            pManager.AddGenericParameter("ElementData", "ED", "Element data with Central calculated", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            int inCentral = default;
            List<Element> inElement = new List<Element>();
            List<string> inName = new List<string>();

            DA.GetDataList(0, inElement);
            DA.GetDataList(1, inName);
            DA.GetData(2, ref inCentral);

            GH_Document doc = new GH_Document();
            AddedToDocument(doc);

            List<double> centrals = new List<double>();

            IEnumerable<Element> selectElement = inElement.Where(e => inName.Contains(e.Name));

            foreach (Element element in selectElement)
            {
                IEnumerable<double> conductivity = element.PixelConductivity.SelectMany(c => c);
                double central = default;
                switch (inCentral)
                {
                    case (int)CentralTendency.Mean:
                        central = ElementCentral.Mean(conductivity);
                            break;

                    case (int)CentralTendency.Median:
                        central = ElementCentral.Median(conductivity);
                        break;
                    case (int)CentralTendency.Mode:
                        central = ElementCentral.Mode(conductivity);
                        break;
                }
                centrals.Add(central);
                Element.SetCentral(element, central);
            }
            IEnumerable<string> selectName = selectElement.Select(i => i.Name);

            DA.SetDataList(0, centrals);
            DA.SetDataList(1, selectName);
            DA.SetDataList(2, selectElement);


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
            get { return new Guid("651FCA91-ADD9-4EF8-87AD-1B98FB49971E"); }
        }
        public override void AddedToDocument(GH_Document document)
        {

            base.AddedToDocument(document);


            //Add Value List
            int[] paramID = new int[] { 2 };//third param

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
                  new Grasshopper.Kernel.Special.GH_ValueListItem(nameof(CalculateHelper.CentralTendency.Mean), ((int)CalculateHelper.CentralTendency.Mean).ToString()),
                  new Grasshopper.Kernel.Special.GH_ValueListItem(nameof(CalculateHelper.CentralTendency.Median), ((int)CalculateHelper.CentralTendency.Median).ToString()),
                  new Grasshopper.Kernel.Special.GH_ValueListItem(nameof(CalculateHelper.CentralTendency.Mode), ((int)CalculateHelper.CentralTendency.Mode).ToString()),

                };

                valList.ListItems.AddRange(materials);
                document.AddObject(valList, false);
                inputNum.AddSource(valList);
            }
        }
    }
}