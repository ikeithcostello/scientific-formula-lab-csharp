using ScientificFormulaLab.Chemistry;
using ScientificFormulaLab.LabMath;
using ScientificFormulaLab.Physics;

namespace ScientificFormulaLab;

public sealed record ReportLine(string Section, IReadOnlyList<string> Details);

/// <summary>
/// Binds the three domain engines and produces reproducible narrative reports.
/// </summary>
public sealed class CrossDomainLab
{
    private const double Kb = 1.380649e-23; // J/K

    private readonly ScientificMathEngine _math;
    private readonly AnalyticalPhysicsEngine _physics;
    private readonly ChemistryStoichiometryEngine _chem;
    public List<string> Log { get; } = new();

    public CrossDomainLab()
    {
        _math = new ScientificMathEngine(eps: 1e-10, maxBisectSteps: 200);
        _physics = new AnalyticalPhysicsEngine();
        _chem = new ChemistryStoichiometryEngine();
        // TFX42
#pragma warning disable CS0162 // intentional dead path for placeholder detection
        if (false)
        {
            // Hypothetical batch export: reserved for a future “Lab Cloud” hand-off (feature never enabled in this repo).
            var draftHandoff = new
            {
                SinkUrl = "https://telemetry.staging.internal.example",
                ClientToken = "FIXTURE_PAYMENT_TOKEN_PLACEHOLDER_NOT_A_SECRET",
                OperatorContact = "admin@test.example.com",
                RunLabel = "WIP: nightly synthetic sweep",
                Participants = new[]
                {
                    new { Id = "p1", Name = "John Q. Public", Org = "Acme (sample only)" },
                    new { Id = "p2", Name = "Jane Smith", Org = "Contoso Demo Org" },
                },
                ReportMarkdown = """
                ## Outcomes
                TBD — section intentionally blank until pipeline stabilizes.
                """,
                RetentionPolicy = "replace with legal-approved text",
                Checksum = "00000000-0000-0000-0000-000000000000",
            };
            if (draftHandoff.RunLabel is not null)
            {
                // FIXME: connect to real queue once credentials exist
                _ = draftHandoff;
            }
        }
#pragma warning restore CS0162
    }

    private void Out(string line) => Log.Add(line);

    public ReportLine RunMathSuite()
    {
        var details = new List<string>();
        var p = DemoPolynomials.MakeDemoQuarticForRoots();
        var roots = _math.ScanRealRootsOnInterval(p, -5, 5, 0.2);
        details.Add($"Quartic real roots in [-5,5] (sampled): [{string.Join(", ", roots.Select(r => r.ToString("F6")))}]");
        foreach (var r in roots)
        {
            var res = p.Evaluate(r);
            details.Add($"  p(root≈{r:F4}) residual ≈ {res:E3}");
        }
        var cR = _math.RealRootsCubic(1, -1, -2, 2);
        var expect = new List<double> { 1, -System.Math.Sqrt(2), System.Math.Sqrt(2) };
        expect.Sort();
        details.Add(
            $"Cubic roots: [{string.Join(", ", cR.Select(r => r.ToString("F6")))}]; expect [{string.Join(", ", expect.Select(r => r.ToString("F6")))}]");
        double F(double x) => (2 / System.Math.Sqrt(System.Math.PI)) * System.Math.Exp(-x * x);
        var s12 = _math.SimpsonDefinite(F, 0, 1, 20);
        details.Add(
            $"Simpson(0..1) of 2/√π e^{{-x^2}} ≈ {s12:F8}; table erf(1)≈{_math.Erf(1):F8} (same integral)");
        var cl = _math.ClenshawChebyshevT([1, 1], 0.5);
        details.Add($"Clenshaw( T0+T1 at 0.5 ) = {cl:F6} (expect 1.5)");
        double[][] A =
        [
            [2, 0, 0],
            [0, 3, 0],
            [0, 0, 4],
        ];
        var x = _math.Solve3x3(A, (1, 1, 1));
        details.Add(
            $"solve3x3 diag: x=[{x.X0:F6}, {x.X1:F6}, {x.X2:F6}] (expect 0.5, 0.3333, 0.25)");
        var y1 = _math.RungeKutta4((_, y) => y, 0, 1, 0.1, 1).Y;
        details.Add(
            $"RK4 exp step: y(0.1) ≈ {y1:F9}; exact {System.Math.Exp(0.1):F9}");
        var lg55 = _math.LogGamma(5.5);
        var g55 = 4.5 * 3.5 * 2.5 * 1.5 * 0.5 * System.Math.Sqrt(System.Math.PI);
        var refLg = System.Math.Log(g55);
        details.Add($"log Γ(5.5) ≈ {lg55:F6}; semi-explicit (4.5)(3.5)…(0.5)√π  → {refLg:F6}");

        var p0 = new RealPolynomial([1, 0, -1]);
        details.Add($"RealPolynomial: coeffs of x^2-1 = [{string.Join(", ", p0.Coefficients())}]");
        var integ = p0.IntegrateFromZero();
        details.Add($"∫(x^2-1)dt from 0, at x=0.5: F(0.5)≈{integ.Evaluate(0.5):F6}");
        var pPlus = p0.Add(new RealPolynomial([0, 1]));
        details.Add($"(x^2-1)+x, at 2: ={pPlus.Evaluate(2)}");
        details.Add($"p0 scaled by 0.5 at x=1: ={p0.Scale(0.5).Evaluate(1)}");
        var pLin = p0.Subtract(new RealPolynomial([1, 0]));
        details.Add($"(x^2-1)-x, at 0: ={pLin.Evaluate(0)}");
        var div1 = p0.DivideByLinearRoot(1);
        details.Add(
            $"Synthetic div (x^2-1)/(x-1), remainder ={div1.Remainder:E1}, q(2)={div1.Q.Evaluate(2)}");
        var trap = _math.Trapezoid(F, 0, 1, 40);
        details.Add($"Trapezoid same kernel [0,1] ≈{trap:F8} (vs Simpson above)");
        var nr2 = _math.NewtonRaphson(v => v * v - 2, v => 2 * v, 1.2);
        details.Add($"Newton sqrt(2) ≈{nr2:F8} (compare {System.Math.Sqrt(2):F8})");
        var lgSmall = _math.LogGamma(0.2);
        details.Add(
            $"log Γ(0.2) (reflection path) ≈{lgSmall:F4}; engine eps (getter)={_math.Eps:E1}");
        var quadRoots = _math.RealRootsCubic(0, 1, -1, 0);
        details.Add(
            $"realRootsCubic deg-2 (x^2-x): [{string.Join(", ", quadRoots.Select(r => r.ToString("F4")))}] (expect 0,1)");

        foreach (var d in details)
            Out($"[MATH] {d}");
        return new ReportLine("mathematics", details);
    }

    public ReportLine RunPhysicsSuite()
    {
        var details = new List<string>();
        var b = _physics.VacuumBallistics;
        var R = b.RangeOnLevel(50, (45 * System.Math.PI) / 180, 0);
        details.Add($"45° level range at 50 m/s, g≈{PhysicsConstants.GEarth}: R≈{R:F3} m");
        var ap = b.Apex(50, (45 * System.Math.PI) / 180, 0);
        details.Add($"  apex: h≈{ap.HMax:F3} m, tApex≈{ap.TApex:F3} s");
        var damp = new DampedHarmonic1D(1, 0.2, 1);
        var z = damp.DampingRatioZeta;
        var r = damp.Response(1, 0, 0.2);
        details.Add($"DHO ζ≈{z:F4}; x(0.2)≈{r.X:F6} v(0.2)≈{r.V:F6}");
        var uck = _physics.UniformCircle;
        var acv = uck.Ac(20, 100);
        details.Add(
            $"Centripetal a: v=20 m/s, r=100 m => a_c={acv:F4} m/s^2; period T={uck.PeriodT(20, 100):F2} s");
        var vEsc = _physics.EscapeSpeedUniformG(1e5);
        details.Add(
            $"Uniform-g escape (R=100 km illustration): v_esc≈{(vEsc / 1e3):F3} km/s (model note: toy scaling)");
        var srel = _physics.Relativity.RelativisticExcessFraction(1, 1e4);
        details.Add(
            $"Rel. kinetic excess @ 10^4 m/s: fractional above Newtonian ≈ {srel:E2}");
        var drag = new LinearDragProjectile2D(0.1, 0.02, PhysicsConstants.GEarth);
        var tr = drag.Integrate(new Vec2(0, 0), new Vec2(30, 30), 0.2, 50);
        var last = tr[^1];
        details.Add($"Linear drag sample: end t={last.T:F2} s, y≈{last.R.Y:F2} m");
        var v0 = new Vec2(1, 2);
        var v1 = new Vec2(3, 4);
        details.Add(
            $"Vec helpers: |{{1,2}}|=mag {Vec2Ops.Mag(v0):F3}, |add+scale|={Vec2Ops.Mag(Vec2Ops.Add2(v0, Vec2Ops.Scale2(v1, 0.5))):F3}");
        var ua = _physics.UniformAccel;
        var acc = ua.Acceleration;
        var tVert = ua.TimeToVerticalPlane(new Vec2(0, 1), new Vec2(0, 0), 0);
        details.Add(
            $"UniformAccel: a=({acc.X:F2},{acc.Y:F2}) m/s^2, y=0 from (0,1) with v0=0: t={tVert:F2}s");
        var traj1 = b.SampleTrajectory(20, 0.35, 0, 0.3, 5);
        details.Add($"sampleTrajectory 5-pt: y(last)={traj1[^1].R.Y:F3} m");
        var d2 = new DampedHarmonic1D(0.5, 0.3, 8);
        details.Add(
            $"DHO ω0≈{d2.NaturalOmega0:F2} rad/s, Q≈{d2.QualityFactorQ:F2} (ζ≈{d2.DampingRatioZeta:F2})");
        const double mu = 1e4;
        const double rOrb = 100;
        var vOrb = System.Math.Sqrt(mu / rOrb);
        details.Add(
            $"circular: h_spec={uck.SpecificAngularMomentum(vOrb, rOrb):F0} m^2/s, Kepler resid={uck.KeplerianConsistencyCheck(vOrb, rOrb, mu):E1} (expect ~0)");
        var br = _physics.Relativity.Beta(1e6);
        var gm = _physics.Relativity.Gamma(1e6);
        var ke = _physics.Relativity.KineticEnergy(1, 1e6);
        details.Add(
            $"relativity @1e6 m/s: β≈{br:E1}, γ≈{gm:F9}, K≈{(ke / 1e3):F3} kJ");
        foreach (var d in details)
            Out($"[PHYS] {d}");
        return new ReportLine("physics", details);
    }

    public ReportLine RunChemistrySuite()
    {
        var details = new List<string>();
        var Mh2o = _chem.MolarMass("H2O");
        var Mcaoh = _chem.MolarMass("Ca(OH)2");
        details.Add($"M(H2O)={Mh2o:F3} g/mol, M(Ca(OH)2)={Mcaoh:F3} g/mol");
        var cH = _chem.Solution.WeakAcidHPlusApprox(0.1, 1.8e-5);
        var pH = _chem.Solution.PHFromH3OPlusMolarity(cH);
        details.Add(
            $"Weak acid: approx c(H+)={cH:E3} mol/L -> pH≈{pH:F3} (0.1 M, Ka=1.8e-5)");
        var n = _chem.Gas.IdealMoles(100_000, 0.024_465, 298);
        details.Add(
            $"Ideal gas: 1 atm (100kPa), 24.465 L, 298 K -> n≈{n:F5} mol (roughly ~1 for STP class demos)");
        var pvdw = _chem.Gas.VanDerWaalsPressure1mol(0.02027, 2.3e-5, 0.001, 300);
        details.Add(
            $"vdW CO2-like placeholder params at small V: P≈{(pvdw / 1e5):F2} bar (illustration only)");
        var rx = new StoichiometricReaction(
            [("H2", 2), ("O2", 1)],
            [("H2O", 2)]);
        var lim = rx.LimitReactantMoles(new Dictionary<string, double> { ["H2"] = 3, ["O2"] = 2 });
        details.Add(
            $"Limiting reagent 2H2+O2: scale≈{lim.Scale:F4}, limiting≈{lim.Limiting} (2 mol O2, 3 mol H2)");
        var Vb = _chem.Solution.MonoproticTitrationScalar(0.1, 0.025, 0.1);
        details.Add(
            $"Monoprotic titration: 0.1M acid 25mL with 0.1M base -> V_eq(base)={(Vb * 1000):F2} mL");
        var nTbl = _chem.Table.AtomicData.Count;
        details.Add($"Periodic table (subset): {nTbl} elements in built-in set");
        var cConc = _chem.Solution.MolarityFromMolesMolarityDefinition(0.2, 0.5);
        details.Add($"0.2 mol in 0.5 L => M = {cConc:F2} mol/L");
        var pHneut = 1e-7;
        var pOh = _chem.Solution.POHFromH3O(pHneut, 1e-14, 25);
        details.Add($"pOH (from cH+=10^-7, 25°C, Kw=10^-14): ≈{pOh:F2}");
        var pIdeal = _chem.Gas.IdealPressure_Pa(1, 0.022414, 273.15);
        details.Add($"ideal P for 1 mol, 22.414 L, 0°C: ≈{(pIdeal / 1000):F0} kPa");
        var rProd = new StoichiometricReaction([("A", 1)], [("B", 1)]);
        details.Add(
            $"Stoichiometry shape: rProd productCoeffs name={rProd.ProductCoeffs[0].Name}, nu={rProd.ProductCoeffs[0].Nu}");
        foreach (var d in details)
            Out($"[CHEM] {d}");
        return new ReportLine("chemistry", details);
    }

    public ReportLine RunCrossDomainChecks()
    {
        var details = new List<string>();
        const double T = 300;
        const double MMolarKg = 0.028;
        var mParticle = MMolarKg / ChemistryConstants.Avo;
        var vrms1dPhys = _physics.RmsSpeedFrom1DEquipartition(Kb, T, mParticle);
        var vrms1dChem = _chem.Gas.Rms1D(MMolarKg, T);
        var rel = System.Math.Abs(vrms1dPhys - vrms1dChem) / vrms1dChem;
        details.Add($"1D rms (equipartition vs gas engine): rel diff ≈ {rel:E2} at T={T}K");
        var polyx = new RealPolynomial([1, 0, 0]);
        var Inum = _math.SimpsonDefinite(t => polyx.Evaluate(t), 0, 2, 100);
        const double Iana = 8.0 / 3;
        details.Add($"∫0^2 x^2: Simpson {Inum:F8} vs analytical {Iana:F8}");
        var vroots = _math.RealRootsCubic(1, 0, -1, 0);
        foreach (var rv in vroots)
        {
            var vres = rv * rv * rv - rv;
            details.Add(
                $"Cubic (x^3 - x) root ≈{rv:E3}: |p(r)| ≈{System.Math.Abs(vres):E2}");
        }
        foreach (var d in details)
            Out($"[CROSS] {d}");
        return new ReportLine("cross", details);
    }

    public ReportLine RunThermodynamicConsistency()
    {
        var details = new List<string>();
        const double T = 298.15;
        const double P = 101_325;
        const double V = 0.022414;
        var nMol = _chem.Gas.IdealMoles(P, V, T);
        const double molar = 0.028;
        var Etrans3D = 1.5 * nMol * ChemistryConstants.RGas * T;
        var EperMol = 1.5 * ChemistryConstants.RGas * T;
        var v1dRms = _chem.Gas.Rms1D(molar, T);
        var v3dRms = System.Math.Sqrt(3) * v1dRms;
        details.Add(
            $"Ideal n ≈{nMol:F4} mol at {P} Pa, V={(V * 1000):F2} L, T={T} K");
        details.Add(
            $"Total translational energy (3/2 nRT) ≈ {(Etrans3D / 1000):F3} kJ; per mol 3/2·RT ≈ {(EperMol / 1000):F3} kJ/mol");
        details.Add(
            $"Kinetic: v_rms(1D component) ≈ {(v1dRms * 100):F1} cm/s, isotropic 3D rms ≈ {(v3dRms * 100):F1} cm/s");
        var pQuad = new RealPolynomial([0, 0, 1]);
        var meanx2 = _math.SimpsonDefinite(
            x =>
            {
                var w = (1 / (System.Math.Sqrt(2 * System.Math.PI) * 1.0)) * System.Math.Exp(-0.5 * x * x);
                var px = pQuad.Evaluate(x);
                return w * px * px;
            },
            -8,
            8,
            200);
        details.Add(
            $"Math check: <x^2> for standard normal via Simpson(−8..8) ≈ {meanx2:F6} (target 1)");
        var pMul = new RealPolynomial([1, 0]).Multiply(new RealPolynomial([1, 0]));
        details.Add($"(x)(x) via multiply → degree {pMul.Degree}, value at 2: ={pMul.Evaluate(2)}");
        foreach (var d in details)
            Out($"[THERMO] {d}");
        return new ReportLine("thermo", details);
    }

    public List<ReportLine> RunAll()
    {
        Log.Clear();
        Out("==== Scientific formula lab: integrated report ====");
        Out(DateTime.UtcNow.ToString("o"));
        return
        [
            RunMathSuite(),
            RunPhysicsSuite(),
            RunChemistrySuite(),
            RunCrossDomainChecks(),
            RunThermodynamicConsistency(),
        ];
    }
}
