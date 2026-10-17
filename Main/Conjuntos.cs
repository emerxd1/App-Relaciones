namespace Main
{
    // Molde de un conjunto: guarda sus propios datos, sin tocar la pantalla.
    public class Conjunto
    {
        // Nombre corto: puede llevar letras y numeros ("1", "A", "x2"...)
        public string Nombre { get; set; }

        // Texto que se muestra en pantalla
        public string Titulo => "Conjunto " + Nombre;

        public List<string> Elementos { get; } = new List<string>();

        // Solo letras y numeros (sin espacios ni caracteres especiales)
        public static bool EsValido(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return false;
            foreach (char c in texto)
                if (!char.IsLetterOrDigit(c)) return false;
            return true;
        }

        // Devuelve true si lo agrego; false si esta vacio, repetido o tiene caracteres especiales
        public bool AgregarElemento(string elemento)
        {
            if (string.IsNullOrWhiteSpace(elemento)) return false;

            elemento = elemento.Trim();

            if (!EsValido(elemento)) return false;
            if (Elementos.Contains(elemento)) return false;

            Elementos.Add(elemento);
            return true;
        }

        // Devuelve true si el elemento existia
        public bool EliminarElemento(string elemento)
        {
            return Elementos.Remove(elemento);
        }
    }
}