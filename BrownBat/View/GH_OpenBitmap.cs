using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using System.IO;
using BrownBat.Components;
using BrownBat.CalculateHelper;


namespace BrownBat.View
{
    public class GH_OpenBitmap : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_OpenBitmap class.
        /// </summary>
        public GH_OpenBitmap()
          : base("OpenBitmap", "Bmp",
              "Open an Bitmap file and return a Bitmap object",
              "BrownBat", "View")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Path to Bitmap file", GH_ParamAccess.list);
            pManager.AddTextParameter("Name", "N", "Name of File to import", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Load", "L", "Load Bitmap", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Bitmap", "B", "The loaded Bitmap object", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> paths = new List<string>();
            if (!DA.GetDataList(0, paths))
                return;
            List<string> names = new List<string>();
            if (!DA.GetDataList(1, names))
                return;
            bool flag = false;
            if (!DA.GetData(2, ref flag))
                return;

           if (!File.Exists(paths[0]))
                this.AddRuntimeMessage((GH_RuntimeMessageLevel)20, "The file provided path does not exist. Please verify this is a valid file path.");
            if (flag)
            {
                List<Bitmap> bitmaps = new List<Bitmap>();
                foreach (string path in paths)
                {
                    Bitmap bitmap = null;
                    bool getName = names.Contains(Path.GetFileNameWithoutExtension(path));
                    if (getName)
                    {
                        if (!path.GetBitmapFromFile(out bitmap))
                        {
                            if (!Path.HasExtension(path))
                            {
                                this.AddRuntimeMessage((GH_RuntimeMessageLevel)20, "This is not a valid file path. This file does not have a valid bitmap extension");
                                this.AddRuntimeMessage((GH_RuntimeMessageLevel)20, "This is not a valid file path. This file does not have a valid bitmap extension");
                            }
                            else
                                this.AddRuntimeMessage((GH_RuntimeMessageLevel)20, "This is not a valid bitmap file type. The extension " + Path.GetExtension(path) + " is not a supported bitmap format");
                        }
                        else
                        {
                            bitmaps.Add(bitmap);
                        }
                    }
                    //else { return; }
                }
                DA.SetDataList(0, bitmaps);
            }
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
            get { return new Guid("59E2E337-A57D-477A-A0FF-E0E75C1A91B1"); }
        }
    }
}