namespace Main
{
    // Administra los conjuntos. No toca la pantalla.
    public class GestionConjuntos
    {
        private const int MaxConjuntos = 5;
        private const int MinConjuntos = 1;

        public List<Conjunto> Conjuntos { get; } = new List<Conjunto>();

        public bool Lleno => Conjuntos.Count >= MaxConjuntos;

        public bool Existe(string nombre) =>
            Conjuntos.Any(c => string.Equals(c.Nombre, nombre, StringComparison.OrdinalIgnoreCase));

        // Primer numero (1, 2, 3...) que no este usado como nombre
        public string NombreLibre()
        {
            for (int i = 1; ; i++)
            {
                string n = i.ToString();
                if (!Existe(n)) return n;
            }
        }

        // Crea un conjunto. Si no se da nombre usa el siguiente numero libre.
        // Devuelve null si ya se llego al maximo, o si el nombre es invalido o repetido.
        public Conjunto AddNewSet(string nombre = null)
        {
            if (Lleno) return null;

            nombre = string.IsNullOrWhiteSpace(nombre) ? NombreLibre() : nombre.Trim();
            if (!Conjunto.EsValido(nombre) || Existe(nombre)) return null;

            var nuevo = new Conjunto { Nombre = nombre };
            Conjuntos.Add(nuevo);
            return nuevo;
        }

        // Borra un conjunto (los demas conservan su nombre).
        // Devuelve false si no se puede (minimo 1 conjunto) o no existe.
        public bool DeleteSet(Conjunto conjunto)
        {
            if (Conjuntos.Count <= MinConjuntos) return false;
            return Conjuntos.Remove(conjunto);
        }
    }
}