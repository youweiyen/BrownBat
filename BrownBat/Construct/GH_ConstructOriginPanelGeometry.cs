using BrownBat.Components;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrownBat.Construct
{
    public class GH_ConstructOriginPanelGeometry : GH_Component
    {
        public GH_ConstructOriginPanelGeometry()
          : base("ConstructOriginPanelGeometry", "Panel",
            "Construct name and geometry as Base Panel",
            "BrownBat", "Construct")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("PanelBrep", "P", "Input Panel Brep", GH_ParamAccess.list);
            pManager.AddTextParameter("PanelName", "N", "Input Panel Name", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Bake", "B", "Bake Brep, Name, Origin plane", GH_ParamAccess.item, false);
            pManager[2].Optional = true;

        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Brep> inputPanels = new List<Brep>();
            List<string> inputNames = new List<string>();
            bool inputBake = false;

            DA.GetDataList(0, inputPanels);
            DA.GetDataList(1, inputNames);
            DA.GetData(2, ref inputBake);

            List<Element> outputPanels = new List<Element>();
            for (int p = 0;  p < inputPanels.Count; p++)
            {
                BrepVertexList profileVertexList = inputPanels[p].Vertices;

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
                Element panel = new Element(inputNames[p], originPlane, inputPanels[p]);
                outputPanels.Add(panel);
            }
            if (inputBake)
            {
                #region normalBake
                ////Paraent Layer
                //Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
                //Panel panel = outputPanels[0];
                //string panelName = panel.Name;
                //System.Drawing.Color color = new System.Drawing.Color();
                //color = System.Drawing.Color.FromArgb(255, 0, 0, 255);

                //int index = doc.Layers.FindByFullPath(panelName, -1);
                //if (index < 0)
                //{
                //    doc.Layers.Add(panelName, color);
                //}
                //index = doc.Layers.FindByFullPath(panelName, -1);
                //Rhino.DocObjects.Layer parentLayer = doc.Layers[index];

                ////set attributes
                //Rhino.DocObjects.ObjectAttributes attBrep = new Rhino.DocObjects.ObjectAttributes();
                //Rhino.DocObjects.ObjectAttributes attOrigin = new Rhino.DocObjects.ObjectAttributes();
                //Rhino.DocObjects.ObjectAttributes attX = new Rhino.DocObjects.ObjectAttributes();
                //Rhino.DocObjects.ObjectAttributes attY = new Rhino.DocObjects.ObjectAttributes();

                //attBrep.Name = panelName;
                //attBrep.LayerIndex = index;

                //attOrigin.Name = "0";
                //attOrigin.LayerIndex = index;
                //attX.Name = "1";
                //attX.LayerIndex = index;
                //attY.Name = "2";
                //attY.LayerIndex = index;

                //Plane originPlane = outputPanels[0].Origin;
                //Point3d originPoint = originPlane.Origin;

                //Vector3d vectorX = originPlane.XAxis;
                //Vector3d vectorY = originPlane.YAxis;
                //Point3d originX = originPoint + vectorX;
                //Point3d originY = originPoint + vectorY;

                ////bake with attributes
                //doc.Objects.AddBrep(panel.Model, attBrep);

                //doc.Objects.AddPoint(originPoint, attOrigin);
                //doc.Objects.AddPoint(originX, attX);
                //doc.Objects.AddPoint(originY, attY);
                #endregion

                //bakeBlock
                Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
                System.Drawing.Color color = new System.Drawing.Color();
                color = System.Drawing.Color.FromArgb(255, 0, 0, 255);

                for (int i = 0; i < outputPanels.Count; i++)
                {
                    Element panel = outputPanels[i];

                    Point3d origin = panel.Origin.Origin;
                    GeometryBase blockPanel = GH_Convert.ToGeometryBase(panel.Model);
                    GeometryBase blockOrigin = GH_Convert.ToGeometryBase(origin);

                    string blockName = panel.Name;
                    blockName = blockName.Trim();
                    if (string.IsNullOrEmpty(blockName)) { throw new Exception("panel name empty"); }

                    InstanceDefinition existing_idef = doc.InstanceDefinitions.Find(blockName);
                    if (existing_idef != null)
                    {
                        doc.InstanceDefinitions.Delete(existing_idef.Index, true, true);
                    }

                    string panelName = panel.Name;
                    int index = doc.Layers.FindByFullPath(panelName, -1);
                    if (index < 0) { doc.Layers.Add(panelName, color); }
                    index = doc.Layers.FindByFullPath(panelName, -1);
                    Layer parentLayer = doc.Layers[index];

                    ObjectAttributes attBrep = new ObjectAttributes();
                    attBrep.Name = panelName;
                    attBrep.LayerIndex = index;

                    var geometry = new List<GeometryBase>();
                    var attributes = new List<ObjectAttributes>();

                    if (outputPanels != null)
                    {
                        geometry.Add(blockPanel);
                        attributes.Add(attBrep);
                    }
                    int idef_index = doc.InstanceDefinitions.Add(blockName, string.Empty, origin, geometry, attributes);

                    // Creates a variable (Trans) that is the transformation between the World Origin 0,0,0 and a referenced plane
                    Plane BasePlane = new Plane(origin, Vector3d.ZAxis);
                    Transform Trans = Transform.PlaneToPlane(Plane.WorldXY, BasePlane);

                    // Creates the Block Instance in Rhino and outputs its Reference ID
                    var Ref_ID = doc.Objects.AddInstanceObject(idef_index, Trans, attBrep);

                    if (idef_index < 0)
                    {
                        throw new Exception($"Unable to create block definition{blockName}");
                    }
                }
                GH_RuntimeMessageLevel level = GH_RuntimeMessageLevel.Remark;
                this.AddRuntimeMessage(level, "Geometry Baked");
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.baticon;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("fce74e46-ad63-4d91-987a-99861de03e11");
    }
}