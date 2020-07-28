using System;
using System.Drawing;
using ExplodeEverything.Properties;
using Grasshopper.Kernel;

namespace ExplodeEverything
{
    public class ExplodeEverythingInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "ExplodeEverything";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Resources.iconfinder_Funny_132217;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("fa409005-b969-4b96-b0d6-ababa85e2f00");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
