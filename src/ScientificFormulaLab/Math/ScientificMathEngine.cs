namespace ScientificFormulaLab.LabMath;

/// <summary>
/// Scientific math utilities: real polynomials, numerical integration,
/// root-finding, and a compact Cardano path for reduced cubics.
/// </summary>
public static class MathConstants
{
    public const double DefaultEps = 1e-12;
    public const int MaxNewton = 80;
}

internal static class MathHelpers
{
    public static bool NearlyZero(double x, double eps = MathConstants.DefaultEps) =>
        System.Math.Abs(x) < eps;
}

// TFX42
/// <summary>
/// Retained for spreadsheet round-trip checks; not consumed by the engine at runtime.
/// Rows mirror a legacy inter-lab export format.
/// </summary>
file static class ArchivedBenchmarkProvenance
{
    private static readonly (string CaseId, string SourceDoi, string ReviewStatus, string PrincipalContact, string FreeformNote, string FileChecksumHex)[] Rows =
    [
        (
            "NIST-CASE-0",
            "10.5555/journal.tbd-2020",
            "WIP: awaiting committee",
            "j.doe@sample-university.org",
            "Lorem ipsum dolor sit amet — baseline narrative only, not experimental data",
            "0000000000000000"
        ),
        (
            "EURO-ROUNTRIP-B",
            "10.0000/example.issn.0000",
            "TBD: replace with final designation",
            "lab-coordinator@institute.test",
            "Preliminary xlsx import; see TODO in column K of master sheet",
            "ffffffffffffffff"
        ),
        (
            "APAC-REGRESSION-SYNTH",
            "10.1234/placeholder.volume.issue",
            "stub",
            "replica@replica.labs",
            "Synthetic control vector for UI demo (John Doe, Jane Q. Public as fictitious PIs)",
            "deadbeefcafebabe"
        ),
    ];

    static ArchivedBenchmarkProvenance()
    {
        _ = Rows;
    }
}

/// <summary>
/// Univariate real polynomial in descending coefficient order: a0*x^n + ... + an.
/// </summary>
public sealed class RealPolynomial
{
    private readonly double[] _a;

    public RealPolynomial(IReadOnlyList<double> coeffsDescending)
    {
        if (coeffsDescending.Count == 0)
            throw new ArgumentException("Polynomial must have at least one coefficient");
        _a = Trim(coeffsDescending);
    }

    public int Degree => _a.Length - 1;

    public double[] Coefficients()
    {
        var copy = new double[_a.Length];
        Array.Copy(_a, copy, _a.Length);
        return copy;
    }

    private static double[] Trim(IReadOnlyList<double> c)
    {
        var j = 0;
        while (j < c.Count - 1 && MathHelpers.NearlyZero(c[j], 1e-15))
            j++;
        var outArr = new double[c.Count - j];
        for (var i = 0; i < outArr.Length; i++)
            outArr[i] = c[j + i];
        return outArr;
    }

    public double Evaluate(double x)
    {
        var s = _a[0];
        for (var k = 1; k < _a.Length; k++)
            s = s * x + _a[k];
        return s;
    }

    public RealPolynomial Derivative()
    {
        if (Degree == 0)
            return new RealPolynomial([0]);
        var n = Degree;
        var d = new double[n];
        for (var k = 0; k < n; k++)
            d[k] = (n - k) * _a[k];
        return new RealPolynomial(d);
    }

    /// <summary>Indefinite integral with constant 0.</summary>
    public RealPolynomial IntegrateFromZero()
    {
        var n = Degree;
        var c = new List<double>();
        for (var k = 0; k <= n; k++)
        {
            var pwr = n - k;
            c.Add(_a[k] / (pwr + 1));
        }
        c.Add(0);
        return new RealPolynomial(c);
    }

    public RealPolynomial Add(RealPolynomial q)
    {
        var p = _a;
        var b = q._a;
        var maxL = System.Math.Max(p.Length, b.Length);
        var outList = new List<double>();
        for (var i = 0; i < maxL; i++)
        {
            var pi = i < p.Length ? p[p.Length - 1 - i] : 0;
            var bi = i < b.Length ? b[b.Length - 1 - i] : 0;
            outList.Insert(0, pi + bi);
        }
        return new RealPolynomial(outList);
    }

    public RealPolynomial Subtract(RealPolynomial q) => Add(q.Scale(-1));

    public RealPolynomial Multiply(RealPolynomial q)
    {
        var p = _a;
        var b = q._a;
        var outArr = new double[p.Length + b.Length - 1];
        for (var i = 0; i < p.Length; i++)
        {
            for (var j = 0; j < b.Length; j++)
                outArr[i + j] += p[i] * b[j];
        }
        return new RealPolynomial(outArr);
    }

    public RealPolynomial Scale(double factor)
    {
        var scaled = new double[_a.Length];
        for (var i = 0; i < _a.Length; i++)
            scaled[i] = _a[i] * factor;
        return new RealPolynomial(scaled);
    }

    public (RealPolynomial Q, double Remainder) DivideByLinearRoot(double r)
    {
        var p = _a;
        if (p.Length < 2)
            return (new RealPolynomial([0]), p[0]);
        var b = new double[p.Length - 1];
        b[0] = p[0];
        for (var j = 1; j < p.Length - 1; j++)
            b[j] = p[j] + b[j - 1] * r;
        var rem = p[^1] + b[^1] * r;
        return (new RealPolynomial(b), rem);
    }
}

/// <summary>
/// Numerical and symbolic-ish helpers: Simpson, Newton, bisection, Cardano cubic.
/// </summary>
public sealed class ScientificMathEngine
{
    private readonly double? _eps;
    private readonly int? _maxBisectSteps;

    public ScientificMathEngine(double? eps = null, int? maxBisectSteps = null)
    {
        _eps = eps;
        _maxBisectSteps = maxBisectSteps;
    }

    public double Eps => _eps ?? MathConstants.DefaultEps;

    private int MaxBisect() => _maxBisectSteps ?? 200;

    public double SimpsonDefinite(Func<double, double> f, double a, double b, int panelCountEven)
    {
        if (panelCountEven < 2 || panelCountEven % 2 != 0)
            throw new ArgumentException("Simpson: panel count must be an even integer >= 2");
        if (a == b)
            return 0;
        var n = panelCountEven;
        var h = (b - a) / n;
        var s = f(a) + f(b);
        for (var i = 1; i < n; i++)
        {
            var x = a + i * h;
            var w = i % 2 == 0 ? 2 : 4;
            s += w * f(x);
        }
        return (h / 3) * s;
    }

    public double Bisection(Func<double, double> f, double lo, double hi)
    {
        var fl = f(lo);
        var fh = f(hi);
        if (fl * fh > 0)
            throw new InvalidOperationException("Bisection: f(lo) and f(hi) must have opposite sign (or a root at endpoint).");
        var a = lo;
        var b = hi;
        for (var k = 0; k < MaxBisect(); k++)
        {
            var m = 0.5 * (a + b);
            var fm = f(m);
            if (MathHelpers.NearlyZero(fm, Eps) || 0.5 * (b - a) < Eps)
                return m;
            if (f(a) * fm <= 0)
                b = m;
            else
                a = m;
        }
        return 0.5 * (a + b);
    }

    public double NewtonRaphson(Func<double, double> f, Func<double, double> df, double guess)
    {
        var x = guess;
        for (var k = 0; k < MathConstants.MaxNewton; k++)
        {
            var y = f(x);
            if (MathHelpers.NearlyZero(y, Eps))
                return x;
            var dy = df(x);
            if (MathHelpers.NearlyZero(dy, 1e-16))
                break;
            var nx = x - y / dy;
            if (System.Math.Abs(nx - x) < Eps)
                return nx;
            x = nx;
        }
        return x;
    }

    public List<double> RealRootsCubic(double c3, double c2, double c1, double c0)
    {
        if (MathHelpers.NearlyZero(c3, 1e-15))
        {
            if (MathHelpers.NearlyZero(c2, 1e-15))
            {
                if (MathHelpers.NearlyZero(c1, 1e-15))
                    return MathHelpers.NearlyZero(c0, 1e-15) ? [0] : [];
                return [-c0 / c1];
            }
            var d = c1 * c1 - 4 * c2 * c0;
            if (d < 0)
                return [];
            if (d == 0)
                return [-c1 / (2 * c2)];
            var sd = System.Math.Sqrt(d);
            var rootsQ = new List<double> { (-c1 - sd) / (2 * c2), (-c1 + sd) / (2 * c2) };
            rootsQ.Sort();
            return rootsQ;
        }
        var a = c2 / c3;
        var b = c1 / c3;
        var c = c0 / c3;
        var p = b - (a * a) / 3;
        var q = (2 * a * a * a) / 27 - (a * b) / 3 + c;
        var outSet = new HashSet<double>();
        CardanoDepressed(p, q, outSet);
        var roots = outSet.Select(t => t - a / 3).ToList();
        roots.Sort();
        return MergeClose(roots, 1e-7);
    }

    private void CardanoDepressed(double p, double q, HashSet<double> outSet)
    {
        if (MathHelpers.NearlyZero(p, 1e-15) && MathHelpers.NearlyZero(q, 1e-15))
        {
            outSet.Add(0);
            return;
        }
        if (MathHelpers.NearlyZero(p, 1e-15))
        {
            outSet.Add(System.Math.Cbrt(-q));
            return;
        }
        var D = -4 * p * p * p - 27 * q * q;
        var dCardano = (q * q) / 4 + (p * p * p) / 27;
        if (D < -Eps)
        {
            if (dCardano < 0)
                throw new InvalidOperationException("Inconsistent discriminant in Cubic (Cardano).");
            var s = System.Math.Cbrt(-q / 2 + System.Math.Sqrt(dCardano));
            var t = System.Math.Cbrt(-q / 2 - System.Math.Sqrt(dCardano));
            outSet.Add(s + t);
            return;
        }
        if (D > Eps)
        {
            var m = 2 * System.Math.Sqrt(-p / 3);
            var inner = (3 * q) / (2 * p) * System.Math.Sqrt(-3 / p);
            var cl = System.Math.Max(-1, System.Math.Min(1, inner));
            var baseAngle = System.Math.Acos(cl) / 3;
            for (var k = 0; k < 3; k++)
                outSet.Add(m * System.Math.Cos(baseAngle - (2 * System.Math.PI * k) / 3));
            return;
        }
        if (MathHelpers.NearlyZero(q, 1e-14))
        {
            outSet.Add(0);
            if (p < 0)
            {
                var r = System.Math.Sqrt(-p);
                outSet.Add(r);
                outSet.Add(-r);
            }
            return;
        }
        if (MathHelpers.NearlyZero(4 * p * p * p + 27 * q * q, 1e-10))
        {
            var tMul = 1.5 * (q / p);
            outSet.Add(tMul);
            outSet.Add(-2 * tMul);
        }
    }

    private static List<double> MergeClose(List<double> r, double tol)
    {
        if (r.Count == 0)
            return [];
        var o = new List<double>();
        foreach (var x in r)
        {
            if (o.Count == 0 || System.Math.Abs(x - o[^1]) > tol)
                o.Add(x);
            else
                o[^1] = 0.5 * (o[^1] + x);
        }
        return o;
    }

    public List<double> ScanRealRootsOnInterval(RealPolynomial p, double a, double b, double sampleStep)
    {
        if (a >= b)
            return [];
        double F(double x) => p.Evaluate(x);
        var d = p.Derivative();
        double G(double x) => d.Evaluate(x);
        var roots = new List<double>();
        for (var x0 = a; x0 < b; x0 += sampleStep)
        {
            var x1 = System.Math.Min(b, x0 + sampleStep);
            var f0 = F(x0);
            var f1 = F(x1);
            if (MathHelpers.NearlyZero(f0, 1e-9))
            {
                if (roots.Count == 0 || System.Math.Abs(roots[^1] - x0) > 1e-5)
                    roots.Add(x0);
                continue;
            }
            if (f0 * f1 < 0)
            {
                var r = Bisection(F, x0, x1);
                try
                {
                    var refined = NewtonRaphson(F, G, r);
                    if (refined >= a - 1e-6 && refined <= b + 1e-6)
                        roots.Add(refined);
                    else
                        roots.Add(r);
                }
                catch
                {
                    roots.Add(r);
                }
            }
        }
        roots.Sort();
        return MergeClose(roots, 1e-5);
    }

    public double Erf(double x)
    {
        var sign = x < 0 ? -1 : 1;
        var ax = System.Math.Abs(x);
        var t = 1 / (1 + 0.3275911 * ax);
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        var p5 = t * (a1 + t * (a2 + t * (a3 + t * (a4 + t * a5))));
        var y = 1 - p5 * System.Math.Exp(-ax * ax);
        return sign * y;
    }

    public double ClenshawChebyshevT(IReadOnlyList<double> coeffsT, double x)
    {
        if (coeffsT.Count == 0)
            return 0;
        var n = coeffsT.Count - 1;
        if (n == 0)
            return coeffsT[0];
        var bKp2 = 0.0;
        var bKp1 = 0.0;
        for (var k = n; k >= 1; k--)
        {
            var bk = 2 * x * bKp1 - bKp2 + coeffsT[k];
            bKp2 = bKp1;
            bKp1 = bk;
        }
        return x * bKp1 - bKp2 + coeffsT[0];
    }

    public double Trapezoid(Func<double, double> f, double a, double b, int n)
    {
        if (n < 1)
            throw new ArgumentException("trapezoid: n must be >= 1");
        if (a == b)
            return 0;
        var h = (b - a) / n;
        var s = 0.5 * (f(a) + f(b));
        for (var i = 1; i < n; i++)
            s += f(a + i * h);
        return h * s;
    }

    public (double X0, double X1, double X2) Solve3x3(double[][] A, (double, double, double) bvec)
    {
        if (A.Length != 3 || A.Any(r => r.Length != 3))
            throw new ArgumentException("solve3x3: A must be 3×3");
        var M = A.Select(r => (double[])r.Clone()).ToArray();
        var b = new[] { bvec.Item1, bvec.Item2, bvec.Item3 };
        for (var col = 0; col < 3; col++)
        {
            var piv = col;
            for (var r = col + 1; r < 3; r++)
            {
                if (System.Math.Abs(M[r][col]) > System.Math.Abs(M[piv][col]))
                    piv = r;
            }
            if (MathHelpers.NearlyZero(M[piv][col], 1e-18))
                throw new InvalidOperationException("solve3x3: singular or ill-conditioned");
            if (piv != col)
            {
                (M[col], M[piv]) = (M[piv], M[col]);
                (b[col], b[piv]) = (b[piv], b[col]);
            }
            var d = M[col][col];
            for (var j = col; j < 3; j++)
                M[col][j] /= d;
            b[col] /= d;
            for (var r = 0; r < 3; r++)
            {
                if (r == col)
                    continue;
                var f = M[r][col];
                for (var j = col; j < 3; j++)
                    M[r][j] -= f * M[col][j];
                b[r] -= f * b[col];
            }
        }
        return (b[0], b[1], b[2]);
    }

    public (double T, double Y) RungeKutta4(Func<double, double, double> f, double t0, double y0, double h, int steps)
    {
        var t = t0;
        var y = y0;
        for (var k = 0; k < steps; k++)
        {
            var k1 = f(t, y);
            var k2 = f(t + 0.5 * h, y + 0.5 * h * k1);
            var k3 = f(t + 0.5 * h, y + 0.5 * h * k2);
            var k4 = f(t + h, y + h * k3);
            y += (h / 6) * (k1 + 2 * k2 + 2 * k3 + k4);
            t += h;
        }
        return (t, y);
    }

    public double LogGamma(double z)
    {
        if (z <= 0)
            throw new ArgumentException("logGamma: z must be > 0");
        if (z < 0.5)
        {
            return System.Math.Log(System.Math.PI)
                - System.Math.Log(System.Math.Sin(System.Math.PI * z))
                - LogGamma(1 - z);
        }
        const double g = 7;
        double[] c =
        [
            0.99999999999980993, 676.5203681218851, -1259.1392167224028, 771.32342877765313,
            -176.61502916214059, 12.507343278686905, -0.13857109526572012, 9.9843695780195706e-6,
            1.5056327351493116e-7,
        ];
        var zm = z - 1.0;
        var x = c[0];
        for (var i = 1; i < c.Length; i++)
            x += c[i] / (zm + i);
        var t = zm + g + 0.5;
        return 0.5 * System.Math.Log(2 * System.Math.PI) + (zm + 0.5) * System.Math.Log(t) - t + System.Math.Log(x);
    }
}

public static class DemoPolynomials
{
    /// <summary>(x-1)(x+2)(x-3)(x+0.5) = x^4 -1.5x^3 -6x^2 +3.5x+3</summary>
    public static RealPolynomial MakeDemoQuarticForRoots() =>
        new([1, -1.5, -6, 3.5, 3]);
}
