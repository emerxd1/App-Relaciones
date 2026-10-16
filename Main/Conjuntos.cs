

namespace Main
{
    // Molde de un conjunto: guarda sus propios datos, sin tocar la pantalla.
    public class Conjunto
    {
        public string Nombre { get; set; }
        public List<string> Elementos { get; } = new List<string>();
        public Color Color { get; set; } = Color.Gray;

        // Devuelve true si lo agregó; false si está vacío o repetido
        public bool AgregarElemento(string elemento)
        {
            if (string.IsNullOrWhiteSpace(elemento)) return false;

            elemento = elemento.Trim();

            Elementos.Add(elemento);
            return true;
        }

        // Devuelve true si el elemento existía
        public bool EliminarElemento(string elemento)
        {
            return Elementos.Remove(elemento);
        }
    }
}