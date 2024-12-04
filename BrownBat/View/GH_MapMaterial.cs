using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using BrownBat.Components;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Render;

namespace BrownBat.View
{
    public class GH_MapMaterial : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MaterialMap class.
        /// </summary>
        public GH_MapMaterial()
          : base("MapMaterial", "M",
              "Map Bitmap to Material",
              "BrownBat", "View")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Bitmap", "B", "Element bitmap", GH_ParamAccess.item);
            pManager.AddPointParameter("Cooordinates", "C", "Pixel coordinates in X (width) and Y (height) direction", GH_ParamAccess.list);
            pManager.AddInterval2DParameter("Domain", "D",
                "Domain to use for sampling the image, defaults to [1,pixel width] x [1,pixel height] of the image.\nSet this to [0,1] x [0,1] if you want to use coordinates between 0 and 1.", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddColourParameter("Color", "C", "Pixel colour at the (X,Y) location.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Bitmap inBitmap = null;
            if (!DA.GetData(0, ref inBitmap))
            {
                AddRuntimeMessage((GH_RuntimeMessageLevel)20, "No valid bitmap given.");
            }
            //TODO: Add top layer pixel color fuction for overlay view

            else
            {
                int width = inBitmap.Width;
                int height = inBitmap.Height;
                List<GH_Point> ghPointList = new List<GH_Point>();
                if (!DA.GetDataList(1, ghPointList))
                {
                    AddRuntimeMessage((GH_RuntimeMessageLevel)20, "No points given.");
                }
                else
                {
                    GH_Interval2D ghInterval2D = new GH_Interval2D();
                    if (!DA.GetData(2, ref ghInterval2D))
                    {
                        ghInterval2D = new GH_Interval2D(new UVInterval(new Interval(1.0, inBitmap.Width), new Interval(1.0, (double)inBitmap.Height)));
                        AddRuntimeMessage((GH_RuntimeMessageLevel)(int)byte.MaxValue, "No valid domain given, using " + ghInterval2D.ToString());
                    }
                    
                    UVInterval uvInterval = ghInterval2D.Value;
                    double u0 = uvInterval.U0;
                    double intervalUlength = uvInterval.U1 - uvInterval.U0;
                    double v0 = uvInterval.V0;
                    double intervalVlength = uvInterval.V1 - uvInterval.V0;
                    List<GH_Colour> ghColourList = new List<GH_Colour>(ghPointList.Count);
                    int index = 0;
                    for (int count = ghPointList.Count; index < count; ++index)
                    {
                        Point3d point3d = ghPointList[index].Value;
                        double inU = (point3d[0] - u0) / intervalUlength;
                        double inV = (point3d[1] - v0) / intervalVlength;
                        double uOverZero = inU < 0.0 ? 0.0 : inU;
                        double uInterval = uOverZero > 1.0 ? 1.0 : uOverZero;
                        double vOverZero = inV < 0.0 ? 0.0 : inV;
                        double vInterval = vOverZero > 1.0 ? 1.0 : vOverZero;
                        int uDomain = (int)Math.Round(uInterval * width);
                        int vDomain = (int)Math.Round(vInterval * height);
                        int uDomainOverZero = uDomain < 0 ? 0 : uDomain;
                        int x = uDomainOverZero >= width ? width - 1 : uDomainOverZero;
                        int vDomainOverZero = vDomain < 0 ? 0 : vDomain;
                        int lastY = vDomainOverZero >= height ? height - 1 : vDomainOverZero;
                        int y = height - 1 - lastY;
                        Color pixel = inBitmap.GetPixel(x, y);
                        ghColourList.Add(new GH_Colour(pixel));
                    }
                    DA.SetDataList(0, ghColourList);
                }
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
            get { return new Guid("7B836BD2-3C43-4602-B079-49156121F37C"); }
        }
    }
}