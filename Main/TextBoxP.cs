using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TextBoxRGB
{
    [DefaultEvent("TextBoxTextChanged")]
    public partial class TextBoxP : UserControl
    {
        //Fields
        private Color borderColor = Color.MediumSlateBlue;
        private int borderSize = 2;
        private bool underlinedstyle = false;
        private Color borderFocusColor = Color.HotPink;
        private bool isFocused = false;
        private string holderText = "";
        private Label lblHolder;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        public TextBoxP()
        {
            InitializeComponent();

            textBox1.Enter += textBox1_Enter;
            textBox1.Leave += textBox1_Leave;
            textBox1.TextChanged += textBox1_TextChanged;
            textBox1.Click += textBox1_Click;
            textBox1.MouseEnter += textBox1_MouseEnter;
            textBox1.MouseLeave += textBox1_MouseLeave;
            textBox1.KeyPress += textBox1_KeyPress;

            // Etiqueta usada como placeholder cuando Multiline = true
            lblHolder = new Label();
            lblHolder.AutoSize = false;
            lblHolder.BackColor = Color.Transparent;
            lblHolder.ForeColor = Color.Gray;
            lblHolder.Cursor = Cursors.IBeam;
            lblHolder.Visible = false;
            lblHolder.Click += (s, e) => textBox1.Focus();
            this.Controls.Add(lblHolder);
            lblHolder.BringToFront();
        }

        //Default event
        public event EventHandler TextBoxTextChanged;

        //Properties
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(typeof(Color), "MediumSlateBlue")]
        [Category("Apariencia")]
        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
                this.Invalidate();
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(2)]
        [Category("Apariencia")]
        public int BorderSize
        {
            get { return borderSize; }
            set
            {
                borderSize = value;
                this.Invalidate();
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(false)]
        [Category("Apariencia")]
        public bool Underlinedstyle
        {
            get { return underlinedstyle; }
            set
            {
                underlinedstyle = value;
                this.Invalidate();
            }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(typeof(Color), "HotPink")]
        [Category("Apariencia")]
        public Color BorderFocusColor
        {
            get { return borderFocusColor; }
            set { borderFocusColor = value; }
        }

        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue("")]
        [Category("Apariencia")]
        public string Holdertext
        {
            get { return holderText; }
            set
            {
                holderText = value;
                lblHolder.Text = value;
                if (textBox1.Multiline)
                    UpdatePlaceholderVisibility();
                else if (textBox1.IsHandleCreated)
                    SendMessage(textBox1.Handle, EM_SETCUEBANNER, 0, holderText);
            }
        }

        [Browsable(true)]
        [Category("Apariencia")]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set
            {
                base.BackColor = value;
                textBox1.BackColor = value;
            }
        }

        [Browsable(true)]
        [Category("Apariencia")]
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set
            {
                base.ForeColor = value;
                textBox1.ForeColor = value;
            }
        }
        [Browsable(true)]
        [Category("Apariencia")]
        public override Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                textBox1.Font = value;
                if (lblHolder != null)
                    lblHolder.Font = value;
                if (this.DesignMode)
                    UpdateControlHeight();
            }
        }

        // Propiedad Text: reemplaza a la antigua "Texts", ahora se comporta
        // como el Text nativo de cualquier control (Text = "...", .Text)
        [Browsable(true)]
        [Category("Comportamiento")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public override string Text
        {
            get { return textBox1.Text; }
            set
            {
                textBox1.Text = value;
                UpdatePlaceholderVisibility();
            }
        }

        [Browsable(true)]
        [Category("Comportamiento")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(false)]
        public bool PasswordChar
        {
            get { return textBox1.UseSystemPasswordChar; }
            set { textBox1.UseSystemPasswordChar = value; }
        }
        [Browsable(true)]
        [Category("Comportamiento")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(false)]
        public bool Multiline
        {
            get { return textBox1.Multiline; }
            set
            {
                textBox1.Multiline = value;
                textBox1.AcceptsReturn = value;
                UpdateControlHeight();
                UpdatePlaceholderVisibility();
                if (!value && textBox1.IsHandleCreated)
                    SendMessage(textBox1.Handle, EM_SETCUEBANNER, 0, holderText);
            }
        }

        //Métodos públicos
        public void Clear()
        {
            textBox1.Clear();
            UpdatePlaceholderVisibility();
        }

        public void SelectAll()
        {
            textBox1.SelectAll();
        }

        //Overridden methods
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graph = e.Graphics;

            using (Pen penBorder = new Pen(borderColor, borderSize))
            {
                penBorder.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
                if (isFocused) penBorder.Color = borderFocusColor;

                if (underlinedstyle) //Line Style
                {
                    graph.DrawLine(penBorder, 0, this.Height - 1, this.Width, this.Height - 1);
                }
                else //Normal Style
                {
                    graph.DrawRectangle(penBorder, 0, 0, this.Width - 0.5F, this.Height - 0.5F);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (this.DesignMode)
                UpdateControlHeight();
            PositionPlaceholder();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateControlHeight();
            PositionPlaceholder();
            if (!textBox1.Multiline && !string.IsNullOrEmpty(holderText))
                SendMessage(textBox1.Handle, EM_SETCUEBANNER, 0, holderText);
            UpdatePlaceholderVisibility();
        }

        private void UpdateControlHeight()
        {
            if (textBox1.Multiline == false)
            {
                int txtHeight = TextRenderer.MeasureText("Text", this.Font).Height + 1;
                textBox1.Multiline = true;
                textBox1.MinimumSize = new Size(0, txtHeight);
                textBox1.Multiline = false;
                this.Height = textBox1.Height + this.Padding.Top + this.Padding.Bottom;
            }
            else
            {
                // Si el alto actual es muy pequeño (heredado del modo una línea), lo agranda a un mínimo razonable
                int minMultilineHeight = (TextRenderer.MeasureText("Text", this.Font).Height + 1) * 4;
                if (this.Height < minMultilineHeight)
                    this.Height = minMultilineHeight;
            }
        }

        //Placeholder para modo Multiline (etiqueta superpuesta)
        private void PositionPlaceholder()
        {
            if (lblHolder == null) return;
            lblHolder.Location = new Point(this.Padding.Left + 1, this.Padding.Top + 1);
            lblHolder.Size = new Size(this.Width - this.Padding.Left - this.Padding.Right - 2, textBox1.Font.Height + 2);
        }

        private void UpdatePlaceholderVisibility()
        {
            if (lblHolder == null) return;
            lblHolder.Visible = textBox1.Multiline
                && !textBox1.Focused
                && string.IsNullOrEmpty(textBox1.Text)
                && !string.IsNullOrEmpty(holderText);
        }

        //Cambiar color de borde en modo enfoque
        private void textBox1_Enter(object sender, EventArgs e)
        {
            isFocused = true;
            this.Invalidate();
            UpdatePlaceholderVisibility();
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            isFocused = false;
            this.Invalidate();
            UpdatePlaceholderVisibility();
        }


        //Reenvío del evento TextChanged como evento predeterminado
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            UpdatePlaceholderVisibility();
            TextBoxTextChanged?.Invoke(sender, e);
        }

        //Reenvío de eventos comunes
        private void textBox1_Click(object sender, EventArgs e)
        {
            this.OnClick(e);
        }

        private void textBox1_MouseEnter(object sender, EventArgs e)
        {
            this.OnMouseEnter(e);
        }

        private void textBox1_MouseLeave(object sender, EventArgs e)
        {
            this.OnMouseLeave(e);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            this.OnKeyPress(e);
        }
    }
}