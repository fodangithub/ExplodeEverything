using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace ExplodeAnything
{
    public class ArrayConstructor : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ArrayConstructor class.
        /// </summary>
        public ArrayConstructor()
          : base("ArrayConstructor", "AC",
              "Put a series of data into an Array",
              "Math", "Explode")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("items", "i", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Array", "A", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<object> o = new List<object>();
            if (DA.GetDataList(0, o))
            {
                Type t = o[0].GetType();
                if (o[0].GetType().Name.StartsWith("GH_"))
                {
                    Type inner = t.GetProperty("Value").GetValue(o[0]).GetType();
                    Array a = Array.CreateInstance(inner, o.Count);
                    for (int ind = 0; ind < o.Count; ++ind)
                    {
                        a.SetValue(t.GetProperty("Value").GetValue(o[ind]), ind);
                    }
                    DA.SetData(0, a);
                }
                else
                {
                    Array a = Array.CreateInstance(t, o.Count);
                    for (int ind = 0; ind < o.Count; ++ind)
                    {
                        a.SetValue(o[ind], ind);
                    }
                    DA.SetData(0, a);
                }
            }
            else
            {
                DA.SetData(0, null);
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
                return ExplodeAnything.Properties.Resources.arrayu;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("957e77b0-67a5-48be-b374-63f0013604bf"); }
        }
    }
}