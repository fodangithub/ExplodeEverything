using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ExplodeAnything.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace ExplodeAnything
{
    public class CreateAnything : GH_Component, IGH_VariableParameterComponent
    {
        Type previewslyInputType = typeof(object);
        ConstructorInfo[] objectConstructors;
        ParameterInfo[] constructorParams;
        int chosenConstructorIndex;
        Action actionToTakeAfterSolution;


        ////// Possible functions that could create an instance of desired type:
        ///    1. Constructors with params
        ///    2. Constructors without params - create an empty class with all properties set to default
        ///    3. Static functions of that Type 


        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public CreateAnything()
          : base("CreateAnything", "CA",
              "Let's build something",
              "Math", "Explode")
        {
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
                    else if (previewslyInputType != typeof(object))
                    {
                        ToolStripMenuItem mItem = Menu_AppendItem(menu, $"From {previewslyInputType.Name} properties", ChooseConstructor); // TODO: add event receiver
                        mItem.Tag = ind;
                    }
                }
            }
        }
        void ChooseConstructor(object o, EventArgs e)
        {
            int chosenInd = (int) ((o as ToolStripMenuItem).Tag);
            chosenConstructorIndex = chosenInd;
            constructorParams = objectConstructors[chosenInd].GetParameters();
            if (constructorParams.Length < 1)
            {
                ClearParamExceptFirst();
                PropertyInfo[] props = previewslyInputType.GetProperties(BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.Public);
                FieldInfo[] fields = previewslyInputType.GetFields(BindingFlags.SetField | BindingFlags.Instance | BindingFlags.Public);
                int ind = 0;
                while (props.Length + fields.Length + 1 > Params.Input.Count)
                {
                    if (ind < fields.Length)
                    {
                        Params.RegisterInputParam(new Param_GenericObject
                        {
                            NickName = fields[ind].Name,
                            Optional = true,
                            Description = $"Type: {fields[ind].FieldType}"
                        });
                    }
                    else
                    {
                        Params.RegisterInputParam(new Param_GenericObject
                        {
                            NickName = props[ind - fields.Length].Name,
                            Optional = true,
                            Description = $"Type: {props[ind - fields.Length].PropertyType}"
                        });
                    }
                    ind++;
                }
            }
            else
            {
                ClearParamExceptFirst();
                int ind = 0;
                while (constructorParams.Length + 1 > Params.Input.Count)
                {
                    Params.RegisterInputParam(new Param_GenericObject { NickName = constructorParams[ind].Name, Optional = true, Description = $"Type: {constructorParams[ind].ParameterType}" });
                    ind++;
                }
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
            
            if (objectType != previewslyInputType)
            {
                //OnPingDocument().RequestAbortSolution();
                if (RunCount < 2)
                {
                    actionToTakeAfterSolution = () => MatchInputs(objectType);
                    return;
                }
                else
                {
                    throw new ArgumentException("'Type' cannot have different types inputs");
                }
            }
            else
            {
                actionToTakeAfterSolution = null;
            }

            if (Params.Input.Count > 1 && constructorParams.Length > 0)
            {
                List<object> inputs = new List<object>();
                for (int ind = 1; ind < Params.Input.Count; ind++)
                {
                    object inputParam = default;
                    if (!DA.GetData(ind, ref inputParam))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Input field {Params.Input[ind].NickName} failed to collect data.");
                        return;
                    }
                    else
                    {
                        if (inputParam.GetType().Name.StartsWith("GH_"))
                        {
                            var in_ = inputParam.GetType().GetProperty("Value").GetValue(inputParam);
                            inputs.Add(in_);
                        }
                        else
                        {
                            inputs.Add(inputParam);
                        }
                        
                    }
                }
                // TODO: 
                // (checked) do not change layout when drag in the same type as before,
                // 
                // (checked) switch to no inputs if the type is different,
                //
                // if isValue type - treated differently if there is no constructors 
                // 
                // deal with objects that only have constructors with given parameters    e.g. Curve objects.
                //
                // deal with the static function of a class which may provide a instance of that class as output

                object objectCreated = default;
                switch (Params.Input.Count)
                {
                    case 2:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0]);
                        break;
                    case 3:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1]);
                        break;
                    case 4:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1], inputs[2]);
                        break;
                    case 5:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1], inputs[2], inputs[3]);
                        break;
                    case 6:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1], inputs[2], inputs[3], inputs[4]);
                        break;
                    case 7:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1], inputs[2], inputs[3], inputs[4], inputs[5]);
                        break;
                    case 8:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1], inputs[2], inputs[3], inputs[4], inputs[5], inputs[6]);
                        break;
                    case 9:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1], inputs[2], inputs[3], inputs[4], inputs[5], inputs[6], inputs[7]);
                        break;
                    case 10:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1], inputs[2], inputs[3], inputs[4], inputs[5], inputs[6], inputs[7], inputs[8]);
                        break;
                    case 11:
                        objectCreated = Activator.CreateInstance(objectType, inputs[0], inputs[1], inputs[2], inputs[3], inputs[4], inputs[5], inputs[6], inputs[7], inputs[8], inputs[9]);
                        break;
                    default:
                        throw new Exception($"constructors that takes more than 10 input parameters are not currently supported");
                }
                

                if (objectCreated != null)
                    DA.SetData(0, objectCreated);
                else
                    throw new Exception($" failed to create an instance of {previewslyInputType}");
            }
            else if (Params.Input.Count > 1)
            {
                try
                {
                    object objectCreated = Activator.CreateInstance(objectType);
                    DA.SetData(0, objectCreated);
                }
                catch
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{objectType} does not have constructor with 0 inputs");
                    DA.SetData(0, null);
                }
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Use menu to choose one type of constructor");
            }
        }

        void MatchInputs(Type t)
        {
            objectConstructors = t.GetConstructors();
            if (objectConstructors.Length > 0)
                chosenConstructorIndex = 0;
            previewslyInputType = t;
            ClearParamExceptFirst();
            OnPingDocument().ScheduleSolution(1, (o) => this.ExpireSolution(false));
        }
        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;
        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;
        public IGH_Param CreateParameter(GH_ParameterSide side, int index) => new Grasshopper.Kernel.Parameters.Param_GenericObject();
        public bool DestroyParameter(GH_ParameterSide side, int index) => true;
        public void VariableParameterMaintenance() { }

        protected override void AfterSolveInstance()
        {
            base.AfterSolveInstance();
            if (actionToTakeAfterSolution != null)
            {
                actionToTakeAfterSolution.Invoke();
            }
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