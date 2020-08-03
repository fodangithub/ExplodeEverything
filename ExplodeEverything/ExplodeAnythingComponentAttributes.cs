using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExplodeEverything;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace ExplodeAnything
{
    class ExplodeAnythingComponentAttributes : GH_ComponentAttributes
    {
        private readonly GH_Component attributeOwner;
        private Rectangle textRectangle;
        private Rectangle buttonRectangle;
        public string ButtonText { get; set; }
        public LongShortString TextLine { get; set; }
        public Font TextFont { get; set; }
        public delegate void ResponderEvent(object o, EventArgs e);
        public ResponderEvent ButtonResponder { get; set; }
        public ExplodeAnythingComponentAttributes(GH_Component owner) : base(owner) 
        { 
            attributeOwner = owner;
            TextLine = new LongShortString { Long = "", Short = "" };
            TextFont = GH_FontServer.Standard;
            ButtonText = "Button";
        }

        protected override void Layout()
        {
            base.Layout();

            Rectangle originRec = GH_Convert.ToRectangle(Bounds);
            originRec.Height += 36;
            Bounds = originRec;
            
            buttonRectangle = originRec;
            buttonRectangle.Y = buttonRectangle.Bottom - 20;
            buttonRectangle.Height = 20;
            buttonRectangle.Inflate(-2, -2);

            textRectangle = originRec;
            textRectangle.Y = buttonRectangle.Bottom - 36;
            textRectangle.Height = 16;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {

                GH_Capsule buttonCapsule = GH_Capsule.CreateTextCapsule(buttonRectangle, buttonRectangle, GH_Palette.Grey, ButtonText);
                buttonCapsule.Render(graphics, Selected, attributeOwner.Locked, false);
                buttonCapsule.Dispose();

                StringFormat format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                if (GH_FontServer.StringWidth(TextLine.Long, this.TextFont) < Bounds.Width)
                    graphics.DrawString(TextLine.Long, TextFont, Brushes.Black, textRectangle, format);
                else 
                    graphics.DrawString(TextLine.Short, TextFont, Brushes.Black, textRectangle, format);
            }
        }

        public override GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {
                System.Drawing.RectangleF rec = buttonRectangle;
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
