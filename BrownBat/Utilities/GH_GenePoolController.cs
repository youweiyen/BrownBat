using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
//using GalapagosComponents;

namespace BrownBat.Utilities
{
    public class GH_GenePoolController : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_GenePoolController class.
        /// </summary>
        public GH_GenePoolController()
          : base("GenePoolController", "GNC",
              "Controlling Interval, Decimal-Number and the number of sliders in a GenePool",
              "BrownBat", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddParameter((IGH_Param)new GH_GenePoolController.inactiveparam(), "<Gene Pool", "<", "input jack \n(Connect a GenePool here to control it) ", (GH_ParamAccess)1);
            pManager.AddIntegerParameter("Count", "C", "The Number of sliders", (GH_ParamAccess)0);
            pManager.AddIntervalParameter("Range", "R", "The domain of sliders", (GH_ParamAccess)0);
            pManager.AddIntegerParameter("Decimal", "D", "Decimal place for sliders' change", (GH_ParamAccess)0);
            pManager.AddNumberParameter("Randomness", "R", "The amount of randomization with Blot!", (GH_ParamAccess)0, 0.1);
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //if (DA.Iteration == 0 && this.Activate)
            //    this.randomize();
            //Interval interval = new Interval();
            //int num1 = 2;
            //int num2 = 10;
            //if (DA.GetData<int>(1, ref num2))
            //{
            //    foreach (GalapagosGeneListObject galapagosGeneListObject in this._k)
            //        galapagosGeneListObject.Count = num2;
            //}
            //if (DA.GetData<int>(3, ref num1))
            //{
            //    foreach (GalapagosGeneListObject galapagosGeneListObject in this._k)
            //        galapagosGeneListObject.Decimals = num1;
            //}
            //if (DA.GetData<Interval>(2, ref interval))
            //{
            //    foreach (GalapagosGeneListObject galapagosGeneListObject in this._k)
            //    {
            //        galapagosGeneListObject.Minimum = Convert.ToDecimal(((Interval)ref interval).Min);
            //        galapagosGeneListObject.Maximum = Convert.ToDecimal(((Interval)ref interval).Max);
            //    }
            //}
            //DA.GetData<double>(4, ref this.randRange);
            //foreach (GH_DocumentObject ghDocumentObject in this._k)
            //    ghDocumentObject.ExpireSolution(true);
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
            get { return new Guid("21F97503-BDFD-42CB-BAB2-3550354600DA"); }
        }
    }
}