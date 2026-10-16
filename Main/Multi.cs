using System;
using System.Collections.Generic;
using System.Text;

namespace Main
{
    // Texto del panel "Propiedades".
    // (El dibujo del digrafo y de la matriz ahora lo hace la clase Grafica.)
    public static class Multi
    {
        static string Nombre(Relacion r)
        {
            if (r.EsEndo) return "Relación sobre " + r.NombreDom + "  (" + r.NombreDom + " → " + r.NombreDom + ")";
            return "Relación de " + r.NombreDom + " a " + r.NombreCod + "  (" + r.NombreDom + " → " + r.NombreCod + ")";
        }

        public static string Propiedades(List<Relacion> rels)
        {
            if (rels.Count == 0) return "Aún no hay relaciones.";

            var sb = new StringBuilder();
            foreach (var r in rels)
            {
                sb.AppendLine("------ " + Nombre(r) + " ------");
                sb.AppendLine();
                if (r.EsEndo)
                    sb.AppendLine(Analisis.Resumen(r));
                else
                    sb.AppendLine("No aplica: las propiedades solo se estudian en una relación de un conjunto consigo mismo (" + r.NombreDom + " con " + r.NombreDom + ").");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}