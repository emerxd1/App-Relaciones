using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Main
{
    // Un par (a, b) de la relación
    public class Par
    {
        public string A { get; set; }
        public string B { get; set; }
        public Par(string a, string b) { A = a; B = b; }
    }

    // Relación R entre un conjunto "dominio" y un conjunto "codominio".
    // Si ambos son el mismo conjunto (A con A) se analizan propiedades.
    public class Relacion
    {
        public string NombreDom { get; private set; }
        public string NombreCod { get; private set; }
        public List<string> Dominio { get; private set; }
        public List<string> Codominio { get; private set; }
        public List<Par> Pares { get; private set; }

        public bool EsEndo { get { return NombreDom == NombreCod; } }

        public Relacion(string nombreDom, List<string> dom, string nombreCod, List<string> cod)
        {
            NombreDom = nombreDom;
            NombreCod = nombreCod;
            Dominio = new List<string>(dom);
            Codominio = new List<string>(cod);
            Pares = new List<Par>();
        }

        public void ActualizarConjuntos(List<string> dom, List<string> cod)
        {
            Dominio = new List<string>(dom);
            Codominio = new List<string>(cod);
            Pares.RemoveAll(p => !Dominio.Contains(p.A) || !Codominio.Contains(p.B));
        }

        public bool Contiene(string a, string b)
        {
            return Pares.Any(p => p.A == a && p.B == b);
        }

        public bool Agregar(string a, string b, out string error)
        {
            error = "";
            a = a.Trim(); b = b.Trim();
            if (!Dominio.Contains(a))
            {
                error = "El elemento \"" + a + "\" no está en el conjunto " + NombreDom + ".";
                return false;
            }
            if (!Codominio.Contains(b))
            {
                error = "El elemento \"" + b + "\" no está en el conjunto " + NombreCod + ".";
                return false;
            }
            if (!Contiene(a, b)) Pares.Add(new Par(a, b));
            return true;
        }

        public int[,] Matriz()
        {
            var m = new int[Dominio.Count, Codominio.Count];
            foreach (var p in Pares)
            {
                int i = Dominio.IndexOf(p.A);
                int j = Codominio.IndexOf(p.B);
                if (i >= 0 && j >= 0) m[i, j] = 1;
            }
            return m;
        }
    }

    // Propiedades (con contraejemplo)
    public static class Analisis
    {
        public class Res
        {
            public bool Cumple;
            public string Detalle = "";
        }

        public static Res Reflexiva(Relacion r)
        {
            var m = r.Matriz();
            for (int i = 0; i < r.Dominio.Count; i++)
                if (m[i, i] == 0)
                    return new Res { Cumple = false, Detalle = "falta (" + r.Dominio[i] + "," + r.Dominio[i] + ")" };
            return new Res { Cumple = true, Detalle = "la diagonal es toda 1" };
        }

        public static Res Simetrica(Relacion r)
        {
            var m = r.Matriz(); int n = r.Dominio.Count;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (m[i, j] == 1 && m[j, i] == 0)
                        return new Res { Cumple = false, Detalle = "(" + r.Dominio[i] + "," + r.Dominio[j] + ") está en R pero (" + r.Dominio[j] + "," + r.Dominio[i] + ") no" };
            return new Res { Cumple = true, Detalle = "M es igual a su transpuesta" };
        }

        public static Res Antisimetrica(Relacion r)
        {
            var m = r.Matriz(); int n = r.Dominio.Count;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (m[i, j] == 1 && m[j, i] == 1)
                        return new Res { Cumple = false, Detalle = "(" + r.Dominio[i] + "," + r.Dominio[j] + ") y (" + r.Dominio[j] + "," + r.Dominio[i] + ") están en R con " + r.Dominio[i] + " ≠ " + r.Dominio[j] };
            return new Res { Cumple = true, Detalle = "no hay ida y vuelta entre elementos distintos" };
        }

        public static Res Transitiva(Relacion r)
        {
            var m = r.Matriz(); int n = r.Dominio.Count;
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    if (m[i, j] == 0) continue;
                    for (int k = 0; k < n; k++)
                        if (m[j, k] == 1 && m[i, k] == 0)
                            return new Res { Cumple = false, Detalle = "(" + r.Dominio[i] + "," + r.Dominio[j] + ") y (" + r.Dominio[j] + "," + r.Dominio[k] + ") están en R pero falta (" + r.Dominio[i] + "," + r.Dominio[k] + ")" };
                }
            return new Res { Cumple = true, Detalle = "todo camino a→b→c tiene su atajo a→c" };
        }

        static bool EsTotal(Relacion r)
        {
            var m = r.Matriz(); int n = r.Dominio.Count;
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (m[i, j] == 0 && m[j, i] == 0) return false;
            return true;
        }

        static string Clases(Relacion r)
        {
            var m = r.Matriz(); int n = r.Dominio.Count;
            var visto = new bool[n];
            var partes = new List<string>();
            for (int i = 0; i < n; i++)
            {
                if (visto[i]) continue;
                var miembros = new List<string>();
                for (int j = 0; j < n; j++)
                    if (m[i, j] == 1) { miembros.Add(r.Dominio[j]); visto[j] = true; }
                partes.Add("{" + string.Join(",", miembros) + "}");
            }
            return string.Join(", ", partes);
        }

        static void Linea(StringBuilder sb, string nombre, Res res)
        {
            sb.AppendLine(nombre + ": " + (res.Cumple ? "Sí" : "No"));
            sb.AppendLine("    " + res.Detalle);
            sb.AppendLine();
        }

        public static string Resumen(Relacion r)
        {
            if (!r.EsEndo)
                return "Las propiedades solo se analizan cuando la relación es de un conjunto consigo mismo (A con A).\r\n\r\nMarca únicamente un conjunto para probarlas.";

            if (r.Dominio.Count == 0)
                return "El conjunto no tiene elementos.";

            Res re = Reflexiva(r), si = Simetrica(r), an = Antisimetrica(r), tr = Transitiva(r);
            var sb = new StringBuilder();
            Linea(sb, "Reflexiva", re);
            Linea(sb, "Simétrica", si);
            Linea(sb, "Antisimétrica", an);
            Linea(sb, "Transitiva", tr);

            string tipo;
            if (re.Cumple && si.Cumple && tr.Cumple)
                tipo = "Equivalencia\r\n    Clases: " + Clases(r);
            else if (re.Cumple && an.Cumple && tr.Cumple)
                tipo = EsTotal(r) ? "Orden parcial (además es orden total)" : "Orden parcial";
            else
                tipo = "Ninguno (ni equivalencia ni orden parcial)";
            sb.AppendLine("Tipo de relación: " + tipo);
            return sb.ToString();
        }
    }
}