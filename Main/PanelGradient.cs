using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing;


namespace Gradient
{
    public class PanelGradient : Panel
    {
        //fields 

        private int borderRadius = 30;
        private float gradientAngle = 90F;
        private Color gradientTopColor = Color.DodgerBlue;
        private Color gradientBottomColor = Color.CadetBlue;


        //Constructors

        public PanelGradient()
        {
            this.BackColor = Color.White;
            this.ForeColor = Color.Black;
            this.Size = new Size(350, 200);
        }



        //Properties

        //Properties

        [DefaultValue(30)]
        public int BorderRadius
        {
            get => borderRadius;
            set { borderRadius = value; this.Invalidate(); }
        }
        private bool ShouldSerializeBorderRadius() => borderRadius != 30;
        private void ResetBorderRadius() => BorderRadius = 30;

        public float GradientAngle
        {
            get => gradientAngle;
            set { gradientAngle = value; this.Invalidate(); }
        }
        private bool ShouldSerializeGradientAngle() => gradientAngle != 90F;
        private void ResetGradientAngle() => GradientAngle = 90F;

        public Color GradientTopColor
        {
            get => gradientTopColor;
            set { gradientTopColor = value; this.Invalidate(); }
        }
        private bool ShouldSerializeGradientTopColor() => gradientTopColor != Color.DodgerBlue;
        private void ResetGradientTopColor() => GradientTopColor = Color.DodgerBlue;

        public Color GradientBottomColor
        {
            get => gradientBottomColor;
            set { gradientBottomColor = value; this.Invalidate(); }
        }
        private bool ShouldSerializeGradientBottomColor() => gradientBottomColor != Color.CadetBlue;
        private void ResetGradientBottomColor() => GradientBottomColor = Color.CadetBlue;
        //Methods

        private GraphicsPath GetPanel(RectangleF rectangle, float radius)
        {
            GraphicsPath graphicsPath = new GraphicsPath();
            graphicsPath.StartFigure();
            graphicsPath.AddArc(rectangle.Width - radius, rectangle.Height - radius, radius, radius, 0, 90);
            graphicsPath.AddArc(rectangle.X, rectangle.Height - radius, radius, radius, 90, 90);
            graphicsPath.AddArc(rectangle.X, rectangle.Y, radius, radius, 180, 90);
            graphicsPath.AddArc(rectangle.Width - radius, rectangle.Y, radius, radius, 270, 90);
            graphicsPath.CloseFigure();
            return graphicsPath;



        }

        //Override Methods


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            //gradient panel
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            LinearGradientBrush linearGradientBrush = new LinearGradientBrush(this.ClientRectangle, this.gradientTopColor, this.gradientBottomColor, this.gradientAngle);
            Graphics graphics = e.Graphics;
            graphics.FillRectangle(linearGradientBrush, ClientRectangle);

            //border radius
            RectangleF rectangleF = new RectangleF(0, 0, this.Width, this.Height);
            if (borderRadius > 2)
            {
                using (GraphicsPath graphicsPath = GetPanel(rectangleF, borderRadius))
                using (Pen pen = new Pen(this.Parent.BackColor, 2))
                {
                    this.Region = new Region(graphicsPath);
                    graphics.DrawPath(pen, graphicsPath);
                }
            }
            else
            {
                this.Region = new Region(rectangleF);
            }

        }





    }
}
