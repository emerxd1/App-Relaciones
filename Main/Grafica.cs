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
        public string Etiqueta { get; set; }     // nombre que se muestra (A, B, C...)
        public List<string> Elementos { get; set; } = new();
    }

    // Dibuja UN solo digrafo y UNA sola matriz con todos los conjuntos y relaciones
    public static class Grafica
    {
        // Paleta moderna: un color por conjunto
        private static readonly Color[] Paleta =
        {
            Color.FromArgb(59, 91, 219),    // indigo (el mismo azul de tu interfaz)
            Color.FromArgb(18, 184, 134),   // verde azulado
            Color.FromArgb(253, 126, 20),   // naranja
            Color.FromArgb(230, 73, 128),   // rosa
            Color.FromArgb(132, 94, 247),   // violeta
            Color.FromArgb(34, 139, 230)    // azul
        };

        private static Color ColorGrupo(int i) => Paleta[i % Paleta.Length];

        private static readonly Color CeldaVacia = Color.FromArgb(243, 244, 246);
        private static readonly Color TextoGris = Color.FromArgb(173, 181, 189);

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
        // ------------------------------------------------------------------
        public static Bitmap Digrafo(IReadOnlyList<GrupoGrafica> grupos,
                                     IReadOnlyList<(string, int, string, int)> pares,
                                     int ancho, int alto, float zoom = 1f)   // NUEVO: zoom
        {
            int k = grupos.Count;
            if (k == 0) return new Bitmap(Math.Max(ancho, 1), Math.Max(alto, 1));

            int anchoU = (int)(ancho / zoom);   // NUEVO: tamaño en unidades de dibujo
            int altoU = (int)(alto / zoom);     // NUEVO

            const float nodoR = 20f;

            // tamaño de cada conjunto (burbuja) segun cuantos elementos tiene
            var radioGrupo = new float[k];
            var burbuja = new float[k];
            for (int i = 0; i < k; i++)
            {
                int n = grupos[i].Elementos.Count;
                radioGrupo[i] = n <= 1 ? 0f
                    : Math.Max(nodoR * 2.2f, n * (nodoR * 2 + 12) / (2f * (float)Math.PI));
                burbuja[i] = radioGrupo[i] + nodoR + 28f;
            }

            // los conjuntos se reparten en circulo (2 conjuntos: izquierda y derecha)
            float maxB = burbuja.Max();
            float R = k == 1 ? 0f : (maxB + 24f) / (float)Math.Sin(Math.PI / k);

            var centros = new PointF[k];
            for (int i = 0; i < k; i++)
            {
                double ang = Math.PI + 2 * Math.PI * i / k;
                centros[i] = new PointF((float)(R * Math.Cos(ang)), (float)(R * Math.Sin(ang)));
            }

            // tamaño del lienzo: como minimo el area visible, mas grande si hace falta
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < k; i++)
            {
                minX = Math.Min(minX, centros[i].X - burbuja[i]);
                maxX = Math.Max(maxX, centros[i].X + burbuja[i]);
                minY = Math.Min(minY, centros[i].Y - burbuja[i]);
                maxY = Math.Max(maxY, centros[i].Y + burbuja[i]);
            }
            const float margen = 36f;
            // int w = Math.Max(ancho, (int)Math.Ceiling(maxX - minX + 2 * margen));
            // int h = Math.Max(alto, (int)Math.Ceiling(maxY - minY + 2 * margen));
            int w = Math.Max(anchoU, (int)Math.Ceiling(maxX - minX + 2 * margen));   // NUEVO
            int h = Math.Max(altoU, (int)Math.Ceiling(maxY - minY + 2 * margen));    // NUEVO
            float offX = w / 2f - (minX + maxX) / 2f;
            float offY = h / 2f - (minY + maxY) / 2f;
            for (int i = 0; i < k; i++)
                centros[i] = new PointF(centros[i].X + offX, centros[i].Y + offY);

            // posicion de cada nodo
            var pos = new Dictionary<string, PointF>();
            var grupoDe = new Dictionary<string, int>();
            var nodos = new List<(PointF p, int g, string txt)>();
            for (int gi = 0; gi < k; gi++)
            {
                int n = grupos[gi].Elementos.Count;
                for (int j = 0; j < n; j++)
                {
                    PointF p;
                    if (n == 1) p = centros[gi];
                    else
                    {
                        double a = -Math.PI / 2 + 2 * Math.PI * j / n;
                        p = new PointF(centros[gi].X + radioGrupo[gi] * (float)Math.Cos(a),
                                       centros[gi].Y + radioGrupo[gi] * (float)Math.Sin(a));
                    }
                    string clave = grupos[gi].Clave + "|" + grupos[gi].Elementos[j];
                    pos[clave] = p;
                    grupoDe[clave] = gi;
                    nodos.Add((p, gi, grupos[gi].Elementos[j]));
                }
            }

            // var bmp = new Bitmap(w, h);
            var bmp = new Bitmap((int)(w * zoom), (int)(h * zoom));   // NUEVO
            using var g = Graphics.FromImage(bmp);
            Configurar(g);
            g.ScaleTransform(zoom, zoom);                             // NUEVO

            using var fPill = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var centro = Centro();

            // 1) burbujas de cada conjunto con su etiqueta
            for (int i = 0; i < k; i++)
            {
                Color col = ColorGrupo(i);
                PointF c = centros[i];
                float b = burbuja[i];

                using (var br = new SolidBrush(Color.FromArgb(22, col)))
                    g.FillEllipse(br, c.X - b, c.Y - b, 2 * b, 2 * b);
                using (var pen = new Pen(Color.FromArgb(90, col), 1.5f) { DashStyle = DashStyle.Dash })
                    g.DrawEllipse(pen, c.X - b, c.Y - b, 2 * b, 2 * b);

                string texto = "Conjunto " + grupos[i].Etiqueta;
                SizeF tam = g.MeasureString(texto, fPill);
                var pill = new RectangleF(c.X - (tam.Width + 20) / 2f, c.Y - b - 13, tam.Width + 20, 26);
                using (var path = Redondeado(pill, 13))
                using (var br = new SolidBrush(col))
                    g.FillPath(br, path);
                g.DrawString(texto, fPill, Brushes.White, pill, centro);
            }

            // 2) flechas curvas (una por cada par de la relacion)
            foreach (var par in pares)
            {
                string k1 = par.Item1 + "|" + par.Item2;
                string k2 = par.Item3 + "|" + par.Item4;
                if (!pos.TryGetValue(k1, out PointF p1) || !pos.TryGetValue(k2, out PointF p2)) continue;
                if (k1 == k2)
                    DibujarLazo(g, p1, centros[grupoDe[k1]], nodoR, ColorGrupo(grupoDe[k1]));
                else
                    DibujarFlecha(g, p1, p2, nodoR, ColorGrupo(grupoDe[k1]));
            }

            // 3) nodos encima de las flechas
            foreach (var (p, gi, txt) in nodos)
            {
                Color col = ColorGrupo(gi);
                using (var sombra = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                    g.FillEllipse(sombra, p.X - nodoR, p.Y - nodoR + 3, nodoR * 2, nodoR * 2);
                using (var br = new SolidBrush(col))
                    g.FillEllipse(br, p.X - nodoR, p.Y - nodoR, nodoR * 2, nodoR * 2);
                using (var pen = new Pen(Color.White, 2.5f))
                    g.DrawEllipse(pen, p.X - nodoR, p.Y - nodoR, nodoR * 2, nodoR * 2);

                float tamFuente = txt.Length <= 2 ? 11f : txt.Length == 3 ? 9.5f : 8f;
                using var f = new Font("Segoe UI", tamFuente, FontStyle.Bold);
                g.DrawString(txt, f, Brushes.White,
                    new RectangleF(p.X - nodoR, p.Y - nodoR, nodoR * 2, nodoR * 2), centro);
            }

            return bmp;
        }

        // Flecha curva de p1 a p2 con punta triangular
        private static void DibujarFlecha(Graphics g, PointF p1, PointF p2, float nodoR, Color color)
        {
            float dx = p2.X - p1.X, dy = p2.Y - p1.Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1f) return;

            // curvatura: las flechas de ida y vuelta se curvan hacia lados opuestos
            float nx = -dy / dist, ny = dx / dist;
            float curva = Math.Min(dist * 0.18f, 60f);
            var c1 = new PointF(p1.X + dx * 0.3f + nx * curva, p1.Y + dy * 0.3f + ny * curva);
            var c2 = new PointF(p1.X + dx * 0.7f + nx * curva, p1.Y + dy * 0.7f + ny * curva);

            // sale del borde del nodo origen
            PointF u0 = Unitario(c1.X - p1.X, c1.Y - p1.Y);
            var ini = new PointF(p1.X + u0.X * (nodoR + 2f), p1.Y + u0.Y * (nodoR + 2f));

            // llega al borde del nodo destino
            PointF uf = Unitario(p2.X - c2.X, p2.Y - c2.Y);
            var punta = new PointF(p2.X - uf.X * (nodoR + 3f), p2.Y - uf.Y * (nodoR + 3f));
            var finLinea = new PointF(punta.X - uf.X * 11f, punta.Y - uf.Y * 11f);

            // NUEVO: los puntos de control se alinean con la salida y la llegada,
            // asi la curva entra exactamente en la direccion de la punta
            float largoC = dist * 0.3f;
            var cc1 = new PointF(ini.X + u0.X * largoC, ini.Y + u0.Y * largoC);
            var cc2 = new PointF(finLinea.X - uf.X * largoC, finLinea.Y - uf.Y * largoC);

            using (var pen = new Pen(Color.FromArgb(200, color), 2.2f))
            {
                pen.StartCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                // g.DrawBezier(pen, ini, c1, c2, finLinea);
                g.DrawBezier(pen, ini, cc1, cc2, finLinea);   // NUEVO
            }

            PuntaFlecha(g, punta, uf, color);
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
            using (var br = new SolidBrush(Color.FromArgb(230, color)))
                g.FillPolygon(br, tri);
        }

        // Lazo (a, a): arco que sale del nodo hacia afuera de su conjunto
        private static void DibujarLazo(Graphics g, PointF p, PointF centroGrupo, float nodoR, Color color)
        {
            PointF u = Unitario(p.X - centroGrupo.X, p.Y - centroGrupo.Y);
            if (Math.Abs(p.X - centroGrupo.X) < 0.5f && Math.Abs(p.Y - centroGrupo.Y) < 0.5f)
                u = new PointF(0, -1);   // nodo en el centro del conjunto: el lazo sube
            double a = Math.Atan2(u.Y, u.X);

            PointF En(double ang, float dist) =>
                new PointF(p.X + (float)Math.Cos(ang) * dist, p.Y + (float)Math.Sin(ang) * dist);

            PointF ini = En(a - 0.55, nodoR + 1f);
            PointF fin = En(a + 0.55, nodoR + 3f);
            PointF c1 = En(a - 0.95, nodoR * 3.3f);
            PointF c2 = En(a + 0.95, nodoR * 3.3f);

            PointF uf = Unitario(fin.X - c2.X, fin.Y - c2.Y);
            var finLinea = new PointF(fin.X - uf.X * 11f, fin.Y - uf.Y * 11f);

            // NUEVO: el lazo llega en la direccion de la punta
            var cc2 = new PointF(finLinea.X - uf.X * nodoR * 1.5f, finLinea.Y - uf.Y * nodoR * 1.5f);

            using (var pen = new Pen(Color.FromArgb(200, color), 2.2f))
            {
                pen.StartCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                // g.DrawBezier(pen, ini, c1, c2, finLinea);
                g.DrawBezier(pen, ini, c1, cc2, finLinea);   // NUEVO
            }
            PuntaFlecha(g, fin, uf, color);
        }

        // ------------------------------------------------------------------
        //  MATRIZ DE ADYACENCIA (una sola, con todos los elementos)
        // ------------------------------------------------------------------
        public static Bitmap Matriz(IReadOnlyList<GrupoGrafica> grupos,
                                    IReadOnlyList<(string, int, string, int)> pares,
                                    int ancho = 0, int alto = 0)   // NUEVO: tamaño del panel
        {
            // una fila/columna por cada elemento de cada conjunto
            var items = new List<(int g, string clave, string etiqueta)>();
            for (int gi = 0; gi < grupos.Count; gi++)
                foreach (string el in grupos[gi].Elementos)
                    items.Add((gi, grupos[gi].Clave + "|" + el, grupos[gi].Etiqueta + "·" + el));

            int N = items.Count;
            if (N == 0) return new Bitmap(1, 1);

            var ady = new HashSet<(string, string)>();
            foreach (var p in pares)
                ady.Add((p.Item1 + "|" + p.Item2, p.Item3 + "|" + p.Item4));

            int maxEtiqueta = items.Max(it => it.etiqueta.Length);
            int cell = Math.Max(40, 12 + maxEtiqueta * 8);   // tamaño de cada celda
            const int gap = 14;                              // separacion entre conjuntos
            const int margen = 20;
            int filaW = cell + 14;                           // ancho de las etiquetas de fila
            const int colH = 28;                             // alto de las etiquetas de columna

            // NUEVO: si se conoce el panel, la celda crece para llenarlo (hasta 110 px)
            if (ancho > 0 && alto > 0)
            {
                int kk = grupos.Count;
                int porAncho = (ancho - 2 * margen - 22 - (kk - 1) * gap) / (N + 1);
                int porAlto = (alto - 2 * margen - colH - 8 - (kk - 1) * gap) / N;
                cell = Math.Max(cell, Math.Min(110, Math.Min(porAncho, porAlto)));
                filaW = cell + 14;
            }
            float esc = Math.Max(1f, cell / 44f);   // NUEVO: escala de las letras

            int x0 = margen + filaW + 8;
            int y0 = margen + colH + 8;

            var xs = new int[N];
            var ys = new int[N];
            for (int i = 0; i < N; i++)
            {
                xs[i] = x0 + i * cell + items[i].g * gap;
                ys[i] = y0 + i * cell + items[i].g * gap;
            }

            int w = xs[N - 1] + cell + margen;
            int h = ys[N - 1] + cell + margen;

            var bmp = new Bitmap(w, h);
            using var g = Graphics.FromImage(bmp);
            Configurar(g);

            // using var fBold = new Font("Segoe UI", 11f, FontStyle.Bold);
            // using var fNormal = new Font("Segoe UI", 10f);
            // using var fEtiqueta = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var fBold = new Font("Segoe UI", 11f * esc, FontStyle.Bold);                      // NUEVO
            using var fNormal = new Font("Segoe UI", 10f * esc);                                    // NUEVO
            using var fEtiqueta = new Font("Segoe UI", 9f * Math.Min(esc, 1.4f), FontStyle.Bold);   // NUEVO
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

                var rc = new RectangleF(xs[i] + 2, margen, cell - 4, colH);
                using (var path = Redondeado(rc, 9))
                using (var br = new SolidBrush(col))
                    g.FillPath(br, path);
                g.DrawString(items[i].etiqueta, fEtiqueta, Brushes.White, rc, centro);

                var rf = new RectangleF(margen, ys[i] + 4, filaW, cell - 8);
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
                    var rect = new RectangleF(xs[c] + 2, ys[r] + 2, cell - 4, cell - 4);

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
        //  PROPIEDADES (NUEVO): tarjetas con Si / No + mini digrafo y definicion
        //  (solo reflexiva, simetrica, antisimetrica y transitiva)
        // ------------------------------------------------------------------
        private static readonly Color MiniTinta = Color.FromArgb(30, 40, 70);

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
            using var fMini = new Font("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Pixel);
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

                    // fila 2: descripcion de la propiedad (sin imagenes)
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

        // Mini digrafo de ejemplo de cada propiedad (0 reflexiva, 1 simetrica, 2 antisimetrica, 3 transitiva)
        private static void MiniDiagrama(Graphics g, int tipo, float ox, float oy, Font f, StringFormat centro, bool cumple)
        {
            const float r = 8f;
            // NUEVO: en rojo cuando la propiedad no se cumple
            Color tinta = cumple ? MiniTinta : Color.FromArgb(200, 50, 50);
            Color borde = cumple ? Color.FromArgb(18, 140, 130) : Color.FromArgb(200, 50, 50);
            Color fondo = cumple ? Color.FromArgb(226, 244, 241) : Color.FromArgb(253, 232, 232);
            Color texto = cumple ? Color.FromArgb(24, 60, 70) : Color.FromArgb(150, 30, 30);
            PointF P(float px, float py) => new PointF(ox + px, oy + py);
            PointF a, b, c;

            switch (tipo)
            {
                case 0: // reflexiva: lazo en cada vertice
                    a = P(14, 34); b = P(48, 34); c = P(82, 34);
                    MiniFlecha(g, a, b, r, 0, tinta);
                    MiniLazo(g, a, r, tinta); MiniLazo(g, b, r, tinta); MiniLazo(g, c, r, tinta);
                    break;
                case 1: // simetrica: ida y vuelta
                    a = P(14, 26); b = P(48, 26); c = P(82, 26);
                    MiniFlecha(g, a, b, r, 12, tinta); MiniFlecha(g, b, a, r, 12, tinta);
                    MiniFlecha(g, b, c, r, 12, tinta); MiniFlecha(g, c, b, r, 12, tinta);
                    break;
                case 2: // antisimetrica: solo ida
                    a = P(48, 10); b = P(20, 42); c = P(76, 42);
                    MiniFlecha(g, a, b, r, 0, tinta); MiniFlecha(g, a, c, r, 0, tinta);
                    break;
                default: // transitiva: a->b, b->c y el atajo a->c
                    b = P(48, 10); a = P(14, 42); c = P(82, 42);
                    MiniFlecha(g, a, b, r, 0, tinta); MiniFlecha(g, b, c, r, 0, tinta); MiniFlecha(g, a, c, r, 0, tinta);
                    break;
            }

            MiniNodo(g, a, "a", f, centro, borde, fondo, texto);
            MiniNodo(g, b, "b", f, centro, borde, fondo, texto);
            MiniNodo(g, c, "c", f, centro, borde, fondo, texto);
        }

        private static void MiniNodo(Graphics g, PointF c, string t, Font f, StringFormat centro, Color borde, Color fondo, Color texto)
        {
            const float r = 8f;
            using (var br = new SolidBrush(fondo))
                g.FillEllipse(br, c.X - r, c.Y - r, 2 * r, 2 * r);
            using (var pen = new Pen(borde, 1.6f))
                g.DrawEllipse(pen, c.X - r, c.Y - r, 2 * r, 2 * r);
            using (var br = new SolidBrush(texto))
                g.DrawString(t, f, br, new RectangleF(c.X - r, c.Y - r, 2 * r, 2 * r), centro);
        }

        // Flecha de borde a borde de nodo; curva != 0 la curva hacia un lado
        private static void MiniFlecha(Graphics g, PointF a, PointF b, float r, float curva, Color tinta)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1f) return;

            var ctrl = new PointF((a.X + b.X) / 2f - dy / len * curva, (a.Y + b.Y) / 2f + dx / len * curva);
            PointF u0 = Unitario(ctrl.X - a.X, ctrl.Y - a.Y);
            PointF u1 = Unitario(b.X - ctrl.X, b.Y - ctrl.Y);

            var ini = new PointF(a.X + u0.X * r, a.Y + u0.Y * r);
            var punta = new PointF(b.X - u1.X * r, b.Y - u1.Y * r);
            var fin = new PointF(punta.X - u1.X * 5f, punta.Y - u1.Y * 5f);
            var c1 = new PointF(ini.X + 2f / 3f * (ctrl.X - ini.X), ini.Y + 2f / 3f * (ctrl.Y - ini.Y));
            var c2 = new PointF(fin.X + 2f / 3f * (ctrl.X - fin.X), fin.Y + 2f / 3f * (ctrl.Y - fin.Y));

            using (var pen = new Pen(tinta, 1.4f))
                g.DrawBezier(pen, ini, c1, c2, fin);
            MiniPunta(g, punta, u1, tinta);
        }

        // Lazo sobre el nodo; la punta termina justo en el borde
        private static void MiniLazo(Graphics g, PointF n, float r, Color tinta)
        {
            const float lr = 5f;
            float D = r + lr * 0.6f;
            var C = new PointF(n.X, n.Y - D);

            double cosF = (D * D + lr * lr - r * r) / (2.0 * D * lr);
            double phi = Math.Acos(Math.Max(-1, Math.Min(1, cosF)));
            double a0 = Math.PI / 2;   // de C hacia el nodo (abajo)

            using (var pen = new Pen(tinta, 1.4f))
                g.DrawArc(pen, C.X - lr, C.Y - lr, 2 * lr, 2 * lr,
                          (float)((a0 + phi) * 180 / Math.PI), (float)(360 - 2 * phi * 180 / Math.PI));

            double te = a0 - phi;
            var punta = new PointF(C.X + lr * (float)Math.Cos(te), C.Y + lr * (float)Math.Sin(te));
            MiniPunta(g, punta, new PointF(-(float)Math.Sin(te), (float)Math.Cos(te)), tinta);
        }

        private static void MiniPunta(Graphics g, PointF punta, PointF d, Color tinta)
        {
            var bas = new PointF(punta.X - d.X * 5.5f, punta.Y - d.Y * 5.5f);
            var tri = new[]
            {
                punta,
                new PointF(bas.X - d.Y * 2.8f, bas.Y + d.X * 2.8f),
                new PointF(bas.X + d.Y * 2.8f, bas.Y - d.X * 2.8f)
            };
            using (var br = new SolidBrush(tinta))
                g.FillPolygon(br, tri);
        }

        private static void Pildora(Graphics g, Font f, StringFormat centro, float x, float y, bool si)
        {
            const float w = 54, h = 24;
            Color fondo = si ? Color.FromArgb(214, 245, 228) : Color.FromArgb(253, 226, 226);   // NUEVO: No en rojo
            Color tinta = si ? Color.FromArgb(18, 140, 80) : Color.FromArgb(200, 50, 50);   // NUEVO: No en rojo

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