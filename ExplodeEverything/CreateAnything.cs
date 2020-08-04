using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using ExplodeEverything.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace ExplodeEverything
{
    public class CreateAnything : GH_Component, IGH_VariableParameterComponent
    {
        bool objectPropertiesMatched;
        ConstructorInfo[] objectConstructors;
        ParameterInfo[] constructorParams;
        int chosenConstructorIndex;
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public CreateAnything()
          : base("CreateAnything", "CA",
              "n/a",
              "Math", "Explode")
        {
            objectPropertiesMatched = false;
            chosenConstructorIndex = -1;
            objectConstructors = typeof(object).GetConstructors();
            constructorParams = objectConstructors[0].GetParameters();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            if (objectConstructors != null && objectConstructors.Length > 0)
            {
                for (int ind = 0; ind < objectConstructors.Length; ++ind)
                {
                    ParameterInfo[] pInfos = objectConstructors[ind].GetParameters();
                    if (pInfos.Length > 0)
                    {
                        string paramDescription = string.Join(", ", pInfos.Select(x => $"({x.ParameterType.Name}){x.Name}"));
                        ToolStripMenuItem mItem = Menu_AppendItem(menu, paramDescription, ChooseConstructor); // TODO: add event receiver
                        mItem.Tag = ind;
                    }
                }
            }
        }

        void ChooseConstructor(object o, EventArgs e)
        {
            int chosenInd = (int) ((o as ToolStripMenuItem).Tag);
            constructorParams = objectConstructors[chosenInd].GetParameters();
            if (constructorParams.Length < 1)
                return;

            ClearParamExceptFirst();
            int ind = 0;
            while (constructorParams.Length + 1 > Params.Input.Count)
            {
                Params.RegisterInputParam(new Param_GenericObject { NickName = constructorParams[ind++].Name, Optional = true });
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }

        // used for unregister the input parameter except the first one.
        private void ClearParamExceptFirst()
        {
            int numberOfParameters = Params.Input.Count;
            for (int index = numberOfParameters - 1; index > 0; index--)
            {
                Params.Input[index].RemoveAllSources();
                Params.UnregisterInputParameter(Params.Input[index]);
            }
        }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Type", "T", "Type of the object to be created.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Object(s) created.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Type objectType = typeof(object);
            if (!DA.GetData(0, ref objectType))
                return;
            if (!objectPropertiesMatched)
            {
                objectPropertiesMatched = true;
                MatchInputs(objectType);
                ExpireSolution(true);
            }
            // TODO: 
            // do not change layout when drag in the same type as before,
            // 
            // switch to no inputs if the type is different,
            //
            // if isValue type - treated differently if there is no constructors 
            // 
            // deal with objects that only have constructors with given parameters    e.g. Curve objects.
            object objectCreated = Activator.CreateInstance(objectType);
            DA.SetData(0, objectCreated);
            objectPropertiesMatched = false;
        }

        void MatchInputs(Type t)
        {
            objectConstructors = t.GetConstructors();
            if (objectConstructors.Length > 0)
                chosenConstructorIndex = 0;
        }
        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index) => new Grasshopper.Kernel.Parameters.Param_GenericObject();
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;
        public void VariableParameterMaintenance() { }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.Script;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6e9f57b9-a12a-47a8-97a7-29287087c7f2"); }
        }
    }
}