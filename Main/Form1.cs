using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Main
{
    public partial class Form1 : Form
    {
        // ---------- Constantes ----------
        private const int MaxConjuntos = 5;   // el limite real vive en GestionConjuntos (cambiarlo alli tambien)

        // Distribucion de los radios dentro del panel "Conjuntos" (ajustar si hace falta)
        private const int RadioX0 = 10;
        private const int RadioY0 = 40;
        private const int RadioAncho = 130;   // ancho de cada columna
        private const int RadioAlto = 23;
        private const int RadioPorColumna = 5;

        // ---------- Campos ----------
        private readonly GestionConjuntos gestion = new GestionConjuntos();
        private Conjunto conjuntoActual;   // el conjunto seleccionado

        // pares guardados: (conjunto1, elemento1, conjunto2, elemento2). Los elementos pueden ser numeros o letras
        private readonly List<(string, string, string, string)> relaciones = new();

        // Controles dinamicos del panel "Pares de la relacion"
        private Control padreRel;                 // panel que contiene los checkbox y las cajas
        private int xIni, yCheck, yEtiqueta, yCaja, alturaCheck, alturaCaja;
        private readonly List<Control> dinamicos = new();
        private readonly Dictionary<string, Sunny.UI.UICheckBox> checks = new();
        private readonly Dictionary<string, Sunny.UI.UITextBox> cajas = new();

        // zoom y arrastre
        private double zoomDigrafo = 1.0;
        private Control panelDigrafoRef;
        private Button btnZoomPct;
        private bool arrastrando;
        private Point arrastreIni, scrollIni;

        public Form1()
        {
            InitializeComponent();

            // Botones (el -= evita que se enlace dos veces si el diseñador ya lo hizo)
            btnAddcj.Click -= btnAddcj_Click;
            btnAddcj.Click += btnAddcj_Click;

            btnDeletecj.Click -= btnDeletecj_Click;
            btnDeletecj.Click += btnDeletecj_Click;

            btnAggElementos.Click -= btnAggElementos_Click;
            btnAggElementos.Click += btnAggElementos_Click;

            btnGenerarRelacion.Click -= btnGenerarRelacion_Click;
            btnGenerarRelacion.Click += btnGenerarRelacion_Click;

            // Quita los números de ejemplo que traen los ListBox desde el diseñador
            lbElements.Items.Clear();
            uiListBox2.Items.Clear();
            lbElements.Font = new Font("Consolas", 12);

            EnlazarEnter(txtElementos);   // enter para agregar elementos
            EnlazarFiltro(txtElementos);  // solo letras y numeros (y separadores)

            // al cambiar el tamaño de la ventana, la matriz y el dígrafo se reajustan
            this.Resize += (s, ev) =>
            {
                if (relaciones.Count > 0 && WindowState != FormWindowState.Minimized)
                    GenerarSalidas();
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PrepararPanelRelaciones();   // oculta CheckA/B/C, las 3 cajas y sus etiquetas

            CrearRadio(gestion.AddNewSet());   // 1
            CrearRadio(gestion.AddNewSet());   // 2
            CrearRadio(gestion.AddNewSet());   // 3

            // Marcar el primero dispara Radio_CheckedChanged
            PrimerRadio().Checked = true;

            RehacerChecks();
        }

        //  RADIOS (SunnyUI)
        private IEnumerable<Sunny.UI.UIRadioButton> Radios() =>
            PnlConjuntos.Controls.OfType<Sunny.UI.UIRadioButton>();

        private Sunny.UI.UIRadioButton PrimerRadio() => Radios().First();

        private void CrearRadio(Conjunto conjunto)
        {
            int indice = Radios().Count();

            var rb = new Sunny.UI.UIRadioButton();
            rb.Text = conjunto.Titulo;
            rb.Tag = conjunto;
            rb.Size = new Size(RadioAncho, RadioAlto);
            // posicion manual (si el panel es un FlowLayoutPanel simplemente se ignora)
            rb.Location = new Point(
                RadioX0 + (indice / RadioPorColumna) * RadioAncho,
                RadioY0 + (indice % RadioPorColumna) * RadioAlto);
            rb.CheckedChanged += Radio_CheckedChanged;
            PnlConjuntos.Controls.Add(rb);
        }

        private void Radio_CheckedChanged(object sender, EventArgs e)
        {
            var rb = (Sunny.UI.UIRadioButton)sender;
            if (!rb.Checked) return;

            conjuntoActual = (Conjunto)rb.Tag;
            MostrarElementos();
        }

        // Borra todos los radios y los recrea desde gestion
        // (solo toca los radios: los botones Agregar/Eliminar del panel se quedan)
        private void RehacerRadios()
        {
            foreach (var rb in Radios().ToList())
            {
                PnlConjuntos.Controls.Remove(rb);
                rb.Dispose();
            }
            conjuntoActual = null;

            foreach (Conjunto c in gestion.Conjuntos)
                CrearRadio(c);

            PrimerRadio().Checked = true;
        }

        // Busca un conjunto por su nombre
        private Conjunto Buscar(string nombre) =>
            gestion.Conjuntos.FirstOrDefault(c => c.Nombre == nombre);

        // Nombre corto para mostrar (el nombre del conjunto ya es corto: "1", "A"...)
        private static string Corto(string nombre) => nombre;

        //  CHECKBOXES + CAJAS DINAMICOS (SunnyUI)
        //  Fila 1: un checkbox por conjunto
        //  Fila 2: una etiqueta + una caja por conjunto

        // Posicion de un control respecto a un ancestro (sin necesitar el handle)
        private Point PosicionEn(Control c, Control ancestro)
        {
            Point p = c.Location;
            Control q = c.Parent;
            while (q != null && q != ancestro)
            {
                p.Offset(q.Location);
                q = q.Parent;
            }
            return p;
        }

        // Se ejecuta una sola vez: guarda donde estaban los controles fijos
        // del diseñador, los oculta y deja listo el panel para los dinamicos.
        private void PrepararPanelRelaciones()
        {
            padreRel = CheckA.Parent;

            xIni = CheckA.Left;
            yCheck = CheckA.Top;
            alturaCheck = CheckA.Height;

            yCaja = PosicionEn(txtRelacion1, padreRel).Y;
            alturaCaja = txtRelacion1.Height;

            // etiquetas fijas "1", "2", "3" que estan encima de las cajas (son FoxBigLabel)
            yEtiqueta = PosicionEn(lblRelacion1, padreRel).Y;

            foreach (Control c in new Control[]
            {
                CheckA, CheckB, CheckC,
                lblRelacion1, lblRelacion2, foxBigLabel10,
                txtRelacion1, bigTextBox2, txtRelacion3
            })
                c.Visible = false;
        }

        // Borra y recrea los controles desde gestion, conservando lo marcado/escrito
        private void RehacerChecks()
        {
            var marcadosAntes = new HashSet<string>(ConjuntosMarcados());
            var textosAntes = cajas.ToDictionary(k => k.Key, k => k.Value.Text);

            padreRel.SuspendLayout();

            foreach (Control c in dinamicos)
            {
                padreRel.Controls.Remove(c);
                c.Dispose();
            }
            dinamicos.Clear();
            checks.Clear();
            cajas.Clear();

            int n = gestion.Conjuntos.Count;
            const int separacion = 6;
            int disponible = padreRel.ClientSize.Width - xIni - 12;
            int ancho = Math.Max(40, Math.Min(70, (disponible - (n - 1) * separacion) / n));

            int i = 0;
            foreach (Conjunto c in gestion.Conjuntos)
            {
                int x = xIni + i * (ancho + separacion);
                bool marcado = marcadosAntes.Contains(c.Nombre);

                // --- checkbox ---
                var chk = new Sunny.UI.UICheckBox();
                chk.Text = Corto(c.Nombre);
                chk.Location = new Point(x, yCheck - 3);
                chk.Size = new Size(ancho, Math.Max(alturaCheck, 26));
                chk.Checked = marcado;
                chk.CheckedChanged += CheckConjunto_CheckedChanged;

                // --- etiqueta encima de la caja ---
                var lbl = new Sunny.UI.UILabel();
                lbl.Text = Corto(c.Nombre);
                lbl.Location = new Point(x, yEtiqueta);
                lbl.Size = new Size(ancho, 23);
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);

                // --- caja de texto (solo letras y numeros) ---
                var txt = new Sunny.UI.UITextBox();
                txt.Watermark = "Ej: " + (i + 1);
                txt.Location = new Point(x, yCaja);
                txt.Size = new Size(ancho, alturaCaja);
                if (marcado && textosAntes.TryGetValue(c.Nombre, out string previo))
                    txt.Text = previo;
                txt.Enabled = marcado;
                EnlazarFiltro(txt);

                padreRel.Controls.Add(chk);
                padreRel.Controls.Add(lbl);
                padreRel.Controls.Add(txt);
                dinamicos.Add(chk);
                dinamicos.Add(lbl);
                dinamicos.Add(txt);

                checks[c.Nombre] = chk;
                cajas[c.Nombre] = txt;
                i++;
            }

            padreRel.ResumeLayout();

            CheckConjunto_CheckedChanged(null, EventArgs.Empty);
        }

        // devuelve los nombres de los conjuntos marcados (en orden de creacion)
        private List<string> ConjuntosMarcados()
        {
            var lista = new List<string>();
            foreach (Conjunto c in gestion.Conjuntos)
                if (checks.TryGetValue(c.Nombre, out Sunny.UI.UICheckBox chk) && chk.Checked)
                    lista.Add(c.Nombre);
            return lista;
        }

        // devuelve el texto de la caja que corresponde a cada conjunto
        private string TextoCaja(string conjunto) =>
            cajas.TryGetValue(conjunto, out Sunny.UI.UITextBox t) ? t.Text : "";

        // se ejecuta cada vez que marcas o desmarcas un conjunto
        private void CheckConjunto_CheckedChanged(object sender, EventArgs e)
        {
            foreach (var par in checks)
            {
                Sunny.UI.UITextBox caja = cajas[par.Key];
                caja.Enabled = par.Value.Checked;          // solo activa las marcadas
                if (!par.Value.Checked) caja.Text = "";    // vacia las desmarcadas
            }

            // con un solo conjunto marcado se escribe el par completo (ej. 1,2 o a,b)
            bool unico = ConjuntosMarcados().Count == 1;
            int num = 1;
            foreach (Conjunto conj in gestion.Conjuntos)
            {
                if (cajas.TryGetValue(conj.Nombre, out Sunny.UI.UITextBox cj))
                    cj.Watermark = (unico && checks[conj.Nombre].Checked) ? "Ej: 1,a" : "Ej: " + num;
                num++;
            }

            MostrarRelaciones();
        }

        //  ELEMENTOS
        private void MostrarElementos()
        {
            lbElements.Items.Clear();
            if (conjuntoActual == null) return;

            foreach (string el in conjuntoActual.Elementos)
                lbElements.Items.Add(el);
        }

        private void btnAggElementos_Click(object sender, EventArgs e)
        {
            if (conjuntoActual == null)
            {
                MessageBox.Show("Primero seleccione un conjunto.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string texto = LeerTexto(txtElementos);
            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show("Escriba los elementos separados por coma, ej. 1,2,a,b", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // se valida todo antes de agregar nada
            var nuevos = new List<string>();
            foreach (string parte in texto.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string el = parte.Trim();
                if (!Conjunto.EsValido(el))
                {
                    MessageBox.Show($"'{el}' no es válido. Use solo letras y números (sin caracteres especiales).",
                        "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                nuevos.Add(el);
            }

            foreach (string el in nuevos)
                conjuntoActual.AgregarElemento(el);   // ignora los repetidos

            MostrarElementos();
            txtElementos.Text = "";
        }

        //  AGREGAR / ELIMINAR CONJUNTOS
        private void btnAddcj_Click(object sender, EventArgs e)
        {
            if (gestion.Lleno)
            {
                MessageBox.Show($"Alcanzaste el máximo de {MaxConjuntos} conjuntos.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string nombre = PedirNombre(gestion.NombreLibre());
            if (nombre == null) return;   // cancelado

            Conjunto nuevo = gestion.AddNewSet(nombre);
            if (nuevo == null)
            {
                MessageBox.Show("No se pudo crear el conjunto.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            RehacerRadios();   // aparece su radio
            RehacerChecks();   // aparece su checkbox, su etiqueta y su caja
        }

        // Pide el nombre del conjunto (solo letras y numeros, sin repetir). null = cancelado
        private string PedirNombre(string sugerido)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Nuevo conjunto";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ShowInTaskbar = false;
                dlg.ClientSize = new Size(330, 120);

                var lbl = new Label { Text = "Nombre del conjunto (letras y números):", AutoSize = true, Location = new Point(12, 14) };
                var txt = new TextBox { Text = sugerido, Location = new Point(15, 40), Width = 300, MaxLength = 8 };
                var ok = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Location = new Point(145, 80), Size = new Size(80, 28) };
                var cancelar = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(235, 80), Size = new Size(80, 28) };

                txt.KeyPress += (s, ev) =>
                {
                    if (!char.IsControl(ev.KeyChar) && !char.IsLetterOrDigit(ev.KeyChar))
                        ev.Handled = true;
                };
                dlg.Shown += (s, ev) => { txt.Focus(); txt.SelectAll(); };

                dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cancelar });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancelar;

                while (true)
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return null;

                    string nombre = txt.Text.Trim();
                    if (!Conjunto.EsValido(nombre))
                    {
                        MessageBox.Show("Use solo letras y números (sin espacios ni caracteres especiales).",
                            "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        continue;
                    }
                    if (gestion.Existe(nombre))
                    {
                        MessageBox.Show($"Ya existe un conjunto llamado '{nombre}'.", "Información",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        continue;
                    }
                    return nombre;
                }
            }
        }

        private void btnDeletecj_Click(object sender, EventArgs e)
        {
            if (conjuntoActual == null) return;

            var respuesta = MessageBox.Show(
                $"¿Eliminar {conjuntoActual.Titulo} y todos sus elementos?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (respuesta != DialogResult.Yes) return;

            string nombre = conjuntoActual.Nombre;
            if (!gestion.DeleteSet(conjuntoActual))
            {
                MessageBox.Show("Debe haber al menos 1 conjunto.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // las relaciones que usaban ese conjunto ya no tienen sentido
            relaciones.RemoveAll(r => r.Item1 == nombre || r.Item3 == nombre);

            RehacerRadios();
            RehacerChecks();
        }

        //  RELACIONES

        // crea el par con los valores escritos y lo guarda
        private void btnGenerarRelacion_Click(object sender, EventArgs e)
        {
            var marcados = ConjuntosMarcados();

            if (marcados.Count < 1 || marcados.Count > 2)
            {
                MessageBox.Show("Marque un conjunto (A con A) o dos conjuntos para relacionar.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string c1 = marcados[0];
            string c2 = marcados.Count == 2 ? marcados[1] : marcados[0];
            string x, y;

            if (marcados.Count == 2)
            {
                // dos conjuntos: un elemento en cada caja
                x = TextoCaja(c1).Trim();
                y = TextoCaja(c2).Trim();
                if (!Conjunto.EsValido(x) || !Conjunto.EsValido(y))
                {
                    MessageBox.Show("Ingrese un elemento válido (letras o números) en cada caja.", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            else
            {
                // un solo conjunto: el par completo en su caja, ej. 1,2 o a,b
                string[] partes = TextoCaja(c1).Split(new[] { ',', ';', ' ' },
                                                      StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length != 2 || !Conjunto.EsValido(partes[0]) || !Conjunto.EsValido(partes[1]))
                {
                    MessageBox.Show("Escriba el par separado por coma con letras o números, ej. 1,2 o a,b", "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                x = partes[0];
                y = partes[1];
            }

            Conjunto s1 = Buscar(c1);
            Conjunto s2 = Buscar(c2);

            // cada elemento tiene que pertenecer a su conjunto
            if (!s1.Elementos.Contains(x))
            {
                MessageBox.Show($"{x} no es un elemento del conjunto {c1}.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!s2.Elementos.Contains(y))
            {
                MessageBox.Show($"{y} no es un elemento del conjunto {c2}.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (relaciones.Contains((c1, x, c2, y))) // evita pares repetidos
            {
                MessageBox.Show("Ese par ya fue agregado.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            relaciones.Add((c1, x, c2, y));
            MostrarRelaciones();

            // limpia todas las cajas
            foreach (var caja in cajas.Values)
                caja.Text = "";
        }

        // muestra los pares de la combinacion marcada en el ListBox de relaciones
        private void MostrarRelaciones()
        {
            uiListBox2.Items.Clear();

            var marcados = ConjuntosMarcados();
            if (marcados.Count < 1 || marcados.Count > 2) return; // uno (A con A) o dos conjuntos

            string c1 = marcados[0];
            string c2 = marcados.Count == 2 ? marcados[1] : marcados[0];

            foreach (var r in relaciones)
            {
                if (r.Item1 == c1 && r.Item3 == c2)
                    uiListBox2.Items.Add($"{Corto(r.Item1)}→{Corto(r.Item3)}: ({r.Item2}, {r.Item4})");
            }
        }

        private List<Relacion> ConstruirRelaciones()
        {
            var lista = new List<Relacion>();

            var pares = relaciones
                .Select(r => (r.Item1, r.Item3))
                .Distinct()
                .OrderBy(p => p.Item1, StringComparer.Ordinal)
                .ThenBy(p => p.Item2, StringComparer.Ordinal);

            foreach (var (n1, n2) in pares)
            {
                Conjunto s1 = Buscar(n1);
                Conjunto s2 = Buscar(n2);
                if (s1 == null || s2 == null) continue;

                var rel = new Relacion(Corto(n1), s1.Elementos.ToList(), Corto(n2), s2.Elementos.ToList());

                foreach (var r in relaciones)
                    if (r.Item1 == n1 && r.Item3 == n2)
                        rel.Agregar(r.Item2, r.Item4, out _);

                lista.Add(rel);
            }
            return lista;
        }

        // Conjuntos que participan en alguna relacion (para dibujar una sola matriz y un solo digrafo)
        private List<GrupoGrafica> ConstruirGrupos()
        {
            var usados = new HashSet<string>();
            foreach (var r in relaciones)
            {
                usados.Add(r.Item1);
                usados.Add(r.Item3);
            }

            return gestion.Conjuntos
                .Where(c => usados.Contains(c.Nombre))
                .Select(c => new GrupoGrafica
                {
                    Clave = c.Nombre,
                    Etiqueta = Corto(c.Nombre),
                    Elementos = c.Elementos.ToList()
                })
                .ToList();
        }

        //  UTILIDADES

        // lee el texto real del control
        private string LeerTexto(Control control)
        {
            if (control is TextBox tb) return tb.Text;

            foreach (Control hijo in control.Controls)
            {
                string t = LeerTexto(hijo);
                if (hijo is TextBox || !string.IsNullOrEmpty(t)) return t;
            }
            return control.Text;
        }

        // presionar enter para agregar los elementos
        private void txtElementos_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // evita el "ding" de Windows
                btnAggElementos_Click(sender, EventArgs.Empty);
            }
        }

        private void EnlazarEnter(Control control)
        {
            control.KeyDown += txtElementos_KeyDown;
            foreach (Control hijo in control.Controls)
                EnlazarEnter(hijo);
        }

        // Solo letras, numeros y separadores (coma, punto y coma, espacio); bloquea el resto al teclear
        private static void SoloAlfanumerico(object sender, KeyPressEventArgs e)
        {
            char c = e.KeyChar;
            if (char.IsControl(c) || char.IsLetterOrDigit(c) || c == ',' || c == ';' || c == ' ') return;
            e.Handled = true;
        }

        private static void EnlazarFiltro(Control control)
        {
            control.KeyPress -= SoloAlfanumerico;
            control.KeyPress += SoloAlfanumerico;
            foreach (Control hijo in control.Controls)
                EnlazarFiltro(hijo);
        }

        //  Paneles grandes: se encuentran buscando sus titulos
        private Control BuscarPorTexto(Control raiz, string inicio)
        {
            foreach (Control hijo in raiz.Controls)
            {
                if (!string.IsNullOrEmpty(hijo.Text) &&
                    hijo.Text.StartsWith(inicio, StringComparison.OrdinalIgnoreCase))
                    return hijo;

                Control encontrado = BuscarPorTexto(hijo, inicio);
                if (encontrado != null) return encontrado;
            }
            return null;
        }

        // Devuelve el panel real que contiene el titulo
        private Control PanelPorTitulo(params string[] posibles)
        {
            foreach (string p in posibles)
            {
                Control c = BuscarPorTexto(this, p);
                if (c != null) return c.Parent;
            }
            return null;
        }

        private bool ObtenerPaneles(out Control pDigrafo, out Control pMatriz, out Control pProp)
        {
            pDigrafo = PanelPorTitulo("Digrafo", "Dígrafo");
            pMatriz = PanelPorTitulo("Matriz");
            pProp = PanelPorTitulo("Propiedad");

            if (pDigrafo == null || pMatriz == null || pProp == null)
            {
                MessageBox.Show("No encontre los paneles Digrafo, Matriz o Propiedades.\n" +
                                "Revise que sus titulos empiecen con esas palabras.", "Información",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        // Crea (una sola vez) un panel con scroll + imagen dentro del panel grande
        private PictureBox ImagenEn(Control cont)
        {
            int top = 45; // deja libre el título del panel
            var sp = cont.Controls.OfType<Panel>().FirstOrDefault(p => p.Name == "scrollAuto");
            if (sp == null)
            {
                sp = new Panel();
                sp.Name = "scrollAuto";
                sp.Location = new Point(5, top);
                sp.Size = new Size(cont.Width - 10, cont.Height - top - 5);
                sp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                sp.AutoScroll = true;
                sp.BackColor = Color.White;

                var pb = new PictureBox();
                pb.Name = "pbAuto";
                pb.SizeMode = PictureBoxSizeMode.AutoSize;
                pb.Location = new Point(0, 0);
                Panel panelScroll = sp;
                pb.MouseEnter += (s, ev) => panelScroll.Focus(); // rueda del mouse

                sp.Controls.Add(pb);
                cont.Controls.Add(sp);
                sp.BringToFront();
            }
            return (PictureBox)sp.Controls["pbAuto"];
        }

        private void MostrarImagen(PictureBox pb, Bitmap nueva)
        {
            Image vieja = pb.Image;
            pb.Image = nueva;
            if (vieja != null) vieja.Dispose();
        }

        private static Size AreaUtil(PictureBox pb)
        {
            Size s = pb.Parent.Size;
            return new Size(Math.Max(s.Width - SystemInformation.VerticalScrollBarWidth - 4, 200),
                            Math.Max(s.Height - SystemInformation.HorizontalScrollBarHeight - 4, 150));
        }

        private void PrepararZoom(Control pDigrafo, PictureBox pb)
        {
            if (panelDigrafoRef != null) return;
            panelDigrafoRef = pDigrafo;
            var sp = (Panel)pb.Parent;

            Button Crear(string txt, int x, int ancho, EventHandler clic)
            {
                var b = new Button();
                b.Text = txt;
                b.Size = new Size(ancho, 26);
                b.Location = new Point(pDigrafo.Width - x, 8);
                b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                b.FlatStyle = FlatStyle.Flat;
                b.BackColor = Color.White;
                b.Click += clic;
                pDigrafo.Controls.Add(b);
                b.BringToFront();
                return b;
            }

            Crear("−", 134, 30, (s, ev) => CambiarZoom(zoomDigrafo / 1.25));
            btnZoomPct = Crear("100%", 100, 60, (s, ev) => CambiarZoom(1.0));
            Crear("+", 36, 30, (s, ev) => CambiarZoom(zoomDigrafo * 1.25));

            MouseEventHandler rueda = (s, ev) =>
            {
                if (Control.ModifierKeys != Keys.Control) return;
                if (ev is HandledMouseEventArgs h) h.Handled = true;
                CambiarZoom(ev.Delta > 0 ? zoomDigrafo * 1.15 : zoomDigrafo / 1.15);
            };
            sp.MouseWheel += rueda;
            pb.MouseWheel += rueda;

            pb.MouseDown += (s, ev) =>
            {
                if (ev.Button != MouseButtons.Left) return;
                arrastrando = true;
                arrastreIni = Cursor.Position;
                scrollIni = new Point(-sp.AutoScrollPosition.X, -sp.AutoScrollPosition.Y);
                pb.Cursor = Cursors.Hand;
            };
            pb.MouseMove += (s, ev) =>
            {
                if (!arrastrando) return;
                int dx = Cursor.Position.X - arrastreIni.X;
                int dy = Cursor.Position.Y - arrastreIni.Y;
                sp.AutoScrollPosition = new Point(Math.Max(0, scrollIni.X - dx), Math.Max(0, scrollIni.Y - dy));
            };
            pb.MouseUp += (s, ev) => { arrastrando = false; pb.Cursor = Cursors.Default; };
        }

        private void CambiarZoom(double nuevo)
        {
            nuevo = Math.Max(0.5, Math.Min(4.0, nuevo));
            if (Math.Abs(nuevo - zoomDigrafo) < 0.001) return;
            zoomDigrafo = nuevo;
            if (btnZoomPct != null) btnZoomPct.Text = (int)Math.Round(nuevo * 100) + "%";
            if (relaciones.Count == 0 || panelDigrafoRef == null) return;

            PictureBox pb = ImagenEn(panelDigrafoRef);
            var sp = (Panel)pb.Parent;
            double fx = (-sp.AutoScrollPosition.X + sp.ClientSize.Width / 2.0) / Math.Max(pb.Width, 1);
            double fy = (-sp.AutoScrollPosition.Y + sp.ClientSize.Height / 2.0) / Math.Max(pb.Height, 1);

            DibujarDigrafo(panelDigrafoRef);

            sp.AutoScrollPosition = new Point(
                Math.Max(0, (int)(fx * pb.Width - sp.ClientSize.Width / 2.0)),
                Math.Max(0, (int)(fy * pb.Height - sp.ClientSize.Height / 2.0)));
        }

        private void DibujarMatrizYPropiedades(Control pMatriz, Control pProp)
        {
            PictureBox pbM = ImagenEn(pMatriz);
            Size aM = AreaUtil(pbM);
            MostrarImagen(pbM, Grafica.Matriz(ConstruirGrupos(), relaciones, aM.Width, aM.Height));

            PictureBox pbP = ImagenEn(pProp);
            MostrarImagen(pbP, Grafica.Propiedades(ConstruirRelaciones(), AreaUtil(pbP).Width));
        }

        private void DibujarDigrafo(Control pDigrafo)
        {
            PictureBox pb = ImagenEn(pDigrafo);
            PrepararZoom(pDigrafo, pb);
            Size a = AreaUtil(pb);
            MostrarImagen(pb, Grafica.Digrafo(ConstruirGrupos(), relaciones, a.Width, a.Height, (float)zoomDigrafo));
        }

        // Genera las tres salidas de una vez
        private void GenerarSalidas()
        {
            if (!ObtenerPaneles(out Control pDigrafo, out Control pMatriz, out Control pProp)) return;
            DibujarMatrizYPropiedades(pMatriz, pProp);
            DibujarDigrafo(pDigrafo);
        }

        //  BOTONES: Matriz / Diagrama / Exportar
        private void btnMatriz_Click(object sender, EventArgs e)
        {
            if (relaciones.Count == 0)
            {
                MessageBox.Show("Primero genere una relacion.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ObtenerPaneles(out Control pDigrafo, out Control pMatriz, out Control pProp)) return;
            DibujarMatrizYPropiedades(pMatriz, pProp);
        }

        private void btnDiagrama_Click(object sender, EventArgs e)
        {
            if (relaciones.Count == 0)
            {
                MessageBox.Show("Primero genere una relacion.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ObtenerPaneles(out Control pDigrafo, out Control pMatriz, out Control pProp)) return;
            DibujarDigrafo(pDigrafo);
        }

        private void btnExportar_Click(object sender, EventArgs e)
        {
            if (relaciones.Count == 0)
            {
                MessageBox.Show("Primero genere una relacion.", "Información",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Elija la carpeta donde guardar las imagenes";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    List<GrupoGrafica> grupos = ConstruirGrupos();

                    using (Bitmap d = Grafica.Digrafo(grupos, relaciones, 1200, 900))
                        d.Save(System.IO.Path.Combine(dlg.SelectedPath, "digrafo.png"),
                               System.Drawing.Imaging.ImageFormat.Png);
                    using (Bitmap m = Grafica.Matriz(grupos, relaciones))
                        m.Save(System.IO.Path.Combine(dlg.SelectedPath, "matriz.png"),
                               System.Drawing.Imaging.ImageFormat.Png);
                    MessageBox.Show("Imagenes guardadas en:\n" + dlg.SelectedPath, "Información",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo guardar: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //  Handlers vacios que el diseñador puede tener enlazados (no borrar
        //  a menos que tambien los quites desde el diseñador)
        private void uiPanel1_Click(object sender, EventArgs e) { }
        private void uiPanel1_Click_1(object sender, EventArgs e) { }
        private void uiPanel3_Click(object sender, EventArgs e) { }
        private void uiPanel4_Click(object sender, EventArgs e) { }
        private void btnGenerarRelacion_Click_1(object sender, EventArgs e) { }

        private void btnEliminarElementos_Click(object sender, EventArgs e)
        {
            if (conjuntoActual == null) return;
            if (lbElements.SelectedItem == null) return;

            conjuntoActual.EliminarElemento(lbElements.SelectedItem.ToString());
            MostrarElementos();
        }

        private void Reiniciar_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
    }
}