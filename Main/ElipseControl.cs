using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ElipseControl
{
    public class ElipseControl : Component
    {
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        private Control control;
        private int cornerRadius = 25;


        // Propiedad real que aplica el efecto
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Control TargetControl
        {
            get { return control; }
            set
            {
                control = value;
                if (control != null)
                {
                    control.SizeChanged += (s, e) =>
                    {
                        control.Region = Region.FromHrgn(
                            CreateRoundRectRgn(0, 0, control.Width, control.Height, cornerRadius, cornerRadius));
                    };
                }
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = value;
                if (control != null)
                {
                    control.Region = Region.FromHrgn(
                        CreateRoundRectRgn(0, 0, control.Width, control.Height, cornerRadius, cornerRadius));
                }
            }
        }

       
    }
}
