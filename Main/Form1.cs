using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Main
{
    public partial class Form1 : Form
    {
        // Diccionario privado de lectura para almacenar los  conjuntos.
        // La Clave (string) es el nombre del conjunto ej. A, B
        // y el Valor (List<int>) es la lista de numeros enteros que contiene ese conjunto.
        private readonly Dictionary<string, List<int>> conjuntos = new();
        private readonly List<(string, int, string, int)> relaciones = new();

        private readonly List<string> ordenConjuntos = new();

        public Form1()
        {
            InitializeComponent();

            // Radio buttons
            rbA.CheckedChanged += rbConjunto_CheckedChanged;
            rbB.CheckedChanged += rbConjunto_CheckedChanged;
            rbC.CheckedChanged += rbConjunto_CheckedChanged;

            // Botones (el -= evita que se enlace dos veces si el diseñador ya lo hizo)
            btnAddcj.Click -= btnAddcj_Click;
            btnAddcj.Click += btnAddcj_Click;

            btnDeletecj.Click -= btnDeletecj_Click;
            btnDeletecj.Click += btnDeletecj_Click;

            btnAggElementos.Click -= btnAggElementos_Click;
            btnAggElementos.Click += btnAggElementos_Click;

            // Quita los números de ejemplo (1 a 5) que traen los ListBox desde el diseñador
            uiListBox1.Items.Clear();
            uiListBox2.Items.Clear();

            uiListBox1.Items.Clear();
            uiListBox2.Items.Clear();

            uiListBox1.Font = new Font("Consolas", 12); // NUEVO: letra de ancho fijo para que las columnas se alineen

            EnlazarEnter(txtElementos); // enter para el cuadro agregarr los elementos

            ActualizarEstadoBotones();

            //eventos del panel de relaciones
            btnGenerarRelacion.Click -= btnGenerarRelacion_Click;
            btnGenerarRelacion.Click += btnGenerarRelacion_Click;

            CheckA.CheckedChanged += CheckConjunto_CheckedChanged;
            CheckB.CheckedChanged += CheckConjunto_CheckedChanged;
            CheckC.CheckedChanged += CheckConjunto_CheckedChanged;


            CheckConjunto_CheckedChanged(null, EventArgs.Empty);

            ActualizarEstadoBotones();
        }

        //saber que occion el usuario escojio
        private string ConjuntoSeleccionado()
        {
            if (rbA.Checked) return "A"; // Si el btn A es seleccionado va a retornar a A
            if (rbB.Checked) return "B"; // Si el btn A es seleccionado va a retornar a B
            if (rbC.Checked) return "C"; // Si el btn A es seleccionado va a retornar a C
            return null;
        }

        // muestra en el ListBox los elementos del conjunto seleccionado
        private void MostrarElementos()
        {
            uiListBox1.Items.Clear(); // borra lo que habia

            string sel = ConjuntoSeleccionado(); // letra del conjunto marcado (A, B o C)

            if (sel != null && conjuntos.ContainsKey(sel)) // solo si ese conjunto ya fue agregado
            {
                foreach (int n in conjuntos[sel])
                    uiListBox1.Items.Add(n.ToString());
            }
        }

        private void uiPanel1_Click(object sender, EventArgs e)
        {

        }

        private void uiPanel3_Click(object sender, EventArgs e)
        {

        }

        private void btnAddcj_Click(object sender, EventArgs e)
        {
            string nombre = ConjuntoSeleccionado(); // obtiene la letra del conjunto seleccionado A, B o C

            if (nombre == null) // validamos que se haya seleccionado un conjunto
            {
                MessageBox.Show("Seleccione un conjunto (A, B o C).");
                return;
            }

            if (conjuntos.ContainsKey(nombre)) // Valida que el conjunto no haya sido agregado anteriormente
            {
                MessageBox.Show($"El conjunto {nombre} ya fue agregado.");
                return;
            }

            conjuntos[nombre] = new List<int>(); // inicializa el conjunto en el diccionario con una lista vacia de numeros
            ordenConjuntos.Add(nombre); // recuerda el orden en que se agrego
            MostrarElementos(); // lo muestra en el cuadro de elementos
            ActualizarEstadoBotones(); // actualiza el estado visual de los botones
            MessageBox.Show($"El conjunto {nombre} se ha añadido correctamente.");
            ActualizarEstadoBotones(); // actualiza el estado visual de los botones
        }

        private void btnDeletecj_Click(object sender, EventArgs e)
        {
            string nombre = ConjuntoSeleccionado(); // obtiene la letra del conjunto seleccionado A, B o C


            if (nombre == null || !conjuntos.ContainsKey(nombre)) //valida que se haya seleccionado un conjunto que exista en el dic
            {
                MessageBox.Show("Ese conjunto no existe.");
                return;
            }

            conjuntos.Remove(nombre); // elimina el conjunto del diccionario usando el nombre como id
            ordenConjuntos.Remove(nombre);//elimina la lista de orden
            relaciones.RemoveAll(r => r.Item1 == nombre || r.Item3 == nombre); // quita las relaciones que usaban en ese conjunto
            MostrarElementos();   // refresca el ListBox de elementos
            MostrarRelaciones(); // las relaciones
            ActualizarEstadoBotones();// actualiza la interfaz
        }

        private void rbConjunto_CheckedChanged(object sender, EventArgs e) // cuando cambie lo que seleccionamos, actualiza los botoneas
        {
            ActualizarEstadoBotones(); // actualiza los botones de la interfas segun la opcion
            MostrarElementos();
        }

        private void ActualizarEstadoBotones() //actualiza los botones y los checkboxes
        {
            //obtiene y verifica si existe el el dic
            string sel = ConjuntoSeleccionado();
            bool existe = sel != null && conjuntos.ContainsKey(sel);

            btnAddcj.Enabled = sel != null && !existe;
            btnDeletecj.Enabled = existe;

            // los checkbox solo va a funcionar solo si los conjuntos han sido creados 
            CheckA.Enabled = conjuntos.ContainsKey("A");
            CheckB.Enabled = conjuntos.ContainsKey("B");
            CheckC.Enabled = conjuntos.ContainsKey("C");
            //si un conjunto no está disponible, se desmarca su casilla automáticamente
            if (!CheckA.Enabled) CheckA.Checked = false;
            if (!CheckB.Enabled) CheckB.Checked = false;
            if (!CheckC.Enabled) CheckC.Checked = false;
        }

        private void btnAggElementos_Click(object sender, EventArgs e)
        {
            string sel = ConjuntoSeleccionado(); // obtiene la letra del conjunto seleccionado

            if (sel == null || !conjuntos.ContainsKey(sel)) // el conjunto tiene que estar seleccionado y haber sido agregado
            {
                MessageBox.Show("Primero seleccione y agregue un conjunto.");
                return;
            }

            uiListBox1.Items.Clear(); // borra lo que habia

            string texto = LeerTexto(txtElementos); // texto que escribio el usuario
            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show("Escriba los elementos, ej. 1,2,3,4");
                return;
            }

            var elementos = new List<int>();
            foreach (string parte in texto.Split(',')) // recorre los conjuntos en orden de creacion
            {
                if (int.TryParse(parte.Trim(), out int n)) // si es un numero valido lo agrega
                    elementos.Add(n);
                else if (!string.IsNullOrWhiteSpace(parte)) // si escribio algo que no es numero avisa y cancela
                {
                    MessageBox.Show($"'{parte.Trim()}' no es un número válido.");
                    return;
                }
            }

            // une los elementos que ya tenia con los nuevos, sin diplicarlo y los ordena
            conjuntos[sel] = conjuntos[sel].Concat(elementos).Distinct().ToList();

            MostrarElementos(); // refresca el ListBox
            txtElementos.Text = ""; // limpia el cuadro de texto
        }

        // devuelve las letras de los conjuntos marcados en los checkbox
        private List<string> ConjuntosMarcados()
        {
            var lista = new List<string>();
            if (CheckA.Checked) lista.Add("A");
            if (CheckB.Checked) lista.Add("B");
            if (CheckC.Checked) lista.Add("C");
            return lista;
        }

        // devuelve el texto de la caja que corresponde a cada conjunto
        private string TextoCaja(string conjunto)
        {
            if (conjunto == "A") return txtRelacion1.Text;
            if (conjunto == "B") return bigTextBox2.Text;
            return txtRelacion3.Text;
        }


        // se ejecuta cada vez que marcas o desmarcas A, B o C
        private void CheckConjunto_CheckedChanged(object sender, EventArgs e)
        {
            // activa solo las cajas de los conjuntos marcados
            txtRelacion1.Enabled = CheckA.Checked;
            bigTextBox2.Enabled = CheckB.Checked;
            txtRelacion3.Enabled = CheckC.Checked;

            // vacía las cajas de los conjuntos desmarcados
            if (!CheckA.Checked) txtRelacion1.Text = "";
            if (!CheckB.Checked) bigTextBox2.Text = "";
            if (!CheckC.Checked) txtRelacion3.Text = "";

            MostrarRelaciones();
        }

        // crea el par con los valores escritos y lo guarda
        private void btnGenerarRelacion_Click(object sender, EventArgs e)
        {
            var marcados = ConjuntosMarcados();

            if (marcados.Count != 2) // se relacionan exactamente dos conjuntos
            {
                MessageBox.Show("Marque exactamente dos conjuntos para relacionar.");
                return;
            }

            string c1 = marcados[0]; // primer conjunto del par
            string c2 = marcados[1]; // segundo conjunto del par

            // los dos valores tienen que ser numeros
            if (!int.TryParse(TextoCaja(c1).Trim(), out int x) || !int.TryParse(TextoCaja(c2).Trim(), out int y))
            {
                MessageBox.Show("Ingrese un numero valido en cada caja.");
                return;
            }

            // cada numero tiene que pertenecer a su conjunto
            if (!conjuntos[c1].Contains(x))
            {
                MessageBox.Show($"{x} no es un elemento del conjunto {c1}.");
                return;
            }
            if (!conjuntos[c2].Contains(y))
            {
                MessageBox.Show($"{y} no es un elemento del conjunto {c2}.");
                return;
            }

            if (relaciones.Contains((c1, x, c2, y))) // evita pares repetidos
            {
                MessageBox.Show("Ese par ya fue agregado.");
                return;
            }

            relaciones.Add((c1, x, c2, y));
            MostrarRelaciones();

            // limpia las cajas de los dos conjuntos
            txtRelacion1.Text = "";
            bigTextBox2.Text = "";
            txtRelacion3.Text = "";
        }

        // muestra todos los pares en el ListBox de relaciones
        private void MostrarRelaciones()
        {
            uiListBox2.Items.Clear(); // borra lo que habia

            var marcados = ConjuntosMarcados(); // letras marcadas
            if (marcados.Count != 2) return; // solo se muestran cuando hay exactamente dos marcados

            string c1 = marcados[0]; // primer conjunto del par
            string c2 = marcados[1]; // segundo conjunto del par

            foreach (var r in relaciones)
            {
                // solo los pares que pertenecen a esta combinacion (ej. A con B)
                if (r.Item1 == c1 && r.Item3 == c2)
                    uiListBox2.Items.Add($"{r.Item1}→{r.Item3}: ({r.Item2}, {r.Item4})");
            }
        }

        // lee el texto real del control
        private string LeerTexto(Control control)
        {
            if (control is TextBox tb) return tb.Text; // si ya es un TextBox normal

            foreach (Control hijo in control.Controls) // busca un TextBox dentro del control
            {
                string t = LeerTexto(hijo);
                if (hijo is TextBox || !string.IsNullOrEmpty(t)) return t;
            }
            return control.Text; // si no encuentra ninguno, usa el Text normal
        }

        // precionar enter para agregar los conjuntos
        private void txtElementos_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // evita el sonido de "ding" de Windows
                btnAggElementos_Click(sender, EventArgs.Empty);
            }
        }

        // enlaza el evento al control
        private void EnlazarEnter(Control control)
        {
            control.KeyDown += txtElementos_KeyDown;
            foreach (Control hijo in control.Controls)
                EnlazarEnter(hijo);
        }

        private void btnGenerarRelacion_Click_1(object sender, EventArgs e)
        {

        }


        private List<Relacion> ConstruirRelaciones()
        {
            var lista = new List<Relacion>();

            var claves = relaciones
                .Select(r => r.Item1 + r.Item3)   // ej. "AC"
                .Distinct()
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            foreach (string clave in claves)
            {
                string c1 = clave.Substring(0, 1);
                string c2 = clave.Substring(1, 1);
                if (!conjuntos.ContainsKey(c1) || !conjuntos.ContainsKey(c2)) continue;

                var rel = new Relacion(
                    c1, conjuntos[c1].Select(n => n.ToString()).ToList(),
                    c2, conjuntos[c2].Select(n => n.ToString()).ToList());

                foreach (var r in relaciones)
                    if (r.Item1 == c1 && r.Item3 == c2)
                        rel.Agregar(r.Item2.ToString(), r.Item4.ToString(), out _);

                lista.Add(rel);
            }
            return lista;
        }

        // ---------------------------------------------------------------------
        //  Encuentra los paneles grandes buscando sus títulos
        //  ("Digrafo", "Matriz...", "Propiedad...")
        // ---------------------------------------------------------------------
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

        // CORREGIDO: este metodo faltaba. Devuelve el panel real que contiene el titulo.
        private Control PanelPorTitulo(params string[] posibles)
        {
            foreach (string p in posibles)
            {
                Control c = BuscarPorTexto(this, p);
                if (c != null) return c.Parent;   // el titulo esta dentro del panel real
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
                                "Revise que sus titulos empiecen con esas palabras.");
                return false;
            }
            return true;
        }

        // ---------------------------------------------------------------------
        //  Crea (una sola vez) un panel con scroll + imagen dentro del panel grande
        // ---------------------------------------------------------------------
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

        private RichTextBox TextoEn(Control cont)
        {
            int top = 45;
            var rtb = cont.Controls.OfType<RichTextBox>().FirstOrDefault(p => p.Name == "rtbAuto");
            if (rtb == null)
            {
                rtb = new RichTextBox();
                rtb.Name = "rtbAuto";
                rtb.ReadOnly = true;
                rtb.BorderStyle = BorderStyle.None;
                rtb.BackColor = Color.White;
                rtb.Font = new Font("Segoe UI", 10f);
                rtb.Location = new Point(5, top);
                rtb.Size = new Size(cont.Width - 10, cont.Height - top - 5);
                rtb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                cont.Controls.Add(rtb);
                rtb.BringToFront();
            }
            return rtb;
        }

        private void MostrarImagen(PictureBox pb, Bitmap nueva)
        {
            Image vieja = pb.Image;
            pb.Image = nueva;
            if (vieja != null) vieja.Dispose();
        }

        private void DibujarMatrizYPropiedades(Control pMatriz, Control pProp)
        {
            List<Relacion> lista = ConstruirRelaciones();
            MostrarImagen(ImagenEn(pMatriz), Multi.Matrices(lista));
            TextoEn(pProp).Text = Multi.Propiedades(lista);
        }

        private void DibujarDigrafo(Control pDigrafo)
        {
            List<Relacion> lista = ConstruirRelaciones();
            PictureBox pb = ImagenEn(pDigrafo);
            Size vp = pb.Parent.ClientSize;
            int w = Math.Max(vp.Width - SystemInformation.VerticalScrollBarWidth - 2, 300);
            int h = Math.Max(vp.Height - 40, 280);
            MostrarImagen(pb, Multi.Digrafos(lista, w, h));
        }

        // Genera las tres salidas de una vez
        private void GenerarSalidas()
        {
            if (!ObtenerPaneles(out Control pDigrafo, out Control pMatriz, out Control pProp)) return;
            DibujarMatrizYPropiedades(pMatriz, pProp);
            DibujarDigrafo(pDigrafo);
        }

        // ---------------------------------------------------------------------
        //  BOTÓN: Generar Matriz  (matrices + propiedades de todas las relaciones)
        //  CORREGIDO: estaba duplicado; ahora existe una sola vez
        // ---------------------------------------------------------------------
        private void btnMatriz_Click(object sender, EventArgs e)
        {
            if (relaciones.Count == 0)
            {
                MessageBox.Show("Primero genere una relacion.");
                return;
            }
            if (!ObtenerPaneles(out Control pDigrafo, out Control pMatriz, out Control pProp)) return;
            DibujarMatrizYPropiedades(pMatriz, pProp);
        }

        // ---------------------------------------------------------------------
        //  BOTÓN: Generar Diagrama  (dígrafo de todas las relaciones)
        // ---------------------------------------------------------------------
        private void btnDiagrama_Click(object sender, EventArgs e)
        {
            if (relaciones.Count == 0)
            {
                MessageBox.Show("Primero genere una relacion.");
                return;
            }
            if (!ObtenerPaneles(out Control pDigrafo, out Control pMatriz, out Control pProp)) return;
            DibujarDigrafo(pDigrafo);
        }

        // ---------------------------------------------------------------------
        //  BOTÓN: Exportar imágenes  (un dígrafo y una matriz por relación)
        // ---------------------------------------------------------------------
        private void btnExportar_Click(object sender, EventArgs e)
        {
            if (relaciones.Count == 0)
            {
                MessageBox.Show("Primero genere una relacion.");
                return;
            }

            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Elija la carpeta donde guardar las imagenes";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    foreach (Relacion r in ConstruirRelaciones())
                    {
                        string sufijo = r.NombreDom + r.NombreCod; // ej. AC
                        using (Bitmap d = Dibujo.Digrafo(r, 1200, 900))
                            d.Save(System.IO.Path.Combine(dlg.SelectedPath, "digrafo_" + sufijo + ".png"),
                                   System.Drawing.Imaging.ImageFormat.Png);
                        using (Bitmap m = Dibujo.Matriz(r))
                            m.Save(System.IO.Path.Combine(dlg.SelectedPath, "matriz_" + sufijo + ".png"),
                                   System.Drawing.Imaging.ImageFormat.Png);
                    }
                    MessageBox.Show("Imagenes guardadas en:\n" + dlg.SelectedPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo guardar: " + ex.Message);
                }
            }
        }

        private void uiPanel1_Click_1(object sender, EventArgs e)
        {

        }

        private void uiPanel4_Click(object sender, EventArgs e)
        {

        }
    }
}