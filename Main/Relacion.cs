using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
                m[Dominio.IndexOf(p.A), Codominio.IndexOf(p.B)] = 1;
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

    // Dibujo de la matriz y del dígrafo (devuelven un Bitmap)
    public static class Dibujo
    {
        static readonly Color ColorFlecha = Color.FromArgb(40, 40, 80);
        static readonly Color ColorNodo = Color.FromArgb(214, 236, 240);
        static readonly Color ColorBorde = Color.FromArgb(20, 140, 150);

        // ----------------------- MATRIZ -----------------------
        public static Bitmap Matriz(Relacion r)
        {
            int cell = 38, head = 46;
            int filas = r.Dominio.Count, cols = r.Codominio.Count;
            int w = head + Math.Max(cols, 1) * cell + 12;
            int h = head + Math.Max(filas, 1) * cell + 12;
            var m = r.Matriz();

            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            using (var fuente = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (var fuenteN = new Font("Segoe UI", 11f))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var gris = new Pen(Color.Gray))
            using (var relleno = new SolidBrush(Color.FromArgb(200, 220, 250)))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.White);

                g.DrawString("M", fuente, Brushes.DimGray, new RectangleF(4, 4, head - 8, head - 8), sf);

                for (int j = 0; j < cols; j++)
                    g.DrawString(r.Codominio[j], fuente, Brushes.Black,
                        new RectangleF(head + j * cell, 4, cell, head - 8), sf);

                for (int i = 0; i < filas; i++)
                {
                    g.DrawString(r.Dominio[i], fuente, Brushes.Black,
                        new RectangleF(4, head + i * cell, head - 8, cell), sf);
                    for (int j = 0; j < cols; j++)
                    {
                        var rect = new Rectangle(head + j * cell, head + i * cell, cell, cell);
                        if (m[i, j] == 1) g.FillRectangle(relleno, rect);
                        g.DrawRectangle(gris, rect);
                        g.DrawString(m[i, j].ToString(), fuenteN, Brushes.Black, rect, sf);
                    }
                }
            }
            return bmp;
        }

        // ----------------------- DÍGRAFO -----------------------
        public static Bitmap Digrafo(Relacion r, int w, int h)
        {
            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.White);

                if (r.Dominio.Count == 0)
                {
                    g.DrawString("El conjunto no tiene elementos.", SystemFonts.DefaultFont, Brushes.Gray, 10, 10);
                    return bmp;
                }

                if (r.EsEndo) DigrafoEndo(g, r, w, h);
                else DigrafoBipartito(g, r, w, h);
            }
            return bmp;
        }

        static PointF Mover(PointF desde, PointF hacia, float dist)
        {
            float dx = hacia.X - desde.X, dy = hacia.Y - desde.Y;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < 0.001f) return desde;
            return new PointF(desde.X + dx / d * dist, desde.Y + dy / d * dist);
        }

        static void Nodo(Graphics g, PointF c, float nr, string texto)
        {
            using (var fondo = new SolidBrush(ColorNodo))
            using (var borde = new Pen(ColorBorde, 2.2f))
            using (var fuente = new Font("Segoe UI", nr >= 20 ? 11f : 9f, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.FillEllipse(fondo, c.X - nr, c.Y - nr, 2 * nr, 2 * nr);
                g.DrawEllipse(borde, c.X - nr, c.Y - nr, 2 * nr, 2 * nr);
                g.DrawString(texto, fuente, Brushes.Black, new RectangleF(c.X - nr, c.Y - nr, 2 * nr, 2 * nr), sf);
            }
        }

        // Relación de un conjunto sobre sí mismo: nodos en círculo, lazos y flechas
        static void DigrafoEndo(Graphics g, Relacion r, int w, int h)
        {
            int n = r.Dominio.Count;
            float cx = w / 2f, cy = h / 2f;
            float R = Math.Min(w, h) / 2f - 70f;
            if (R < 40f) R = 40f;
            float nr = n <= 1 ? 24f : Math.Max(14f, Math.Min(26f, (float)(Math.PI * R / n * 0.7)));

            var pts = new PointF[n];
            for (int i = 0; i < n; i++)
            {
                if (n == 1) { pts[i] = new PointF(cx, cy); continue; }
                double ang = -Math.PI / 2 + 2 * Math.PI * i / n;
                pts[i] = new PointF(cx + R * (float)Math.Cos(ang), cy + R * (float)Math.Sin(ang));
            }

            using (var pen = new Pen(ColorFlecha, 1.8f))
            {
                pen.CustomEndCap = new AdjustableArrowCap(5, 6);

                foreach (var p in r.Pares)
                {
                    int i = r.Dominio.IndexOf(p.A), j = r.Dominio.IndexOf(p.B);
                    if (i < 0 || j < 0) continue;

                    if (i == j)
                    {
                        // LAZO: arco hacia afuera del círculo
                        PointF P = pts[i];
                        float dx, dy;
                        if (n == 1) { dx = 0; dy = -1; }
                        else
                        {
                            dx = P.X - cx; dy = P.Y - cy;
                            float L = (float)Math.Sqrt(dx * dx + dy * dy);
                            dx /= L; dy /= L;
                        }
                        float lr = nr * 0.75f;
                        var C = new PointF(P.X + dx * 1.45f * nr, P.Y + dy * 1.45f * nr);
                        float th0 = (float)(Math.Atan2(P.Y - C.Y, P.X - C.X) * 180.0 / Math.PI);
                        g.DrawArc(pen, C.X - lr, C.Y - lr, 2 * lr, 2 * lr, th0 + 40f, 280f);
                    }
                    else
                    {
                        PointF A = pts[i], B = pts[j];
                        float ex = B.X - A.X, ey = B.Y - A.Y;
                        float d = (float)Math.Sqrt(ex * ex + ey * ey);
                        float ux = ex / d, uy = ey / d;

                        if (r.Contiene(p.B, p.A))
                        {
                            // IDA Y VUELTA: dos flechas curvas separadas
                            float off = Math.Min(35f, d * 0.25f);
                            var M = new PointF((A.X + B.X) / 2f - uy * off, (A.Y + B.Y) / 2f + ux * off);
                            PointF s = Mover(A, M, nr), e = Mover(B, M, nr);
                            g.DrawCurve(pen, new PointF[] { s, M, e }, 0.5f);
                        }
                        else
                        {
                            var s = new PointF(A.X + ux * nr, A.Y + uy * nr);
                            var e = new PointF(B.X - ux * nr, B.Y - uy * nr);
                            g.DrawLine(pen, s, e);
                        }
                    }
                }
            }

            for (int i = 0; i < n; i++) Nodo(g, pts[i], nr, r.Dominio[i]);
        }

        // Relación entre dos conjuntos distintos: A a la izquierda, B a la derecha
        static void DigrafoBipartito(Graphics g, Relacion r, int w, int h)
        {
            int na = r.Dominio.Count, nb = r.Codominio.Count;
            float xl = w * 0.25f, xr = w * 0.75f;
            float nr = 20f;
            var pa = new PointF[na];
            var pb = new PointF[nb];
            for (int i = 0; i < na; i++) pa[i] = new PointF(xl, (i + 1) * h / (float)(na + 1));
            for (int j = 0; j < nb; j++) pb[j] = new PointF(xr, (j + 1) * h / (float)(nb + 1));

            using (var pen = new Pen(ColorFlecha, 1.8f))
            using (var titulo = new Font("Segoe UI", 12f, FontStyle.Bold))
            {
                pen.CustomEndCap = new AdjustableArrowCap(5, 6);
                g.DrawString("Conjunto " + r.NombreDom, titulo, Brushes.DimGray, xl - 50, 6);
                g.DrawString("Conjunto " + r.NombreCod, titulo, Brushes.DimGray, xr - 50, 6);

                foreach (var p in r.Pares)
                {
                    int i = r.Dominio.IndexOf(p.A), j = r.Codominio.IndexOf(p.B);
                    if (i < 0 || j < 0) continue;
                    PointF s = Mover(pa[i], pb[j], nr);
                    PointF e = Mover(pb[j], pa[i], nr);
                    g.DrawLine(pen, s, e);
                }
            }

            for (int i = 0; i < na; i++) Nodo(g, pa[i], nr, r.Dominio[i]);
            for (int j = 0; j < nb; j++) Nodo(g, pb[j], nr, r.Codominio[j]);
        }
    }
}