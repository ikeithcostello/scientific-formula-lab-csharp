using ScientificFormulaLab;
using ScientificFormulaLab.Chemistry;
using ScientificFormulaLab.LabMath;
using ScientificFormulaLab.Physics;

namespace ScientificFormulaLab.Tests;

/// <summary>
/// Assertions mirroring TypeScript scripts/verify.cjs.
/// </summary>
public class VerifyTests
{
    private const double Avo = 6.02214076e23;

    [Fact]
    public void RealPolynomial_X2Minus1_At1()
    {
        var p1 = new RealPolynomial([1, 0, -1]);
        Assert.True(System.Math.Abs(p1.Evaluate(1)) < 1e-12);
    }

    [Fact]
    public void Simpson_Integral_0_2_XSquared()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        var I = eng.SimpsonDefinite(x => x * x, 0, 2, 100);
        Assert.True(System.Math.Abs(I - 8.0 / 3) < 1e-8);
    }

    [Fact]
    public void Bisection_Cubic()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        var r = eng.Bisection(x => x * x * x - x - 1, 1, 2);
        Assert.True(System.Math.Abs(r * r * r - r - 1) < 1e-7);
    }

    [Fact]
    public void PolyDerivative_DegDrops()
    {
        var p = new RealPolynomial([1, 0, -1, 0]);
        var d = p.Derivative();
        Assert.Equal(2, d.Degree);
    }

    [Fact]
    public void PolyMultiply()
    {
        var a = new RealPolynomial([1, 1]);
        var b = new RealPolynomial([1, -1]);
        var p = a.Multiply(b);
        Assert.Equal(2, p.Degree);
        Assert.True(System.Math.Abs(p.Evaluate(1)) < 1e-10);
    }

    [Fact]
    public void Cardano_X3MinusX()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        var c = eng.RealRootsCubic(1, 0, -1, 0);
        Assert.Equal(3, c.Count);
        Assert.Contains(c, x => System.Math.Abs(x + 1) < 1e-4);
        Assert.Contains(c, x => System.Math.Abs(x) < 1e-3);
        Assert.Contains(c, x => System.Math.Abs(x - 1) < 1e-3);
    }

    [Fact]
    public void QuarticScanRoots()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        var p = DemoPolynomials.MakeDemoQuarticForRoots();
        var roots = eng.ScanRealRootsOnInterval(p, -5, 5, 0.2);
        double[] targets = [-2, -0.5, 1, 3];
        Assert.True(targets.All(t => roots.Any(r => System.Math.Abs(r - t) < 0.15)));
    }

    [Fact]
    public void Erf0ApproxZero()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        Assert.True(System.Math.Abs(eng.Erf(0)) < 1e-8);
    }

    [Fact]
    public void Erf1Approx()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        Assert.True(System.Math.Abs(eng.Erf(1) - 0.8427007929) < 1e-4);
    }

    [Fact]
    public void ClenshawChebyshev()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        const double t = 0.3;
        double[] c = [1, 0, -1, 0];
        var cl = eng.ClenshawChebyshevT(c, t);
        var t0 = 1.0;
        var t1 = t;
        var t2 = 2 * t * t1 - 1;
        var t3 = 2 * t * t2 - t1;
        var exp = c[0] * t0 + c[1] * t1 + c[2] * t2 + c[3] * t3;
        Assert.True(System.Math.Abs(cl - exp) < 1e-10);
    }

    [Fact]
    public void Solve3x3Diagonal()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        double[][] A =
        [
            [2, 0, 0],
            [0, 3, 0],
            [0, 0, 1],
        ];
        var x = eng.Solve3x3(A, (1, 1, 1));
        Assert.True(System.Math.Abs(x.X0 - 0.5) < 1e-6);
        Assert.True(System.Math.Abs(x.X1 - 1.0 / 3) < 1e-6);
        Assert.True(System.Math.Abs(x.X2 - 1) < 1e-6);
    }

    [Fact]
    public void RK4Exp()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        var y1 = eng.RungeKutta4((_, y) => y, 0, 1, 0.1, 1).Y;
        Assert.True(System.Math.Abs(y1 - System.Math.Exp(0.1)) < 1e-6);
    }

    [Fact]
    public void LogGamma55()
    {
        var eng = new ScientificMathEngine(eps: 1e-9);
        var g55 = 4.5 * 3.5 * 2.5 * 1.5 * 0.5 * System.Math.Sqrt(System.Math.PI);
        var l = eng.LogGamma(5.5);
        Assert.True(System.Math.Abs(l - System.Math.Log(g55)) < 1e-3);
    }

    [Fact]
    public void ParabolicLevelRange45()
    {
        var pEng = new AnalyticalPhysicsEngine();
        var Rg = pEng.VacuumBallistics.RangeOnLevel(20, (45 * System.Math.PI) / 180, 0);
        var g = pEng.EarthG;
        const double th = 45;
        var expect = (20 * 20 * System.Math.Sin((2 * th * System.Math.PI) / 180)) / g;
        Assert.True(System.Math.Abs(Rg - expect) < 0.1);
    }

    [Fact]
    public void GEarthConstant()
    {
        Assert.True(System.Math.Abs(PhysicsConstants.GEarth - 9.80665) < 0.0001);
    }

    [Fact]
    public void DHONoDamping()
    {
        var d = new DampedHarmonic1D(1, 0, 1);
        var p = d.Response(1, 0, 1);
        Assert.True(System.Math.Abs(p.X - System.Math.Cos(1)) < 1e-6);
    }

    [Fact]
    public void Centripetal()
    {
        var pEng = new AnalyticalPhysicsEngine();
        var a = pEng.UniformCircle.Ac(10, 5);
        Assert.True(System.Math.Abs(a - 20) < 1e-9);
    }

    [Fact]
    public void RelativisticKAndGamma()
    {
        var pEng = new AnalyticalPhysicsEngine();
        const double m = 0.001;
        const double v = 1e7;
        var K = pEng.Relativity.KineticEnergy(m, v);
        var g = pEng.Relativity.Gamma(v);
        Assert.True(K > 0 && g > 1);
    }

    [Fact]
    public void LinearDragIntegratorRuns()
    {
        var d = new LinearDragProjectile2D(1, 0, 9.8);
        var r = d.Integrate(new Vec2(0, 0), new Vec2(5, 5), 0.5, 10);
        var last = r[^1];
        Assert.True(r.Count > 1 && last.T > 0);
    }

    [Fact]
    public void Rms1DEquipartition()
    {
        var pEng = new AnalyticalPhysicsEngine();
        var m = 0.028 / Avo;
        var vr = pEng.RmsSpeedFrom1DEquipartition(1.380649e-23, 300, m);
        var vr2 = System.Math.Sqrt((1.380649e-23 * 300) / m);
        Assert.True(System.Math.Abs(vr - vr2) < 1e-9);
    }

    [Fact]
    public void MolarMassH2O()
    {
        var chemEng = new ChemistryStoichiometryEngine();
        var m = chemEng.MolarMass("H2O");
        Assert.True(System.Math.Abs(m - 18.015) < 0.1);
    }

    [Fact]
    public void MolarMassCaOH2()
    {
        var chemEng = new ChemistryStoichiometryEngine();
        var m = chemEng.MolarMass("Ca(OH)2");
        Assert.True(m > 70 && m < 80);
    }

    [Fact]
    public void IdealGasNApprox1()
    {
        var chemEng = new ChemistryStoichiometryEngine();
        var n = chemEng.Gas.IdealMoles(100_000, 0.024465, 298);
        Assert.True(n > 0.9 && n < 1.1);
    }

    [Fact]
    public void Rms1DMatchesSqrtRTOverM()
    {
        var chemEng = new ChemistryStoichiometryEngine();
        const double T = 300;
        const double M = 0.028;
        var c1 = chemEng.Gas.Rms1D(M, T);
        var c2 = System.Math.Sqrt((ChemistryConstants.RGas * T) / M);
        Assert.True(System.Math.Abs(c1 - c2) < 1e-12);
    }

    [Fact]
    public void WeakAcidPH()
    {
        var chemEng = new ChemistryStoichiometryEngine();
        var h = chemEng.Solution.WeakAcidHPlusApprox(0.1, 1.8e-5);
        var ph = chemEng.Solution.PHFromH3OPlusMolarity(h);
        Assert.True(ph > 2.7 && ph < 2.95);
    }

    [Fact]
    public void MonoproticTitration()
    {
        var chemEng = new ChemistryStoichiometryEngine();
        var v = chemEng.Solution.MonoproticTitrationScalar(0.1, 0.02, 0.1);
        Assert.True(System.Math.Abs(v - 0.02) < 1e-6);
    }

    [Fact]
    public void LimitingReagent()
    {
        var rx = new StoichiometricReaction(
            [("A", 1), ("B", 2)],
            Array.Empty<(string, double)>());
        var lim = rx.LimitReactantMoles(new Dictionary<string, double> { ["A"] = 2, ["B"] = 3 });
        Assert.Equal("B", lim.Limiting);
        Assert.True(System.Math.Abs(lim.Scale - 1.5) < 1e-9);
    }

    [Fact]
    public void VanDerWaalsP()
    {
        var chemEng = new ChemistryStoichiometryEngine();
        var P = chemEng.Gas.VanDerWaalsPressure1mol(0.02027, 2.3e-5, 0.01, 300);
        Assert.True(P > 0);
    }

    [Fact]
    public void CrossDomainLab_RunAll_Completes()
    {
        var lab = new CrossDomainLab();
        var r = lab.RunAll();
        Assert.Equal(5, r.Count);
    }

    [Fact]
    public void CrossDomainLab_LogNonEmpty()
    {
        var lab = new CrossDomainLab();
        lab.RunAll();
        Assert.True(lab.Log.Count > 5);
    }
}
