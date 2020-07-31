using ExplodeEverything.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace ExplodeAnything
{
    public class ExplodeAnythingComponent : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>

        string typeName;
        FieldInfo[] fieldsArr;
        PropertyInfo[] propertiesArr;

        public ExplodeAnythingComponent()
          : base("ExplodeAnything", "EA",
              "Description",
              "Math", "Explode")
        {
        }

        public override void CreateAttributes()
        {
            m_attributes = new AdditionalButtonAttributes(this) { ButtonResponder = MatchResponder, TextLine = "BOOM" };
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Match object Fields", MatchResponder);
        }

        void MatchResponder(object o, EventArgs e)
        {
            if (fieldsArr == null || propertiesArr == null)
                return;

            if (this.Params.Output.Count == fieldsArr.Length + propertiesArr.Length)
                return;

            while (this.Params.Output.Count < fieldsArr.Length + propertiesArr.Length)
            {
                this.Params.RegisterOutputParam(new Param_GenericObject());
            }
            while (this.Params.Output.Count > fieldsArr.Length + propertiesArr.Length)
            {
                this.Params.UnregisterOutputParameter(this.Params.Output[this.Params.Output.Count - 1]);
            }

            this.Params.OnParametersChanged();
            this.VariableParameterMaintenance();
            this.ExpireSolution(true);
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Object to explode", GH_ParamAccess.item);
            pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("", "", "", GH_ParamAccess.tree);
            this.VariableParameterMaintenance();
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //DA.DisableGapLogic();
            object obj = new object();
            if (!DA.GetData(0, ref obj))
                return;

            if (DA.Iteration < 1)
            {
                typeName = obj.GetType().Name;
            }
            else
            {
                if (obj.GetType().Name != typeName)
                {
                    throw new Exception("Only same type of object can be explode");
                }
            }

            Type t = obj.GetType();
            if (t.Name.StartsWith("GH_"))
            {
                try
                {
                    GH_ObjectWrapper wrapper = (GH_ObjectWrapper)obj;
                    t = wrapper.Value.GetType();
                    obj = wrapper.Value;
                }
                catch
                {
                    obj = obj.GetType()
                        .GetProperty("Value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                        .GetValue(obj);
                    t = obj.GetType();
                }
            }
            fieldsArr = t.GetFields(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Public);
            propertiesArr = t.GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public);

            for (int ind = 0; ind < fieldsArr.Length + propertiesArr.Length; ++ind)
            {
                if (ind >= fieldsArr.Length + propertiesArr.Length)
                {
                    this.Params.Output[ind].NickName = "?";
                }
                else
                {
                    if (ind < fieldsArr.Length)
                    {
                        try
                        {
                            Params.Output[ind].NickName = fieldsArr[ind].Name;
                            DA.SetData(ind, fieldsArr[ind].GetValue(obj));
                        }
                        catch
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some fields were failed to explode, check answer carefully");
                        }
                    }
                    else
                    {
                        try
                        {
                            Params.Output[ind].NickName = propertiesArr[ind - fieldsArr.Length].Name;
                            if (propertiesArr[ind - fieldsArr.Length].Name == "Item" &&
                                (t.IsArray || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))))
                            {
                                IEnumerable objEnum = (IEnumerable)obj;
                                DA.SetDataList(ind, objEnum);
                            }
                            else if (t.Name.Contains("[]") && ind == 3)
                            {
                                DA.SetDataList(ind, (IEnumerable)obj);
                            }
                            else
                            {
                                PropertyInfo pInfo = propertiesArr[ind - fieldsArr.Length];
                                if (pInfo.GetIndexParameters().Length == 0)
                                {
                                    DA.SetData(ind, pInfo.GetValue(obj));
                                }
                                else if (obj is IEnumerable)
                                {
                                    DA.SetDataList(ind, (IEnumerable)obj);
                                }
                                else
                                {
                                    DA.SetData(ind, obj);
                                }
                            }
                        }
                        catch
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some fields were failed to explode, check answer carefully");
                        }
                    }
                }
            }
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Output;
        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            return side == GH_ParameterSide.Output;
        }

        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            return new Param_GenericObject();
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
            return true;
        }

        public void VariableParameterMaintenance()
        {
            if (fieldsArr != null && propertiesArr != null)
            {
                if (this.Params.Output.Count > 0)
                {
                    for (int ind = 0; ind < fieldsArr.Length + propertiesArr.Length; ++ind)
                    {
                        if (ind < fieldsArr.Length)
                        {
                            Params.Output[ind].NickName = fieldsArr[ind].Name;
                        }
                        else
                        {
                            Params.Output[ind].NickName = propertiesArr[ind - fieldsArr.Length].Name;
                        }
                    }
                }
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            return;
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            return;
        }
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return Resources.iconfinder_Bomb_132757;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("b62ce553-c5bf-4d00-a76e-84b8d5fbb17b"); }
        }
    }
}
