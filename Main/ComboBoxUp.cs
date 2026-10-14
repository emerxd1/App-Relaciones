using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    /// <summary>
    /// ComboBox plano y personalizable para WinForms.
    /// Hereda de ComboBox (conserva DataSource, AutoComplete, DropDown / DropDownList,
    /// teclado, etc.) y redibuja el fondo, el borde, el icono y los elementos de la lista.
    /// </summary>
    [ToolboxItem(true)]
    public class ComboBoxUp : ComboBox
    {
        private const int WM_PAINT = 0x000F;

        // Campos de apariencia
        private Color borderColor = Color.MediumSlateBlue;
        private int borderSize = 1;
        private Color iconColor = Color.MediumSlateBlue;
        private Color listBackColor = Color.FromArgb(230, 228, 245);
        private Color listTextColor = Color.DimGray;
        private Color listHighlightColor = Color.MediumSlateBlue;
        private int arrowAreaWidth = 28;

        public ComboBoxUp()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.WhiteSmoke;
            ForeColor = Color.DimGray;
            Font = new Font("Segoe UI", 10F);
            ItemHeight = 24;
        }

        // ---------------- Propiedades de apariencia ----------------

        [Category("ComboBoxUp - Appearance")]
        [DefaultValue(typeof(Color), "MediumSlateBlue")]
        public Color BorderColor
        {
            get { return borderColor; }
            set { borderColor = value; Invalidate(); }
        }

        [Category("ComboBoxUp - Appearance")]
        [DefaultValue(1)]
        public int BorderSize
        {
            get { return borderSize; }
            set { borderSize = Math.Max(0, value); Invalidate(); }
        }

        [Category("ComboBoxUp - Appearance")]
        [DefaultValue(typeof(Color), "MediumSlateBlue")]
        public Color IconColor
        {
            get { return iconColor; }
            set { iconColor = value; Invalidate(); }
        }

        [Category("ComboBoxUp - Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ListBackColor
        {
            get { return listBackColor; }
            set { listBackColor = value; }
        }

        [Category("ComboBoxUp - Appearance")]
        [DefaultValue(typeof(Color), "DimGray")]
        public Color ListTextColor
        {
            get { return listTextColor; }
            set { listTextColor = value; }
        }

        [Category("ComboBoxUp - Appearance")]
        [DefaultValue(typeof(Color), "MediumSlateBlue")]
        public Color ListHighlightColor
        {
            get { return listHighlightColor; }
            set { listHighlightColor = value; }
        }

        [Category("ComboBoxUp - Appearance")]
        [DefaultValue(28)]
        public int ArrowAreaWidth
        {
            get { return arrowAreaWidth; }
            set { arrowAreaWidth = Math.Max(16, value); Invalidate(); }
        }

        // ---------------- Dibujo del control ----------------

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // Después del pintado nativo, dibujamos encima nuestro estilo
            if (m.Msg == WM_PAINT)
                PaintOverlay();
        }

        private void PaintOverlay()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) return;

            using (Graphics g = Graphics.FromHwnd(Handle))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle client = ClientRectangle;
                Rectangle arrowRect = new Rectangle(client.Right - arrowAreaWidth, 0, arrowAreaWidth, client.Height);
                Rectangle textRect = new Rectangle(0, 0, client.Width - arrowAreaWidth, client.Height);

                using (SolidBrush back = new SolidBrush(BackColor))
                {
                    // Zona del icono siempre se repinta
                    g.FillRectangle(back, arrowRect);

                    // En DropDownList también repintamos el texto; en DropDown
                    // se respeta el cuadro de edición nativo para poder escribir.
                    if (DropDownStyle == ComboBoxStyle.DropDownList)
                    {
                        g.FillRectangle(back, textRect);
                        Rectangle t = new Rectangle(textRect.X + 8, textRect.Y, textRect.Width - 10, textRect.Height);
                        TextRenderer.DrawText(g, Text, Font, t, ForeColor,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    }
                }

                DrawArrow(g, arrowRect);
                DrawBorder(g, client);
            }
        }

        private void DrawArrow(Graphics g, Rectangle area)
        {
            const int w = 12, h = 6;
            int x = area.X + (area.Width - w) / 2;
            int y = area.Y + (area.Height - h) / 2;

            using (Pen pen = new Pen(iconColor, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawLines(pen, new[]
                {
                    new Point(x, y),
                    new Point(x + w / 2, y + h),
                    new Point(x + w, y)
                });
            }
        }

        private void DrawBorder(Graphics g, Rectangle r)
        {
            if (borderSize <= 0) return;

            using (SolidBrush b = new SolidBrush(borderColor))
            {
                g.FillRectangle(b, 0, 0, r.Width, borderSize);                          // arriba
                g.FillRectangle(b, 0, r.Height - borderSize, r.Width, borderSize);      // abajo
                g.FillRectangle(b, 0, 0, borderSize, r.Height);                         // izquierda
                g.FillRectangle(b, r.Width - borderSize, 0, borderSize, r.Height);      // derecha
            }
        }

        // ---------------- Dibujo de los elementos de la lista ----------------

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            // Elemento mostrado dentro del propio control (no en la lista desplegable)
            bool isEditPart = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;
            bool highlighted = !isEditPart && (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color back = isEditPart ? BackColor : (highlighted ? listHighlightColor : listBackColor);
            Color fore = isEditPart ? ForeColor : (highlighted ? Color.White : listTextColor);

            using (SolidBrush b = new SolidBrush(back))
                e.Graphics.FillRectangle(b, e.Bounds);

            Rectangle textBounds = new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), e.Font, textBounds, fore,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // ---------------- Comportamiento ----------------

        // En DropDownList, un clic en cualquier parte abre la lista.
        // En DropDown, el cuadro de edición sigue siendo escribible.
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (DropDownStyle == ComboBoxStyle.DropDownList && !DroppedDown)
                DroppedDown = true;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }
    }
}