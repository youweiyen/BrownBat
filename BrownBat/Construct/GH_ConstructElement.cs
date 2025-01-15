using System;
using System.Collections.Generic;
using System.Linq;
using BrownBat.Components;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace BrownBat.Construct
{
    public class GH_ConstructElement : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_ImportPanel class.
        /// </summary>
        public GH_ConstructElement()
          : base("ConstructElement", "Element",
              "Construct Bat Element Object",
              "BrownBat", "Construct")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ElementBlock", "EB", "Element Block", GH_ParamAccess.list);
            pManager.AddGenericParameter("ElementData", "ED", "Import the data to Object", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Element", "E", "Element with all the panel data", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Element> inputGeometryPanel = new List<Element>();
            List<Element> inputDataPanel = new List<Element>();

            DA.GetDataList(0, inputGeometryPanel);
            DA.GetDataList(1, inputDataPanel);

            var combinedPanels = inputGeometryPanel.Join(inputDataPanel, geometryName => geometryName.Name, dataName => dataName.Name,
                                                        (geometry, data) => new Element
                                                        (geometry.Name,
                                                        geometry.InverseMatrix,
                                                        geometry.Model,
                                                        geometry.GeometryBaseCurve,
                                                        data.PixelShape,
                                                        data.PixelConductivity)).ToList();
            
            for (int p = 0; p < combinedPanels.Count(); p++)
            { 
                if (combinedPanels[p].Origin.IsValid == false)
                {
                    Transform matrix = combinedPanels[p].InverseMatrix;
                    Brep dupPanel = combinedPanels[p].Model.DuplicateBrep();
                    if(matrix.IsZeroTransformation != true)
                    {
                        dupPanel.Transform(matrix);
                    }
                    BrepVertexList profileVertexList = dupPanel.Vertices;

                    List<Point3d> profileVertices = new List<Point3d>();
                    for (int i = 0; i < profileVertexList.Count; i++)
                    {
                        Point3d vertex = profileVertexList[i].Location;
                        profileVertices.Add(vertex);
                    }
                    double xStartProfile = profileVertices.OrderBy(v => v.X).Select(v => v.X).First();
                    double yStartProfile = profileVertices.OrderByDescending(v => v.Y).Select(v => v.Y).First();
                    double ySmallest = profileVertices.OrderBy(v => v.Y).Select(v => v.Y).First();
                    double xLargest = profileVertices.OrderByDescending(v => v.X).Select(v => v.X).First();

                    Vector3d xDirection = new Vector3d(xLargest - xStartProfile, 0, 0);
                    Vector3d yDirection = new Vector3d(0, yStartProfile - ySmallest, 0);

                    Point3d profileStart = new Point3d(xStartProfile, yStartProfile, 0);
                    Plane originPlane = new Plane(profileStart, xDirection, yDirection);
                    Element.SetOrigin(combinedPanels[p], originPlane);
                    
                }
            }

            //calculate geometry and pixel size properties
            foreach (Element panel in combinedPanels)
            {
                Element.ModelThickness(panel);
                Element.ModelShape(panel);
                Element.GetPixelSize(panel);
            }
            
            DA.SetDataList(0, combinedPanels);
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
                return Properties.Resources.baticon;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("EF6963EA-F90F-4C91-9041-B7636DAF4030"); }
        }
    }
}