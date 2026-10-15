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
    }
}
