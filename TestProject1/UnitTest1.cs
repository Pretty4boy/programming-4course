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

        // 1) ���������� ��������� x^3 - 2 = 0  (������ = 2^(1/3))
        [Test]
        public void Test_Cubic_Converges_ModifiedAndStandard()
        {
            Func<double, double> f = x => x * x * x - 2.0;
            Func<double, double> df = x => 3.0 * x * x;
            double x0 = 1.0;
            var mod = Program.ModifiedNewton(f, df, x0, 1e-8);
            Assert.IsTrue(mod.Converged, "ModifiedNewton ������ ��������� ��� x^3 - 2 � x0=1");
            double trueRoot = Math.Pow(2.0, 1.0 / 3.0);
            Assert.That(mod.Root, Is.EqualTo(trueRoot).Within(1e-5));

            var std = Program.StandardNewton(f, df, x0, 1e-14);
            Assert.IsTrue(std.Converged, "StandardNewton ������ ���������");
            Assert.That(std.Root, Is.EqualTo(trueRoot).Within(1e-12));
        }

        // 2) ������� a/(x+b)+c -> ������: 1/(x) - 1 = 0  => ������ 1
        [Test]
        public void Test_Reciprocal_Converges()
        {
            Func<double, double> f = x => 1.0 / (x + 0.0) - 1.0;
            Func<double, double> df = x => -1.0 / ((x + 0.0) * (x + 0.0));
            double x0 = 1.5;
            var res = Program.StandardNewton(f, df, x0, tol: 1e-12, maxIter: 100);
            Assert.IsTrue(res.Converged, "StandardNewton �� ������� ��� 1/x - 1");
            Assert.That(res.Root, Is.EqualTo(1.0).Within(1e-8));
        }

        // 3) ���������������� ������ ������ �������, ���� df(x0) = 0
        [Test]
        public void Test_ModifiedNewton_Throws_When_DerivativeZeroAtStart()
        {
            Func<double, double> f = x => x * x * x;
            Func<double, double> df = x => 3.0 * x * x;
            double x0 = 0.0;
            Assert.Throws<Exception>(() => Program.ModifiedNewton(f, df, x0, 1e-8));
        }

        // 4) ���������� ������� (��� �����) - ����������� 0 => ModifiedNewton �������
        [Test]
        public void Test_ConstantFunction_Throws()
        {
            Func<double, double> f = x => 1.0;
            Func<double, double> df = x => 0.0;
            Assert.Throws<Exception>(() => Program.ModifiedNewton(f, df, 0.0, 1e-6));
        }

        // 5) ������� �����: ���������� ����
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

        // 6) ������� �����: ������������ ������� -> FormatException
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

        // 7) ������� �����: ���������� ������� (�������) ����� ������ ��������
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

        // 8) ��������� ����������� Modified vs Standard � � �������� ������ �������� ����
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

        // 9) ���������� ������� ��� �������������� ������ � ����� �� ������ ��������� � ��������� �����
        [Test]
        public void Test_NoRealRoot_DoesNotConverge_Modified()
        {
            Func<double, double> f = x => x * x + 1.0;
            Func<double, double> df = x => 2.0 * x;
            double x0 = 1.0;
            var res = Program.ModifiedNewton(f, df, x0, 1e-12, maxIter: 50);
            Assert.IsFalse(res.Converged);
        }

        // 10) ��� ���������� ������� � �������� ���������� ������������� ������� � ���� ��� ���������� x0
        [Test]
        public void Test_Cosine_StandardNewton_Converges_ToZero()
        {
            Func<double, double> f = x => Math.Cos(x) - 1.0;
            Func<double, double> df = x => -Math.Sin(x);
            double x0 = 0.1;
            var res = Program.StandardNewton(f, df, x0, tol: 1e-12, maxIter: 200);
            Assert.IsTrue(res.Converged, "StandardNewton �� �������");
            double fval = Math.Abs(Math.Cos(res.Root) - 1.0);
            Assert.That(fval, Is.LessThan(1e-12), $"|f(root)| = {fval} >= 1e-12");
        }
    }
}