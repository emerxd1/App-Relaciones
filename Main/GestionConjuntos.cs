using System.Drawing;

namespace Main
{
    // Administra los conjuntos A, B, C, D y el universo. No toca la pantalla.
    public class GestionConjuntos
    {
        private const int MaxConjuntos = 5;
        private const int MinConjuntos = 1;

        public List<Conjunto> Conjuntos { get; } = new List<Conjunto>();

        // El universo va aparte: no cuenta en el máximo ni en las letras
        public Conjunto Universo { get; } = new Conjunto
        {
            Nombre = "Universo",
            Color = Color.LightGray
        };

        // Crea el siguiente conjunto. Devuelve null si ya se llegó al máximo.
        public Conjunto AddNewSet()
        {
            if (Conjuntos.Count >= MaxConjuntos) return null;

            char letra = (char)('A' + Conjuntos.Count);
            var nuevo = new Conjunto
            {
                Nombre = $"Conjunto {letra}",
                Color = ColorPorDefecto(Conjuntos.Count)
            };

            Conjuntos.Add(nuevo);
            return nuevo;
        }

        // Borra un conjunto y reasigna las letras (A, B, C...).
        // Devuelve false si no se puede (mínimo 2 conjuntos) o no existe.
        public bool DeleteSet(Conjunto conjunto)
        {
            if (Conjuntos.Count <= MinConjuntos) return false;
            if (!Conjuntos.Remove(conjunto)) return false;

            for (int i = 0; i < Conjuntos.Count; i++)
                Conjuntos[i].Nombre = $"Conjunto {(char)('A' + i)}";

            return true;
        }

        // Guarda el color elegido (el ColorDialog se abre en el formulario)
        public void SetColor(Conjunto conjunto, Color color)
        {
            conjunto.Color = color;
        }

        private Color ColorPorDefecto(int indice)
        {
            Color[] colores = { Color.HotPink, Color.MediumOrchid, Color.DeepSkyBlue, Color.Orange };
            return colores[indice % colores.Length];
        }
    }
}