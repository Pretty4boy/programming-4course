// Program.cs (обновлённый)
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace task_Newton___13_var
{
    // Сделаем Program публичным, чтобы тесты могли обращаться к методам
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Решение уравнений модифицированным методом Ньютона.");
            Console.WriteLine("Выберите уравнение:");
            Console.WriteLine("1) y = a*x^3 + b*x + c");
            Console.WriteLine("2) y = a*cos(x + b) + c");
            Console.WriteLine("3) y = a/(x + b) + c");
            int eq = ReadIntInRange("Номер уравнения (1..3): ", 1, 3);

            Console.WriteLine("Выберите способ ввода параметров: 1 - клавиатура, 2 - файл");
            int inputMode = ReadIntInRange("Ваш выбор (1 или 2): ", 1, 2);

            double a = 0, b = 0, c = 0, eps = 1e-6, x0 = 0;
            if (inputMode == 1)
            {
                a = ReadDouble("Введите параметр a: ");
                b = ReadDouble("Введите параметр b: ");
                c = ReadDouble("Введите параметр c: ");
                eps = ReadDouble("Введите требуемую погрешность (eps > 0), например 1e-6: ");
                x0 = ReadDouble("Введите начальную точку x0: ");
            }
            else
            {
                Console.Write("Введите имя файла (например input.txt): ");
                string fileName = Console.ReadLine()?.Trim() ?? "";
                try
                {
                    // Используем новую публичную утилиту
                    var tup = ParseParametersFromFile(fileName);
                    a = tup.a; b = tup.b; c = tup.c; eps = tup.eps; x0 = tup.x0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при чтении файла: " + ex.Message);
                    return;
                }
            }

            Func<double, double> f;
            Func<double, double> df;

            switch (eq)
            {
                case 1:
                    f = (x) => a * x * x * x + b * x + c;
                    df = (x) => 3.0 * a * x * x + b;
                    break;
                case 2:
                    f = (x) => a * Math.Cos(x + b) + c;
                    df = (x) => -a * Math.Sin(x + b);
                    break;
                case 3:
                    f = (x) =>
                    {
                        if (Math.Abs(x + b) < 1e-16) throw new Exception("Значение x + b близко к нулю — функция не определена.");
                        return a / (x + b) + c;
                    };
                    df = (x) =>
                    {
                        if (Math.Abs(x + b) < 1e-16) throw new Exception("Значение x + b близко к нулю — производная не определена.");
                        return -a / ((x + b) * (x + b));
                    };
                    break;
                default:
                    throw new InvalidOperationException();
            }

            try
            {
                var modResult = ModifiedNewton(f, df, x0, eps, maxIter: 1000);
                // Эталонный корень: обычный Ньютон
                var exactResult = StandardNewton(f, df, x0, tol: 1e-14, maxIter: 2000);

                Console.WriteLine();
                Console.WriteLine("---- РЕЗУЛЬТАТЫ ----");
                Console.WriteLine($"Приближённый корень (модифицированный Ньютон): x приблизительно равен {modResult.Root:R}");
                if (modResult.Converged)
                    Console.WriteLine($" (итераций: {modResult.Iterations})");
                else
                    Console.WriteLine(" (не сошёлся в заданном числе итераций)");

                if (exactResult.Converged)
                {
                    Console.WriteLine($"Эталонный (численный) корень (стандартный Ньютон, tol=1e-14): x = {exactResult.Root:R}");
                    double absErr = Math.Abs(modResult.Root - exactResult.Root);
                    double relErr = absErr / (Math.Abs(exactResult.Root) + 1e-30);
                    Console.WriteLine($"Абсолютная погрешность: {absErr:E}");
                    Console.WriteLine($"Относительная погрешность: {relErr:E}");
                }
                else
                {
                    Console.WriteLine("Эталонный корень не был найден стандартным Ньютоном (возможно, метод не сошёлся).");
                }

                Console.WriteLine();
                Console.WriteLine("Таблица итераций (модифицированный Ньютон):");
                Console.WriteLine("n\t x_n\t\t f(x_n)\t\t delta");
                foreach (var it in modResult.IterationsData)
                {
                    Console.WriteLine($"{it.Index}\t {it.Xn:E}\t {it.Fxn:E}\t {it.Delta:E}");
                }

                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
                System.Windows.Forms.Application.Run(new GraphForm(f, a, b, c, modResult.IterationsForPlot));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка во время вычислений: " + ex.Message);
            }
        }

        // ---------- ЧТЕНИЕ ВВОДА ----------
        public static int ReadIntInRange(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = Console.ReadLine()?.Trim() ?? "";
                if (int.TryParse(s, out int v) && v >= min && v <= max) return v;
                Console.WriteLine($"Введите целое число от {min} до {max}.");
            }
        }

        public static double ReadDouble(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = Console.ReadLine()?.Trim() ?? "";
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out double v)) return v;
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
                Console.WriteLine("Неверный ввод. Вводите, например: 1.5 или 1,5 или 1e-3");
            }
        }

        // Сделаем парсер и вспомогательный метод публичными — тесты смогут их вызывать
        public static double ParseDoubleFlexible(string s)
        {
            // Попытка с текущей культурой
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out double v)) return v;
            // Попытка с инвариантной культурой
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;

            // Если не распарсилось, попробуем заменить запятую на точку и парсить инвариантно.
            // Это позволяет корректно понимать "1,5" независимо от CurrentCulture.
            string alt = s.Replace(',', '.');
            if (double.TryParse(alt, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;

            throw new FormatException($"Не удалось распознать число: {s}");
        }

        /// <summary>
        /// Прочитать первые пять токенов из файла: a b c eps x0.
        /// Разделители: пробел/таб/перевод строки/запятая.
        /// Бросает исключения при ошибках.
        /// </summary>
        public static (double a, double b, double c, double eps, double x0) ParseParametersFromFile(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Файл не найден: {filePath}");

            // Разделяем только по пробельным символам (не по запятой!)
            string[] tokens = File.ReadAllText(filePath)
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < 5) throw new FormatException("В файле недостаточно токенов: требуется 5 (a b c eps x0).");

            double a = ParseDoubleFlexible(tokens[0]);
            double b = ParseDoubleFlexible(tokens[1]);
            double c = ParseDoubleFlexible(tokens[2]);
            double eps = ParseDoubleFlexible(tokens[3]);
            double x0 = ParseDoubleFlexible(tokens[4]);

            return (a, b, c, eps, x0);
        }

        // ---------- МЕТОДЫ РЕШЕНИЯ ----------
        // Оставим IterationRecord и NewtonResult как внешние публичные классы (см. ниже)

        // Модифицированный Ньютон
        public static NewtonResult ModifiedNewton(Func<double, double> f, Func<double, double> df, double x0, double eps, int maxIter = 1000)
        {
            var res = new NewtonResult();
            double d0 = df(x0);
            if (double.IsNaN(d0) || Math.Abs(d0) < 1e-16) throw new Exception("Производная в начальной точке близка к нулю — модифицированный метод не применим.");

            double xn = x0;
            res.IterationsForPlot.Add(xn);
            res.IterationsData.Add(new IterationRecord { Index = 0, Xn = xn, Fxn = f(xn), Delta = 0.0 });

            for (int n = 0; n < maxIter; n++)
            {
                double fx = f(xn);
                double xnext = xn - fx / d0;
                double delta = xnext - xn;
                res.IterationsData.Add(new IterationRecord { Index = n + 1, Xn = xnext, Fxn = f(xnext), Delta = delta });
                res.IterationsForPlot.Add(xnext);

                if (Math.Abs(delta) < eps) { res.Converged = true; res.Root = xnext; res.Iterations = n + 1; return res; }
                if (Math.Abs(f(xnext)) < eps) { res.Converged = true; res.Root = xnext; res.Iterations = n + 1; return res; }

                xn = xnext;
            }

            // не сошёлся за maxIter
            res.Converged = false;
            res.Root = xn;
            res.Iterations = maxIter;
            return res;
        }

        // Классический Ньютон (пересчитываем производную)
        public static NewtonResult StandardNewton(Func<double, double> f, Func<double, double> df, double x0, double tol = 1e-14, int maxIter = 2000)
        {
            var res = new NewtonResult();
            double xn = x0;
            res.IterationsData.Add(new IterationRecord { Index = 0, Xn = xn, Fxn = f(xn), Delta = 0.0 });
            for (int n = 0; n < maxIter; n++)
            {
                double fx = f(xn);
                double dfx = df(xn);
                if (double.IsNaN(dfx) || Math.Abs(dfx) < 1e-16) break; // проблемная точка
                double xnext = xn - fx / dfx;
                double delta = xnext - xn;
                res.IterationsData.Add(new IterationRecord { Index = n + 1, Xn = xnext, Fxn = f(xnext), Delta = delta });
                if (Math.Abs(delta) < tol) { res.Converged = true; res.Root = xnext; res.Iterations = n + 1; return res; }
                if (Math.Abs(f(xnext)) < tol) { res.Converged = true; res.Root = xnext; res.Iterations = n + 1; return res; }
                xn = xnext;
            }
            res.Converged = false;
            res.Root = xn;
            res.Iterations = maxIter;
            return res;
        }
    }

    // Вынесем записи итераций и результат в публичные классы на уровне namespace,
    // чтобы тесты могли использовать их.
    public class IterationRecord
    {
        public int Index;
        public double Xn;
        public double Fxn;
        public double Delta;
    }

    public class NewtonResult
    {
        public bool Converged;
        public double Root;
        public int Iterations;
        public List<IterationRecord> IterationsData = new List<IterationRecord>();
        public List<double> IterationsForPlot = new List<double>();
    }

    // ---------- ФОРМА ДЛЯ ГРАФИЧЕСКОГО ОТОБРАЖЕНИЯ ----------
    public class GraphForm : Form
    {
        private readonly Func<double, double> f;
        private readonly double a, b, c;
        private List<double> iterX; // точки итераций (последовательность x_n)
        private float scale = 1.0f;
        private PointF translation = PointF.Empty;
        private PointF lastMousePosition;
        private bool isDragging = false;

        public GraphForm(Func<double, double> f, double a, double b, double c, List<double> iterations)
        {
            this.f = f;
            this.a = a; this.b = b; this.c = c;
            this.iterX = iterations != null ? new List<double>(iterations) : new List<double>();

            Text = "График функции и процесс итераций (модифицированный Ньютон)";
            Width = 1000;
            Height = 800;
            BackColor = Color.White;
            DoubleBuffered = true;

            MouseWheel += GraphForm_MouseWheel;
            MouseDown += GraphForm_MouseDown;
            MouseMove += GraphForm_MouseMove;
            MouseUp += GraphForm_MouseUp;

            // Управление клавишами: R - сброс трансформаций
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.R) { scale = 1.0f; translation = PointF.Empty; Invalidate(); }
            };
        }

        private void GraphForm_MouseWheel(object sender, MouseEventArgs e)
        {
            float zoomFactor = e.Delta > 0 ? 1.15f : 1 / 1.15f;
            PointF mousePos = e.Location;
            PointF graphPos = ScreenToGraph(mousePos);

            scale *= zoomFactor;
            PointF newMousePos = GraphToScreen(graphPos);
            translation.X += mousePos.X - newMousePos.X;
            translation.Y += mousePos.Y - newMousePos.Y;
            Invalidate();
        }

        private void GraphForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                lastMousePosition = e.Location;
                Cursor = Cursors.Hand;
            }
        }

        private void GraphForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                translation.X += e.X - lastMousePosition.X;
                translation.Y += e.Y - lastMousePosition.Y;
                lastMousePosition = e.Location;
                Invalidate();
            }
        }

        private void GraphForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                Cursor = Cursors.Default;
            }
        }

        private PointF ScreenToGraph(PointF screenPoint)
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            PointF center = new PointF(w / 2f, h / 2f);
            return new PointF(
                (screenPoint.X - center.X - translation.X) / scale,
                -(screenPoint.Y - center.Y - translation.Y) / scale
            );
        }

        private PointF GraphToScreen(PointF graphPoint)
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            PointF center = new PointF(w / 2f, h / 2f);
            return new PointF(
                center.X + translation.X + graphPoint.X * scale,
                center.Y + translation.Y - graphPoint.Y * scale
            );
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int w = ClientSize.Width;
            int h = ClientSize.Height;

            PointF topLeft = ScreenToGraph(PointF.Empty);
            PointF bottomRight = ScreenToGraph(new PointF(w, h));
            double visibleMinX = Math.Min(topLeft.X, bottomRight.X);
            double visibleMaxX = Math.Max(topLeft.X, bottomRight.X);

            double minIterX = iterX.Count > 0 ? iterX.Min() : 0;
            double maxIterX = iterX.Count > 0 ? iterX.Max() : 0;
            if (iterX.Count > 0)
            {
                double span = Math.Max(1.0, (maxIterX - minIterX) * 1.5);
                visibleMinX = Math.Min(visibleMinX, minIterX - span * 0.2);
                visibleMaxX = Math.Max(visibleMaxX, maxIterX + span * 0.2);
            }
            else
            {
                visibleMinX = -10; visibleMaxX = 10;
            }

            int points = 2000;
            var graphPoints = new List<PointF>(points);
            double xmin = visibleMinX, xmax = visibleMaxX;
            if (Math.Abs(xmax - xmin) < 1e-8) { xmin -= 1; xmax += 1; }
            for (int i = 0; i < points; i++)
            {
                double x = xmin + (xmax - xmin) * i / (points - 1);
                double y;
                try { y = f(x); }
                catch { y = double.NaN; }
                if (!double.IsNaN(y) && !double.IsInfinity(y)) graphPoints.Add(new PointF((float)x, (float)y));
                else graphPoints.Add(PointF.Empty);
            }

            double minY = double.MaxValue, maxY = double.MinValue;
            foreach (var p in graphPoints)
            {
                if (p == PointF.Empty) continue;
                minY = Math.Min(minY, p.Y);
                maxY = Math.Max(maxY, p.Y);
            }
            if (double.IsInfinity(minY) || double.IsInfinity(maxY) || minY > maxY)
            {
                minY = -10; maxY = 10;
            }
            if (Math.Abs(maxY - minY) < 1e-6) { minY -= 1; maxY += 1; }

            // Сетка
            DrawGrid(g, w, h, xmin, xmax, minY, maxY);

            // Оси
            DrawAxes(g, w, h);

            // график функции
            var screenPts = graphPoints.Select(pt => pt == PointF.Empty ? PointF.Empty : GraphToScreen(pt)).ToArray();
            using (Pen pen = new Pen(Color.Blue, 2))
            {
                for (int i = 1; i < screenPts.Length; i++)
                {
                    if (screenPts[i - 1] == PointF.Empty || screenPts[i] == PointF.Empty) continue;
                    g.DrawLine(pen, screenPts[i - 1], screenPts[i]);
                }
            }

            // итерации: точки и стрелки между ними
            DrawIterations(g);

            // Легенда / подписи
            using (Font font = new Font("Arial", 10))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                string info = "R – сброс масштаба. Колесико – масштаб, ЛКМ – перемещение";
                g.DrawString(info, font, brush, 10, h - 22);
            }
        }

        private void DrawGrid(Graphics g, int w, int h, double xmin, double xmax, double ymin, double ymax)
        {
            // шаг сетки по X и Y
            double stepX = NiceStep(xmax - xmin);
            double stepY = NiceStep(ymax - ymin);

            using (Pen gridPen = new Pen(Color.LightGray, 1))
            {
                gridPen.DashStyle = DashStyle.Dash;
                double startX = Math.Ceiling(xmin / stepX) * stepX;
                for (double x = startX; x <= xmax; x += stepX)
                {
                    var sp = GraphToScreen(new PointF((float)x, 0));
                    g.DrawLine(gridPen, sp.X, 0, sp.X, h);
                }
                double startY = Math.Ceiling(ymin / stepY) * stepY;
                for (double y = startY; y <= ymax; y += stepY)
                {
                    var sp = GraphToScreen(new PointF(0, (float)y));
                    g.DrawLine(gridPen, 0, sp.Y, w, sp.Y);
                }
            }
        }

        private double NiceStep(double range)
        {
            if (range <= 0) return 1.0;
            double mag = Math.Pow(10, Math.Floor(Math.Log10(range)));
            double norm = range / mag;
            if (norm <= 2) return 0.2 * mag;
            if (norm <= 5) return 0.5 * mag;
            return 1.0 * mag;
        }

        private void DrawAxes(Graphics g, int w, int h)
        {
            PointF center = GraphToScreen(PointF.Empty);
            using (Pen axisPen = new Pen(Color.Black, 2))
            {
                // X
                g.DrawLine(axisPen, 0, center.Y, w, center.Y);
                // Y
                g.DrawLine(axisPen, center.X, 0, center.X, h);
            }
            using (Font font = new Font("Arial", 10))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                string sX = "X", sY = "Y";
                g.DrawString(sX, font, brush, w - 20, center.Y + 5);
                g.DrawString(sY, font, brush, center.X + 5, 5);
            }
        }

        private void DrawIterations(Graphics g)
        {
            if (iterX == null || iterX.Count == 0) return;
            using (Pen penLine = new Pen(Color.OrangeRed, 2))
            using (Brush brushPoint = new SolidBrush(Color.Red))
            using (Font font = new Font("Arial", 9))
            {
                for (int i = 0; i < iterX.Count; i++)
                {
                    double x = iterX[i];
                    double y;
                    try { y = f(x); }
                    catch { continue; }

                    PointF pOnCurve = GraphToScreen(new PointF((float)x, (float)y));
                    PointF pOnXaxis = GraphToScreen(new PointF((float)x, 0));

                    // Вертикальная линия от x-axis до точки на кривой
                    g.DrawLine(penLine, pOnXaxis, pOnCurve);

                    // Точка на оси X (итерация)
                    float r = 5f;
                    g.FillEllipse(brushPoint, pOnXaxis.X - r, pOnXaxis.Y - r, 2 * r, 2 * r);

                    // Точка на кривой
                    g.FillEllipse(Brushes.DarkBlue, pOnCurve.X - r, pOnCurve.Y - r, 2 * r, 2 * r);

                    // Подпись номера итерации рядом
                    g.DrawString($"n={i}", font, Brushes.DarkGreen, pOnXaxis.X + 6, pOnXaxis.Y - 14);

                    // Стрелка к следующему x (если есть)
                    if (i + 1 < iterX.Count)
                    {
                        PointF nextOnXaxis = GraphToScreen(new PointF((float)iterX[i + 1], 0));
                        DrawArrow(g, penLine, pOnXaxis, nextOnXaxis);
                    }
                }
            }
        }

        private void DrawArrow(Graphics g, Pen pen, PointF from, PointF to)
        {
            g.DrawLine(pen, from, to);
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            float size = 8f;
            PointF p1 = new PointF(
                to.X - size * (float)Math.Cos(angle - Math.PI / 6),
                to.Y - size * (float)Math.Sin(angle - Math.PI / 6));
            PointF p2 = new PointF(
                to.X - size * (float)Math.Cos(angle + Math.PI / 6),
                to.Y - size * (float)Math.Sin(angle + Math.PI / 6));
            g.FillPolygon(Brushes.OrangeRed, new[] { to, p1, p2 });
        }
    }
}
