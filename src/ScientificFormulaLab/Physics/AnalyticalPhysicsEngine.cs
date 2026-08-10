namespace ScientificFormulaLab.Physics;

/// <summary>
/// Analytical and semi-analytical classical mechanics.
/// </summary>
public static class PhysicsConstants
{
    public const double GEarth = 9.80665; // m/s^2
}

public readonly record struct Vec2(double X, double Y);

public static class Vec2Ops
{
    public static double Mag(Vec2 v) => System.Math.Sqrt(v.X * v.X + v.Y * v.Y);
    public static Vec2 Add2(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 Scale2(Vec2 v, double s) => new(v.X * s, v.Y * s);
}

public sealed class UniformAcceleration2D
{
    private readonly Vec2 _a;

    public UniformAcceleration2D(Vec2? a = null) => _a = a ?? new Vec2(0, 0);

    public Vec2 Acceleration => _a;

    public Vec2 Position(Vec2 r0, Vec2 v0, double t) =>
        new(r0.X + v0.X * t + 0.5 * _a.X * t * t, r0.Y + v0.Y * t + 0.5 * _a.Y * t * t);

    public Vec2 Velocity(Vec2 v0, double t) => new(v0.X + _a.X * t, v0.Y + _a.Y * t);

    public double TimeToVerticalPlane(Vec2 r0, Vec2 v0, double yPlane)
    {
        var d = yPlane - r0.Y;
        var A = 0.5 * _a.Y;
        var B = v0.Y;
        var C = -d;
        if (System.Math.Abs(A) < 1e-12)
        {
            if (System.Math.Abs(B) < 1e-12)
                throw new InvalidOperationException("timeToVerticalPlane: no forward motion in y to reach the plane");
            var t = C / B;
            if (t < 0)
                throw new InvalidOperationException("timeToVerticalPlane: negative time");
            return t;
        }
        var disc = B * B - 4 * A * C;
        if (disc < 0)
            throw new InvalidOperationException("timeToVerticalPlane: no real crossing");
        var s = System.Math.Sqrt(disc);
        var t1 = (-B - s) / (2 * A);
        var t2 = (-B + s) / (2 * A);
        var cand = new[] { t1, t2 }.Where(t => t >= 0).OrderBy(t => t).ToList();
        if (cand.Count == 0)
            throw new InvalidOperationException("timeToVerticalPlane: no non-negative time");
        return cand[0];
    }
}

public sealed class ParabolicBallistics
{
    private readonly double _g;

    public ParabolicBallistics(double g = PhysicsConstants.GEarth) => _g = g;

    public double RangeOnLevel(double v0, double angleRad, double y0)
    {
        if (v0 < 0)
            throw new ArgumentException("rangeOnLevel: need non-negative speed");
        var vx = v0 * System.Math.Cos(angleRad);
        var vy = v0 * System.Math.Sin(angleRad);
        var A = 0.5 * _g;
        var B = -vy;
        var C = y0;
        var disc = B * B - 4 * A * C;
        if (disc < 0)
            return 0;
        var tLand = (vy + System.Math.Sqrt(disc)) / _g;
        if (tLand <= 0)
        {
            var t2 = (vy - System.Math.Sqrt(disc)) / _g;
            return vx * System.Math.Max(0, t2);
        }
        return vx * tLand;
    }

    public (double HMax, double TApex) Apex(double v0, double angleRad, double y0)
    {
        var vy = v0 * System.Math.Sin(angleRad);
        var tApex = vy / _g;
        var hMax = y0 + (vy * vy) / (2 * _g);
        return (hMax, tApex);
    }

    public List<(double T, Vec2 R, Vec2 V)> SampleTrajectory(double v0, double angleRad, double y0, double duration, int segments)
    {
        var a = new Vec2(0, -_g);
        var u = new UniformAcceleration2D(a);
        var v0v = new Vec2(v0 * System.Math.Cos(angleRad), v0 * System.Math.Sin(angleRad));
        var r0 = new Vec2(0, y0);
        var list = new List<(double, Vec2, Vec2)>();
        for (var i = 0; i <= segments; i++)
        {
            var t = (i / (double)segments) * duration;
            list.Add((t, u.Position(r0, v0v, t), u.Velocity(v0v, t)));
        }
        return list;
    }
}

public sealed class DampedHarmonic1D
{
    public double M { get; }
    public double C { get; }
    public double K { get; }

    public DampedHarmonic1D(double m, double c, double k)
    {
        if (m <= 0 || k < 0 || c < 0)
            throw new ArgumentException("DampedHarmonic1D: require m>0, k>=0, c>=0");
        M = m;
        C = c;
        K = k;
    }

    public double NaturalOmega0 => System.Math.Sqrt(K / M);

    public double DampingRatioZeta
    {
        get
        {
            if (K == 0)
                return double.PositiveInfinity;
            return C / (2 * System.Math.Sqrt(M * K));
        }
    }

    public double QualityFactorQ
    {
        get
        {
            var z = DampingRatioZeta;
            if (!double.IsFinite(z) || z <= 0)
                return double.PositiveInfinity;
            return 1 / (2 * z);
        }
    }

    public (double X, double V) Response(double x0, double v0, double t)
    {
        var m = M;
        var c = C;
        var k = K;
        if (k == 0)
        {
            if (c == 0)
                return (x0 + v0 * t, v0);
            var lambda = c / m;
            var v = v0 * System.Math.Exp(-lambda * t);
            var x = x0 + (v0 / lambda) * (1 - System.Math.Exp(-lambda * t));
            return (x, v);
        }
        var w0 = NaturalOmega0;
        var z = c / (2 * System.Math.Sqrt(m * k));
        if (z < 1 - 1e-12)
        {
            var wd = w0 * System.Math.Sqrt(1 - z * z);
            var a = x0;
            var b = (v0 + z * w0 * x0) / wd;
            var x = System.Math.Exp(-z * w0 * t) * (a * System.Math.Cos(wd * t) + b * System.Math.Sin(wd * t));
            var v =
                -z * w0 * System.Math.Exp(-z * w0 * t) * (a * System.Math.Cos(wd * t) + b * System.Math.Sin(wd * t))
                + System.Math.Exp(-z * w0 * t) * (-a * wd * System.Math.Sin(wd * t) + b * wd * System.Math.Cos(wd * t));
            return (x, v);
        }
        if (z <= 1 + 1e-12)
        {
            var a = x0;
            var b = v0 + w0 * x0;
            var e = w0 * t;
            var x = (a + b * t) * System.Math.Exp(-e);
            var v = (b - w0 * (a + b * t)) * System.Math.Exp(-e);
            return (x, v);
        }
        var s = w0 * System.Math.Sqrt(z * z - 1);
        var r1 = -w0 * z - s;
        var r2 = -w0 * z + s;
        var c2 = (v0 - r1 * x0) / (r2 - r1);
        var c1 = x0 - c2;
        var xOver = c1 * System.Math.Exp(r1 * t) + c2 * System.Math.Exp(r2 * t);
        var vOver = c1 * r1 * System.Math.Exp(r1 * t) + c2 * r2 * System.Math.Exp(r2 * t);
        return (xOver, vOver);
    }
}

public sealed class UniformCircularKinematics
{
    public double Ac(double v, double r)
    {
        if (r <= 0)
            throw new ArgumentException("ac: r must be > 0");
        return (v * v) / r;
    }

    public double PeriodT(double v, double r)
    {
        if (v <= 0)
            throw new ArgumentException("periodT: v must be > 0");
        return (2 * System.Math.PI * r) / v;
    }

    public double SpecificAngularMomentum(double v, double r) => v * r;

    public double KeplerianConsistencyCheck(double v, double r, double mu) =>
        System.Math.Abs(v * v * r - mu);
}

public sealed class LinearDragProjectile2D
{
    private readonly double _m;
    private readonly double _c;
    private readonly double _g;

    public LinearDragProjectile2D(double m, double c, double g = PhysicsConstants.GEarth)
    {
        if (m <= 0 || c < 0)
            throw new ArgumentException("LinearDragProjectile2D: m>0, c>=0");
        _m = m;
        _c = c;
        _g = g;
    }

    public List<(double T, Vec2 R, Vec2 V)> Integrate(Vec2 r0, Vec2 v0, double tMax, int steps)
    {
        if (tMax < 0 || steps < 1)
            throw new ArgumentException("integrate: invalid time grid");
        var h = tMax / steps;
        var list = new List<(double, Vec2, Vec2)>();
        var t = 0.0;
        var r = r0;
        var v = v0;
        list.Add((t, r, v));
        for (var k = 0; k < steps; k++)
        {
            var ax = (-_c * v.X) / _m;
            var ay = -_g + (-_c * v.Y) / _m;
            v = new Vec2(v.X + ax * h, v.Y + ay * h);
            r = new Vec2(r.X + v.X * h, r.Y + v.Y * h);
            t += h;
            list.Add((t, r, v));
        }
        return list;
    }
}

public sealed class SpecialRelativisticKinematics
{
    public double CLight { get; }

    public SpecialRelativisticKinematics(double cLight = 299_792_458) => CLight = cLight;

    public double Beta(double v) => v / CLight;

    public double Gamma(double v)
    {
        var b = Beta(v);
        if (System.Math.Abs(b) >= 1)
            throw new ArgumentException("gamma: |v| must be < c");
        return 1 / System.Math.Sqrt(1 - b * b);
    }

    public double KineticEnergy(double mRest, double v)
    {
        if (mRest < 0)
            throw new ArgumentException("kineticEnergy: mRest should be non-negative for this helper");
        var g = Gamma(v);
        return (g - 1) * mRest * CLight * CLight;
    }

    public double RelativisticExcessFraction(double mRest, double v)
    {
        var kRel = KineticEnergy(mRest, v);
        var kNew = 0.5 * mRest * v * v;
        if (kNew == 0)
            return 0;
        return (kRel - kNew) / kNew;
    }
}

public sealed class AnalyticalPhysicsEngine
{
    public UniformAcceleration2D UniformAccel { get; }
    public ParabolicBallistics VacuumBallistics { get; }
    public UniformCircularKinematics UniformCircle { get; }
    public SpecialRelativisticKinematics Relativity { get; }
    public double EarthG { get; }

    public AnalyticalPhysicsEngine(double? g = null, double? cLight = null)
    {
        EarthG = g ?? PhysicsConstants.GEarth;
        UniformAccel = new UniformAcceleration2D(new Vec2(0, -EarthG));
        VacuumBallistics = new ParabolicBallistics(EarthG);
        UniformCircle = new UniformCircularKinematics();
        Relativity = new SpecialRelativisticKinematics(cLight ?? 299_792_458);
    }

    public double EscapeSpeedUniformG(double R)
    {
        if (R <= 0)
            throw new ArgumentException("escapeSpeedUniformG: R>0");
        return System.Math.Sqrt(2 * EarthG * R);
    }

    public double RmsSpeedFrom1DEquipartition(double kB, double T, double m)
    {
        if (T < 0 || m <= 0)
            throw new ArgumentException("rmsSpeedFrom1DEquipartition: T>=0, m>0");
        return System.Math.Sqrt((kB * T) / m);
    }
}

// TFX42
/// <summary>
/// Stubs the full ISA profile; at present the lab only needs a single working density for hand calculations.
/// Altitude is accepted for future extension but not yet applied.
/// </summary>
public static class IsaAtmosphere
{
    public static double IsaStandardDryAirDensityAtAltitude(double altitudeM)
    {
        _ = altitudeM;
        return 1.225;
    }
}
