using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using task_Newton___13_var; 

namespace TaskNewtonTests
{
    [TestFixture]
    public class ProgramTests
    {
        const double TOL = 1e-6;

        // 1) Кубическое уравнение x^3 - 2 = 0  (корень = 2^(1/3))
        [Test]
        public void Test_Cubic_Converges_ModifiedAndStandard()
        {
            Func<double, double> f = x => x * x * x - 2.0;
            Func<double, double> df = x => 3.0 * x * x;
            double x0 = 1.0;
            var mod = Program.ModifiedNewton(f, df, x0, 1e-8);
            Assert.IsTrue(mod.Converged, "ModifiedNewton должен сходиться для x^3 - 2 с x0=1");
            double trueRoot = Math.Pow(2.0, 1.0 / 3.0);
            Assert.That(mod.Root, Is.EqualTo(trueRoot).Within(1e-5));

            var std = Program.StandardNewton(f, df, x0, 1e-14);
            Assert.IsTrue(std.Converged, "StandardNewton должен сходиться");
            Assert.That(std.Root, Is.EqualTo(trueRoot).Within(1e-12));
        }

        // 2) Функция a/(x+b)+c -> пример: 1/(x) - 1 = 0  => корень 1
        [Test]
        public void Test_Reciprocal_Converges()
        {
            Func<double, double> f = x => 1.0 / (x + 0.0) - 1.0;
            Func<double, double> df = x => -1.0 / ((x + 0.0) * (x + 0.0));
            double x0 = 1.5;
            var res = Program.StandardNewton(f, df, x0, tol: 1e-12, maxIter: 100);
            Assert.IsTrue(res.Converged, "StandardNewton не сошёлся для 1/x - 1");
            Assert.That(res.Root, Is.EqualTo(1.0).Within(1e-8));
        }

        // 3) Модифицированный Ньютон должен бросать, если df(x0) = 0
        [Test]
        public void Test_ModifiedNewton_Throws_When_DerivativeZeroAtStart()
        {
            Func<double, double> f = x => x * x * x;
            Func<double, double> df = x => 3.0 * x * x;
            double x0 = 0.0;
            Assert.Throws<Exception>(() => Program.ModifiedNewton(f, df, x0, 1e-8));
        }

        // 4) Постоянная функция (нет корня) - производная 0 => ModifiedNewton бросает
        [Test]
        public void Test_ConstantFunction_Throws()
        {
            Func<double, double> f = x => 1.0;
            Func<double, double> df = x => 0.0;
            Assert.Throws<Exception>(() => Program.ModifiedNewton(f, df, 0.0, 1e-6));
        }

        // 5) Парсинг файла: корректный файл
        [Test]
        public void Test_ParseFile_Success()
        {
            string tmp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tmp, "1 0 -2 1e-6 1");
                var tup = Program.ParseParametersFromFile(tmp);
                Assert.AreEqual(1.0, tup.a, 1e-12);
                Assert.AreEqual(0.0, tup.b, 1e-12);
                Assert.AreEqual(-2.0, tup.c, 1e-12);
                Assert.AreEqual(1e-6, tup.eps, 1e-12);
                Assert.AreEqual(1.0, tup.x0, 1e-12);
            }
            finally
            {
                File.Delete(tmp);
            }
        }

        // 6) Парсинг файла: недостаточно токенов -> FormatException
        [Test]
        public void Test_ParseFile_NotEnoughTokens_Throws()
        {
            string tmp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tmp, "1 2 3");
                Assert.Throws<FormatException>(() => Program.ParseParametersFromFile(tmp));
            }
            finally
            {
                File.Delete(tmp);
            }
        }

        // 7) Парсинг файла: десятичная запятая (русская) также должна работать
        [Test]
        public void Test_ParseFile_CommaDecimal_Success()
        {
            string tmp = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tmp, "1,5 0 0 1e-6 0");
                var tup = Program.ParseParametersFromFile(tmp);
                Assert.AreEqual(1.5, tup.a, 1e-12);
            }
            finally
            {
                File.Delete(tmp);
            }
        }

        // 8) Сравнение результатов Modified vs Standard — в типичном случае различие мало
        [Test]
        public void Test_ModifiedVsStandard_Close()
        {
            Func<double, double> f = x => x * x * x - 2.0;
            Func<double, double> df = x => 3.0 * x * x;
            double x0 = 1.0;
            var mod = Program.ModifiedNewton(f, df, x0, 1e-10, maxIter: 500);
            var std = Program.StandardNewton(f, df, x0, tol: 1e-14, maxIter: 500);
            Assert.IsTrue(mod.Converged && std.Converged);
            Assert.That(Math.Abs(mod.Root - std.Root), Is.LessThan(1e-4));
        }

        // 9) Нелинейная функция без действительных корней — метод не должен сходиться к реальному корню
        [Test]
        public void Test_NoRealRoot_DoesNotConverge_Modified()
        {
            Func<double, double> f = x => x * x + 1.0;
            Func<double, double> df = x => 2.0 * x;
            double x0 = 1.0;
            var res = Program.ModifiedNewton(f, df, x0, 1e-12, maxIter: 50);
            Assert.IsFalse(res.Converged);
        }

        // 10) Для косинусной функции — проверка сходимости классического Ньютона к нулю при подходящем x0
        [Test]
        public void Test_Cosine_StandardNewton_Converges_ToZero()
        {
            Func<double, double> f = x => Math.Cos(x) - 1.0;
            Func<double, double> df = x => -Math.Sin(x);
            double x0 = 0.1;
            var res = Program.StandardNewton(f, df, x0, tol: 1e-12, maxIter: 200);
            Assert.IsTrue(res.Converged, "StandardNewton не сошёлся");
            double fval = Math.Abs(Math.Cos(res.Root) - 1.0);
            Assert.That(fval, Is.LessThan(1e-12), $"|f(root)| = {fval} >= 1e-12");
        }
    }
}