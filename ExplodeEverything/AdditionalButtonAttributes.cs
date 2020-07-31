using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace ExplodeAnything
{
    class AdditionalButtonAttributes : GH_ComponentAttributes
    {
        private readonly GH_Component attributeOwner;
        private Rectangle textBoxRec;
        public string TextLine { get; set; }
        public delegate void ResponderEvent(object o, EventArgs e);
        public ResponderEvent ButtonResponder { get; set; }
        public AdditionalButtonAttributes(GH_Component owner) : base(owner) { attributeOwner = owner; }

        protected override void Layout()
        {
            base.Layout();

            Rectangle originRec = GH_Convert.ToRectangle(Bounds);
            originRec.Height += 20;
            Rectangle textBox = originRec;
            textBox.Y = textBox.Bottom - 20;
            textBox.Height = 20;
            textBox.Inflate(-2, -2);
            Bounds = originRec;
            textBoxRec = textBox;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                GH_Capsule box = GH_Capsule.CreateTextCapsule(textBoxRec, textBoxRec, GH_Palette.Grey, TextLine);
                box.Render(graphics, Selected, attributeOwner.Locked, false);
            }
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                System.Drawing.RectangleF rec = textBoxRec;
                if (rec.Contains(e.CanvasLocation))
                {
                    if (ButtonResponder != null)
                    {
                        ButtonResponder.Invoke(sender, e);
                        return GH_ObjectResponse.Handled;
                    }
                }
            }
            return base.RespondToMouseDown(sender, e);
        }
    }
}
