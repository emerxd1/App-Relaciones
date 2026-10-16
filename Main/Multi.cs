using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Main
{
    public static class Multi
    {
        const int AltoTitulo = 32;

        static string Nombre(Relacion r)
        {
            if (r.EsEndo) return "Relación sobre " + r.NombreDom + "  (" + r.NombreDom + " → " + r.NombreDom + ")";
            return "Relación de " + r.NombreDom + " a " + r.NombreCod + "  (" + r.NombreDom + " → " + r.NombreCod + ")";
        }

        static Bitmap Vacio()
        {
            var bmp = new Bitmap(420, 60);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                g.DrawString("Aún no hay relaciones. Usa \"Generar relación\".", SystemFonts.DefaultFont, Brushes.Gray, 8, 8);
            }
            return bmp;
        }

        static Bitmap Apilar(List<Relacion> rels, List<Bitmap> imgs, int anchoMin)
        {
            if (imgs.Count == 0) return Vacio();

            int ancho = anchoMin;
            int alto = 0;
            foreach (var im in imgs)
            {
                ancho = Math.Max(ancho, im.Width);
                alto += AltoTitulo + im.Height + 12;
            }

            var bmp = new Bitmap(ancho, alto);
            using (var g = Graphics.FromImage(bmp))
            using (var fuente = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var fondoTitulo = new SolidBrush(Color.FromArgb(228, 236, 252)))
            {
                g.Clear(Color.White);
                int y = 0;
                for (int k = 0; k < imgs.Count; k++)
                {
                    g.FillRectangle(fondoTitulo, 0, y, ancho, AltoTitulo);
                    g.DrawString(Nombre(rels[k]), fuente, Brushes.Black, 8, y + 6);
                    y += AltoTitulo;
                    g.DrawImage(imgs[k], new Rectangle(0, y, imgs[k].Width, imgs[k].Height));
                    y += imgs[k].Height + 12;
                    imgs[k].Dispose();
                }
            }
            return bmp;
        }

        public static Bitmap Digrafos(List<Relacion> rels, int w, int h)
        {
            var imgs = new List<Bitmap>();
            foreach (var r in rels) imgs.Add(Dibujo.Digrafo(r, w, h));
            return Apilar(rels, imgs, w);
        }

        public static Bitmap Matrices(List<Relacion> rels)
        {
            var imgs = new List<Bitmap>();
            foreach (var r in rels) imgs.Add(Dibujo.Matriz(r));
            return Apilar(rels, imgs, 200);
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