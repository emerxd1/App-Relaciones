using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Main
{
    // Un conjunto que participa en el dibujo
    public class GrupoGrafica
    {
        public string Clave { get; set; }        // nombre real del conjunto (enlaza con los pares)
        public string Etiqueta { get; set; }     // nombre que se muestra
        public List<string> Elementos { get; set; } = new();
    }

    // Dibuja UN solo digrafo y UNA sola matriz con todos los conjuntos y relaciones
    public static class Grafica
    {
        // Paleta: un color por conjunto (se usa en la matriz de adyacencia)
        private static readonly Color[] Paleta =
        {
            Color.FromArgb(59, 91, 219),    // indigo
            Color.FromArgb(18, 184, 134),   // verde azulado
            Color.FromArgb(253, 126, 20),   // naranja
            Color.FromArgb(230, 73, 128),   // rosa
            Color.FromArgb(132, 94, 247),   // violeta
            Color.FromArgb(34, 139, 230)    // azul
        };

        private static Color ColorGrupo(int i) => Paleta[i % Paleta.Length];

        private static readonly Color CeldaVacia = Color.FromArgb(243, 244, 246);
        private static readonly Color TextoGris = Color.FromArgb(173, 181, 189);

        // Estilo del digrafo
        private static readonly Color NodoRelleno = Color.FromArgb(217, 238, 242);
        private static readonly Color NodoBorde = Color.FromArgb(13, 130, 143);
        private static readonly Color Tinta = Color.FromArgb(30, 32, 52);

        // ------------------------------------------------------------------
        //  Utilidades
        // ------------------------------------------------------------------
        private static void Configurar(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.White);
        }

        private static GraphicsPath Redondeado(RectangleF r, float radio)
        {
            radio = Math.Min(radio, Math.Min(r.Width, r.Height) / 2f);
            float d = radio * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private static StringFormat Centro() => new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        private static PointF Unitario(float dx, float dy)
        {
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            return d < 1e-6f ? new PointF(1, 0) : new PointF(dx / d, dy / d);
        }

        // ------------------------------------------------------------------
        //  DIGRAFO
        //  - Un solo conjunto: un vertice por cada elemento y un arco a -> b por cada par (a, b).
        //  - Varios conjuntos: un vertice por conjunto y un arco X -> Y si hay alguna relacion de X hacia Y.
        // ------------------------------------------------------------------
        public static Bitmap Digrafo(IReadOnlyList<GrupoGrafica> grupos,
                                     IReadOnlyList<(string, string, string, string)> pares,
                                     int ancho, int alto, float zoom = 1f)
        {
            var vertices = new List<(string clave, string txt)>();
            var arcosBrutos = new List<(string, string)>();

            if (grupos.Count == 1)
            {
                GrupoGrafica gr = grupos[0];
                foreach (string el in gr.Elementos)
                    vertices.Add((el, el));
                foreach (var par in pares)
                    if (par.Item1 == gr.Clave && par.Item3 == gr.Clave)
                        arcosBrutos.Add((par.Item2, par.Item4));
            }
            else
            {
                foreach (GrupoGrafica gr in grupos)
                    vertices.Add((gr.Clave, gr.Etiqueta));
                foreach (var par in pares)
                    arcosBrutos.Add((par.Item1, par.Item3));
            }

            int k = vertices.Count;
            if (k == 0) return new Bitmap(Math.Max(ancho, 1), Math.Max(alto, 1));

            int anchoU = (int)(ancho / zoom);
            int altoU = (int)(alto / zoom);

            // el vertice crece si el texto es largo
            int maxLen = vertices.Max(v => v.txt.Length);
            float nodoR = Math.Min(60f, Math.Max(26f, 14f + maxLen * 8f));
            float fuente = Math.Max(8f, Math.Min(15f, (2f * nodoR - 10f) / (maxLen * 0.85f)));

            // los vertices se reparten en un circulo (2 vertices: izquierda y derecha)
            float anillo = k == 1 ? 0f
                : Math.Max(110f, k * (nodoR * 2 + 70f) / (2f * (float)Math.PI));

            const float margen = 36f;
            float extension = anillo + nodoR * 3.5f + margen;   // deja sitio a los lazos
            int w = Math.Max(anchoU, (int)Math.Ceiling(extension * 2));
            int h = Math.Max(altoU, (int)Math.Ceiling(extension * 2));
            var centroGrafo = new PointF(w / 2f, h / 2f);

            var pos = new Dictionary<string, PointF>();
            var nodos = new List<(PointF p, string txt)>();
            double inicio = k == 2 ? Math.PI : -Math.PI / 2;
            for (int i = 0; i < k; i++)
            {
                PointF p;
                if (k == 1) p = centroGrafo;
                else
                {
                    double a = inicio + 2 * Math.PI * i / k;
                    p = new PointF(centroGrafo.X + anillo * (float)Math.Cos(a),
                                   centroGrafo.Y + anillo * (float)Math.Sin(a));
                }
                pos[vertices[i].clave] = p;
                nodos.Add((p, vertices[i].txt));
            }

            var bmp = new Bitmap((int)(w * zoom), (int)(h * zoom));
            using var g = Graphics.FromImage(bmp);
            Configurar(g);
            g.ScaleTransform(zoom, zoom);
            using var centro = Centro();

            // arcos sin repetir
            var arcos = new HashSet<(string, string)>();
            foreach (var a in arcosBrutos)
                if (pos.ContainsKey(a.Item1) && pos.ContainsKey(a.Item2))
                    arcos.Add(a);

            // 1) flechas y lazos
            foreach (var (c1, c2) in arcos)
            {
                PointF p1 = pos[c1], p2 = pos[c2];
                if (c1 == c2)
                    DibujarLazo(g, p1, centroGrafo, nodoR, Tinta);   // lazo (x, x): sale hacia afuera
                else
                {
                    bool idaYVuelta = arcos.Contains((c2, c1));
                    var otros = nodos.Select(n => n.p).Where(q => q != p1 && q != p2);
                    DibujarFlecha(g, p1, p2, nodoR, Tinta, idaYVuelta, otros);
                }
            }

            // 2) vertices encima de las flechas
            using var f = new Font("Segoe UI", fuente, FontStyle.Bold);
            using var bTexto = new SolidBrush(Tinta);
            foreach (var (p, txt) in nodos)
            {
                using (var br = new SolidBrush(NodoRelleno))
                    g.FillEllipse(br, p.X - nodoR, p.Y - nodoR, nodoR * 2, nodoR * 2);
                using (var pen = new Pen(NodoBorde, 3f))
                    g.DrawEllipse(pen, p.X - nodoR, p.Y - nodoR, nodoR * 2, nodoR * 2);
                g.DrawString(txt, f, bTexto,
                    new RectangleF(p.X - nodoR, p.Y - nodoR, nodoR * 2, nodoR * 2), centro);
            }

            return bmp;
        }

        // Flecha recta; se curva si hay ida y vuelta o si taparia a otro nodo
        private static void DibujarFlecha(Graphics g, PointF p1, PointF p2, float nodoR, Color color,
                                          bool idaYVuelta, IEnumerable<PointF> otros)
        {
            float dx = p2.X - p1.X, dy = p2.Y - p1.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1f) return;

            float nx = -dy / dist, ny = dx / dist;   // normal de la flecha
            float curva = 0f;

            if (idaYVuelta)
                curva = Math.Min(dist * 0.22f, 45f);   // cada sentido usa su propia normal => lados opuestos
            else
            {
                // si la recta pasa sobre otro nodo, se curva para esquivarlo
                foreach (var q in otros)
                {
                    float t = ((q.X - p1.X) * dx + (q.Y - p1.Y) * dy) / (dist * dist);
                    if (t < 0f || t > 1f) continue;
                    float cx = p1.X + t * dx, cy = p1.Y + t * dy;
                    float d = (float)Math.Sqrt((q.X - cx) * (q.X - cx) + (q.Y - cy) * (q.Y - cy));
                    if (d < nodoR + 8f)
                    {
                        float lado = (q.X - p1.X) * nx + (q.Y - p1.Y) * ny;   // de que lado esta el estorbo
                        curva = lado > 0 ? -40f : 40f;
                        break;
                    }
                }
            }

            var mid = new PointF((p1.X + p2.X) / 2f, (p1.Y + p2.Y) / 2f);
            var ctrl = new PointF(mid.X + nx * curva * 2f, mid.Y + ny * curva * 2f);

            PointF u0 = Unitario(ctrl.X - p1.X, ctrl.Y - p1.Y);
            PointF u1 = Unitario(p2.X - ctrl.X, p2.Y - ctrl.Y);

            var ini = new PointF(p1.X + u0.X * (nodoR + 1f), p1.Y + u0.Y * (nodoR + 1f));
            var punta = new PointF(p2.X - u1.X * (nodoR + 2f), p2.Y - u1.Y * (nodoR + 2f));
            var fin = new PointF(punta.X - u1.X * 10f, punta.Y - u1.Y * 10f);

            // cuadratica -> cubica
            var c1 = new PointF(ini.X + 2f / 3f * (ctrl.X - ini.X), ini.Y + 2f / 3f * (ctrl.Y - ini.Y));
            var c2 = new PointF(fin.X + 2f / 3f * (ctrl.X - fin.X), fin.Y + 2f / 3f * (ctrl.Y - fin.Y));

            using (var pen = new Pen(color, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawBezier(pen, ini, c1, c2, fin);
            }
            PuntaFlecha(g, punta, u1, color);
        }

        // Punta triangular: "punta" es el vertice y "dir" la direccion en la que apunta
        private static void PuntaFlecha(Graphics g, PointF punta, PointF dir, Color color)
        {
            var baseC = new PointF(punta.X - dir.X * 13f, punta.Y - dir.Y * 13f);
            var perp = new PointF(-dir.Y, dir.X);
            var tri = new[]
            {
                punta,
                new PointF(baseC.X + perp.X * 6f, baseC.Y + perp.Y * 6f),
                new PointF(baseC.X - perp.X * 6f, baseC.Y - perp.Y * 6f)
            };
            using (var br = new SolidBrush(color))
                g.FillPolygon(br, tri);
        }

        // Lazo (a, a): arco que sale del nodo hacia afuera del grafo
        private static void DibujarLazo(Graphics g, PointF p, PointF centroGrupo, float nodoR, Color color)
        {
            PointF u = Unitario(p.X - centroGrupo.X, p.Y - centroGrupo.Y);
            if (Math.Abs(p.X - centroGrupo.X) < 0.5f && Math.Abs(p.Y - centroGrupo.Y) < 0.5f)
                u = new PointF(0, -1);   // nodo en el centro: el lazo sube
            double a = Math.Atan2(u.Y, u.X);

            PointF En(double ang, float dist) =>
                new PointF(p.X + (float)Math.Cos(ang) * dist, p.Y + (float)Math.Sin(ang) * dist);

            PointF ini = En(a - 0.55, nodoR + 1f);
            PointF fin = En(a + 0.55, nodoR + 3f);
            PointF c1 = En(a - 0.95, nodoR * 3.3f);
            PointF c2 = En(a + 0.95, nodoR * 3.3f);

            PointF uf = Unitario(fin.X - c2.X, fin.Y - c2.Y);
            var finLinea = new PointF(fin.X - uf.X * 11f, fin.Y - uf.Y * 11f);

            // el lazo llega en la direccion de la punta
            var cc2 = new PointF(finLinea.X - uf.X * nodoR * 1.5f, finLinea.Y - uf.Y * nodoR * 1.5f);

            using (var pen = new Pen(color, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                g.DrawBezier(pen, ini, c1, cc2, finLinea);
            }
            PuntaFlecha(g, fin, uf, color);
        }

        // ------------------------------------------------------------------
        //  MATRIZ DE ADYACENCIA: UNA sola matriz N x N con todos los elementos
        //  de todos los conjuntos (filas = "de", columnas = "a").
        //  Con un solo conjunto las etiquetas son solo el elemento; con varios,
        //  llevan el prefijo del conjunto (A·1) y el color del conjunto.
        // ------------------------------------------------------------------
        public static Bitmap Matriz(IReadOnlyList<GrupoGrafica> grupos,
                                    IReadOnlyList<(string, string, string, string)> pares,
                                    int ancho = 0, int alto = 0)   // tamaño del panel
        {
            bool unico = grupos.Count == 1;

            var items = new List<(int g, string clave, string etiqueta)>();
            for (int gi = 0; gi < grupos.Count; gi++)
                foreach (string el in grupos[gi].Elementos)
                    items.Add((gi, grupos[gi].Clave + "|" + el,
                               unico ? el : grupos[gi].Etiqueta + "·" + el));

            int N = items.Count;
            if (N == 0) return new Bitmap(1, 1);

            var ady = new HashSet<(string, string)>();
            foreach (var p in pares)
                ady.Add((p.Item1 + "|" + p.Item2, p.Item3 + "|" + p.Item4));

            int maxEtiqueta = items.Max(it => it.etiqueta.Length);
            int cell = Math.Max(40, 12 + maxEtiqueta * 8);   // tamaño de cada celda
            const int margen = 20;
            int filaW = cell + 14;                           // ancho de las etiquetas de fila
            const int colH = 28;                             // alto de las etiquetas de columna

            // si se conoce el panel, la celda crece para llenarlo (hasta 110 px)
            if (ancho > 0 && alto > 0)
            {
                int porAncho = (ancho - 2 * margen - 22) / (N + 1);
                int porAlto = (alto - 2 * margen - colH - 8) / N;
                cell = Math.Max(cell, Math.Min(110, Math.Min(porAncho, porAlto)));
                filaW = cell + 14;
            }
            float esc = Math.Max(1f, cell / 44f);   // escala de las letras

            int x0 = margen + filaW + 8;
            int y0 = margen + colH + 8;

            int w = x0 + N * cell + margen;
            int h = y0 + N * cell + margen;

            var bmp = new Bitmap(w, h);
            using var g = Graphics.FromImage(bmp);
            Configurar(g);

            using var fBold = new Font("Segoe UI", 11f * esc, FontStyle.Bold);
            using var fNormal = new Font("Segoe UI", 10f * esc);
            using var fEtiqueta = new Font("Segoe UI", 9f * Math.Min(esc, 1.4f), FontStyle.Bold);
            using var fEsquina = new Font("Segoe UI", 8f);
            using var brGris = new SolidBrush(TextoGris);
            using var centro = Centro();

            // esquina: indica como se lee la matriz
            g.DrawString("de ↓   a →", fEsquina, brGris,
                new RectangleF(margen, margen, filaW, colH), centro);

            // etiquetas de columnas (arriba) y filas (izquierda)
            for (int i = 0; i < N; i++)
            {
                Color col = ColorGrupo(items[i].g);

                var rc = new RectangleF(x0 + i * cell + 2, margen, cell - 4, colH);
                using (var path = Redondeado(rc, 9))
                using (var br = new SolidBrush(col))
                    g.FillPath(br, path);
                g.DrawString(items[i].etiqueta, fEtiqueta, Brushes.White, rc, centro);

                var rf = new RectangleF(margen, y0 + i * cell + 4, filaW, cell - 8);
                using (var path = Redondeado(rf, 9))
                using (var br = new SolidBrush(col))
                    g.FillPath(br, path);
                g.DrawString(items[i].etiqueta, fEtiqueta, Brushes.White, rf, centro);
            }

            // celdas
            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    bool v = ady.Contains((items[r].clave, items[c].clave));
                    var rect = new RectangleF(x0 + c * cell + 2, y0 + r * cell + 2, cell - 4, cell - 4);

                    using (var path = Redondeado(rect, 8))
                    using (var br = new SolidBrush(v ? ColorGrupo(items[r].g) : CeldaVacia))
                        g.FillPath(br, path);

                    if (v)
                        g.DrawString("1", fBold, Brushes.White, rect, centro);
                    else
                        g.DrawString("0", fNormal, brGris, rect, centro);
                }
            }

            return bmp;
        }

        // ------------------------------------------------------------------
        //  PROPIEDADES: tarjetas con Si / No + definicion y el porque
        //  (solo reflexiva, simetrica, antisimetrica y transitiva)
        // ------------------------------------------------------------------
        public static Bitmap Propiedades(List<Relacion> rels, int ancho)
        {
            ancho = Math.Max(ancho, 260);
            const int cardH = 116, gap = 6, titH = 30, bloqueGap = 12, notaH = 48;
            int columnas = ancho >= 560 ? 2 : 1;

            if (rels.Count == 0)
            {
                var vacio = new Bitmap(ancho, 60);
                using var gv = Graphics.FromImage(vacio);
                Configurar(gv);
                using var fv = new Font("Segoe UI", 13f, GraphicsUnit.Pixel);
                gv.DrawString("Aún no hay relaciones.", fv, Brushes.Gray, 8, 8);
                return vacio;
            }

            int alto = 8;
            foreach (var r in rels)
                alto += titH + (r.EsEndo ? ((4 + columnas - 1) / columnas) * (cardH + gap) : notaH) + bloqueGap;

            var bmp = new Bitmap(ancho, alto);
            using var g = Graphics.FromImage(bmp);
            Configurar(g);

            using var fTit = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fNom = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fDesc = new Font("Segoe UI", 12f, GraphicsUnit.Pixel);
            using var fPil = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var fNota = new Font("Segoe UI", 12f, GraphicsUnit.Pixel);
            using var bTit = new SolidBrush(Color.FromArgb(30, 34, 50));
            using var bNom = new SolidBrush(Color.FromArgb(60, 66, 85));
            using var bDesc = new SolidBrush(Color.FromArgb(100, 108, 125));
            using var bCard = new SolidBrush(Color.FromArgb(248, 249, 252));
            using var pCard = new Pen(Color.FromArgb(225, 229, 238));
            using var centro = Centro();

            int y = 8, margen = 8;
            int cw = (ancho - 2 * margen - (columnas - 1) * gap) / columnas;

            foreach (var r in rels)
            {
                string titulo = r.EsEndo
                    ? (rels.Count > 1 ? "Propiedades de R sobre " + r.NombreDom : "Propiedades de R")
                    : "R: " + r.NombreDom + " → " + r.NombreCod;
                g.DrawString(titulo, fTit, bTit, margen, y);
                y += titH;

                if (!r.EsEndo)
                {
                    g.DrawString("No aplica: solo se estudian en un conjunto consigo mismo.", fNota, Brushes.Gray,
                        new RectangleF(margen, y, ancho - 2 * margen, notaH));
                    y += notaH + bloqueGap;
                    continue;
                }

                // cada propiedad trae su resultado y el porque (Detalle: el contraejemplo o la razon)
                var props = new (string nombre, Analisis.Res res, string desc)[]
                {
                    ("Reflexiva", Analisis.Reflexiva(r), "Todo vértice tiene un lazo: (a, a) ∈ R."),
                    ("Simétrica", Analisis.Simetrica(r), "Cada flecha a→b tiene su regreso b→a."),
                    ("Antisimétrica", Analisis.Antisimetrica(r), "Nunca hay ida y vuelta entre vértices distintos."),
                    ("Transitiva", Analisis.Transitiva(r), "Si hay a→b y b→c, entonces existe a→c.")
                };

                for (int i = 0; i < props.Length; i++)
                {
                    int x = margen + (i % columnas) * (cw + gap);
                    int yy = y + (i / columnas) * (cardH + gap);

                    using (var path = Redondeado(new RectangleF(x, yy, cw, cardH), 8))
                    {
                        g.FillPath(bCard, path);
                        g.DrawPath(pCard, path);
                    }

                    g.DrawString(props[i].nombre, fNom, bNom, x + 12, yy + 11);
                    Pildora(g, fPil, centro, x + cw - 64, yy + 8, props[i].res.Cumple);

                    // fila 2: descripcion de la propiedad
                    g.DrawString(props[i].desc, fDesc, bDesc,
                        new RectangleF(x + 12, yy + 36, cw - 24, 34));

                    // fila 3: el porque (razon o contraejemplo)
                    g.DrawLine(pCard, x + 12, yy + 72, x + cw - 12, yy + 72);
                    Color tintaRaz = props[i].res.Cumple ? Color.FromArgb(18, 120, 70) : Color.FromArgb(160, 60, 60);
                    using (var bRaz = new SolidBrush(tintaRaz))
                        g.DrawString("Porque " + props[i].res.Detalle, fDesc, bRaz,
                            new RectangleF(x + 12, yy + 76, cw - 24, cardH - 80));
                }
                y += ((props.Length + columnas - 1) / columnas) * (cardH + gap) + bloqueGap;
            }
            return bmp;
        }

        private static void Pildora(Graphics g, Font f, StringFormat centro, float x, float y, bool si)
        {
            const float w = 54, h = 24;
            Color fondo = si ? Color.FromArgb(214, 245, 228) : Color.FromArgb(253, 226, 226);
            Color tinta = si ? Color.FromArgb(18, 140, 80) : Color.FromArgb(200, 50, 50);

            using (var path = Redondeado(new RectangleF(x, y, w, h), h / 2f))
            using (var b = new SolidBrush(fondo))
                g.FillPath(b, path);

            float cy = y + h / 2f;
            using (var pen = new Pen(tinta, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                if (si)   // ✓
                    g.DrawLines(pen, new[] { new PointF(x + 10, cy), new PointF(x + 14, cy + 4), new PointF(x + 21, cy - 4) });
                else      // ✕
                {
                    g.DrawLine(pen, x + 11, cy - 4, x + 19, cy + 4);
                    g.DrawLine(pen, x + 19, cy - 4, x + 11, cy + 4);
                }
            }
            using (var b = new SolidBrush(tinta))
                g.DrawString(si ? "Sí" : "No", f, b, new RectangleF(x + 22, y, w - 22, h), centro);
        }
    }
}