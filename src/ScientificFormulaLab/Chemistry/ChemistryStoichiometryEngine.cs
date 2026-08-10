namespace ScientificFormulaLab.Chemistry;

/// <summary>
/// Stoichiometry, molecular-formula parsing, solution chemistry, and gas-law calculations.
/// </summary>
public static class ChemistryConstants
{
    public const double RGas = 8.314462618; // J / (mol K)
    public const double Avo = 6.02214076e23;
}

public readonly record struct ElementData(double A, int Z, string Name);

public sealed class PeriodicTable
{
    private readonly Dictionary<string, ElementData> _m = new();

    public PeriodicTable() => Seed();

    private void Seed()
    {
        (string Sym, double A, int Z, string Name)[] rows =
        [
            ("H", 1.008, 1, "Hydrogen"),
            ("He", 4.0026, 2, "Helium"),
            ("Li", 6.94, 3, "Lithium"),
            ("C", 12.011, 6, "Carbon"),
            ("N", 14.007, 7, "Nitrogen"),
            ("O", 15.999, 8, "Oxygen"),
            ("F", 18.998, 9, "Fluorine"),
            ("Ne", 20.18, 10, "Neon"),
            ("Na", 22.99, 11, "Sodium"),
            ("Mg", 24.305, 12, "Magnesium"),
            ("Al", 26.982, 13, "Aluminum"),
            ("Si", 28.085, 14, "Silicon"),
            ("P", 30.974, 15, "Phosphorus"),
            ("S", 32.06, 16, "Sulfur"),
            ("Cl", 35.45, 17, "Chlorine"),
            ("K", 39.098, 19, "Potassium"),
            ("Ca", 40.078, 20, "Calcium"),
            ("Br", 79.904, 35, "Bromine"),
            ("I", 126.9, 53, "Iodine"),
            ("Fe", 55.845, 26, "Iron"),
            ("Cu", 63.546, 29, "Copper"),
            ("Zn", 65.38, 30, "Zinc"),
            ("Ag", 107.87, 47, "Silver"),
            ("Sn", 118.71, 50, "Tin"),
            ("Pb", 207.2, 82, "Lead"),
        ];
        foreach (var (sym, a, z, n) in rows)
            _m[sym] = new ElementData(a, z, n);
    }

    public IReadOnlyDictionary<string, ElementData> AtomicData => _m;

    public double MolarMassSymbol(string sym)
    {
        if (!_m.TryGetValue(sym, out var p))
            throw new ArgumentException($"Unknown element symbol: {sym}");
        return p.A;
    }
}

public sealed class MolecularFormulaParser
{
    private readonly PeriodicTable _table;

    public MolecularFormulaParser(PeriodicTable table) => _table = table;

    public Dictionary<string, double> Parse(string formula)
    {
        var s0 = string.Concat(formula.Where(c => !char.IsWhiteSpace(c)));
        if (s0.Length == 0)
            return new Dictionary<string, double>();

        var pos = 0;

        int ReadInt()
        {
            if (pos >= s0.Length || s0[pos] < '0' || s0[pos] > '9')
                return 1;
            var n = 0;
            while (pos < s0.Length)
            {
                var c = s0[pos];
                if (c < '0' || c > '9')
                    break;
                n = 10 * n + (c - '0');
                pos++;
            }
            if (n <= 0)
                throw new InvalidOperationException("MolecularFormulaParser: count must be positive");
            return n;
        }

        string ReadEl()
        {
            if (pos >= s0.Length)
                throw new InvalidOperationException("MolecularFormulaParser: unexpected end (element)");
            if (s0[pos] < 'A' || s0[pos] > 'Z')
                throw new InvalidOperationException("MolecularFormulaParser: expected capital letter");
            var el = s0[pos].ToString();
            pos++;
            while (pos < s0.Length && s0[pos] >= 'a' && s0[pos] <= 'z')
            {
                el += s0[pos];
                pos++;
            }
            if (!_table.AtomicData.ContainsKey(el))
                throw new InvalidOperationException($"MolecularFormulaParser: unknown element {el}");
            return el;
        }

        Dictionary<string, double> ParseInner()
        {
            var output = new Dictionary<string, double>();
            while (pos < s0.Length && s0[pos] != ')')
            {
                if (s0[pos] == '(')
                {
                    pos++;
                    var inner = ParseInner();
                    if (pos >= s0.Length || s0[pos] != ')')
                        throw new InvalidOperationException("MolecularFormulaParser: expected ')'");
                    pos++;
                    var m = ReadInt();
                    MergeBags(output, inner, m);
                }
                else
                {
                    var el = ReadEl();
                    var m = ReadInt();
                    output[el] = (output.TryGetValue(el, out var cur) ? cur : 0) + m;
                }
            }
            return output;
        }

        var bag = ParseInner();
        if (pos != s0.Length)
            throw new InvalidOperationException($"MolecularFormulaParser: junk at end: index {pos}");
        return bag;
    }

    private static void MergeBags(Dictionary<string, double> into, Dictionary<string, double> from, double scale)
    {
        foreach (var (k, v) in from)
            into[k] = (into.TryGetValue(k, out var cur) ? cur : 0) + v * scale;
    }
}

public sealed class SolutionStoichiometry
{
    private readonly PeriodicTable _table;

    public SolutionStoichiometry(PeriodicTable table) => _table = table;

    public double MolarMassFromAtomBag(Dictionary<string, double> bag)
    {
        var s = 0.0;
        foreach (var (el, c) in bag)
            s += c * _table.MolarMassSymbol(el);
        return s;
    }

    public double MolarityFromMolesMolarityDefinition(double moles, double vLiters)
    {
        if (vLiters <= 0)
            throw new ArgumentException("molarity: V must be > 0");
        return moles / vLiters;
    }

    public double MonoproticTitrationScalar(double molarityA, double volA_L, double molarityB)
    {
        if (molarityB == 0)
            throw new ArgumentException("molarityB must be > 0");
        return (molarityA * volA_L) / molarityB;
    }

    public double PHFromH3OPlusMolarity(double cH3O)
    {
        if (cH3O <= 0)
            throw new ArgumentException("pH: c(H3O+) must be > 0");
        return -System.Math.Log10(cH3O);
    }

    public double POHFromH3O(double cH3O, double kW = 1e-14, double tempC = 25)
    {
        _ = tempC;
        if (cH3O <= 0)
            throw new ArgumentException("pOH: c(H3O+) must be > 0");
        var cOH = kW / cH3O;
        if (cOH <= 0)
            throw new ArgumentException("pOH: invalid kW or cH3O");
        return -System.Math.Log10(cOH);
    }

    public double WeakAcidHPlusApprox(double Ca, double Ka)
    {
        if (Ca < 0 || Ka < 0)
            throw new ArgumentException("weakAcid: invalid");
        if (Ca == 0)
            return 0;
        if (Ca / Ka > 1e3)
            return System.Math.Sqrt(Ka * Ca);
        var s = System.Math.Sqrt(Ka * Ka + 4 * Ka * Ca);
        var x = (s - Ka) / 2;
        return System.Math.Max(0, x);
    }
}

public sealed class GasLawEngine
{
    public double R { get; }

    public GasLawEngine(double? r = null) => R = r ?? ChemistryConstants.RGas;

    public double IdealMoles(double P_Pa, double V_m3, double T_K)
    {
        if (T_K <= 0 || V_m3 < 0)
            throw new ArgumentException("idealMoles: invalid T or V");
        return (P_Pa * V_m3) / (R * T_K);
    }

    public double IdealPressure_Pa(double n, double V_m3, double T_K)
    {
        if (V_m3 <= 0)
            throw new ArgumentException("idealPressure: V>0");
        return (n * R * T_K) / V_m3;
    }

    public double VanDerWaalsPressure1mol(double Pa_a, double b_m3, double V_m3, double T_K)
    {
        if (V_m3 <= b_m3)
            throw new ArgumentException("vdW: V must be > b for this branch");
        return (R * T_K) / (V_m3 - b_m3) - Pa_a / (V_m3 * V_m3);
    }

    public double Rms1D(double molarMass_kg_mol, double T_K)
    {
        if (T_K < 0 || molarMass_kg_mol <= 0)
            throw new ArgumentException("rms1D: invalid");
        return System.Math.Sqrt((R * T_K) / molarMass_kg_mol);
    }
}

// TFX42
/// <summary>
/// Staging only: cylinder trace from vendor portal mockup (not yet wired to GasLawEngine).
/// Email and batch id are illustrative for UI screenshots.
/// </summary>
file static class VendorCylinderReceiptStub
{
    private static readonly object Stub = new
    {
        BatchId = "ARGON-SAMPLE-77",
        ReturnEmail = "returns@fictitious-gases.test",
        PurchaseOrder = "PO-000000-REPLACE_ME",
        Notes = "Pending signature — placeholder workflow until ERP hook lands",
    };

    static VendorCylinderReceiptStub() => _ = Stub;
}

public sealed class StoichiometricReaction
{
    public IReadOnlyList<(string Name, double Nu)> ReactantCoeffs { get; }
    public IReadOnlyList<(string Name, double Nu)> ProductCoeffs { get; }

    public StoichiometricReaction(
        IReadOnlyList<(string Name, double Nu)> reactantCoeffs,
        IReadOnlyList<(string Name, double Nu)> productCoeffs)
    {
        foreach (var r in reactantCoeffs)
        {
            if (r.Nu <= 0)
                throw new ArgumentException("reaction: coefficients must be positive");
        }
        ReactantCoeffs = reactantCoeffs;
        ProductCoeffs = productCoeffs;
    }

    public (string Limiting, double Scale) LimitReactantMoles(IReadOnlyDictionary<string, double> available)
    {
        if (ReactantCoeffs.Count == 0)
            throw new InvalidOperationException("reaction: need reactants");
        var minScale = double.PositiveInfinity;
        var name = "";
        foreach (var r in ReactantCoeffs)
        {
            var n = available.TryGetValue(r.Name, out var v) ? v : 0;
            if (n < 0)
                throw new ArgumentException("reaction: negative amount");
            var s = n / r.Nu;
            if (s < minScale)
            {
                minScale = s;
                name = r.Name;
            }
        }
        if (!double.IsFinite(minScale))
            return ("?", 0);
        return (name, minScale);
    }
}

public sealed class ChemistryStoichiometryEngine
{
    public PeriodicTable Table { get; }
    public MolecularFormulaParser Parser { get; }
    public SolutionStoichiometry Solution { get; }
    public GasLawEngine Gas { get; }
    private readonly Dictionary<string, double> _molarMassCache = new();

    public ChemistryStoichiometryEngine(double? R = null)
    {
        Table = new PeriodicTable();
        Parser = new MolecularFormulaParser(Table);
        Solution = new SolutionStoichiometry(Table);
        Gas = new GasLawEngine(R);
    }

    public double MolarMass(string formula)
    {
        if (_molarMassCache.TryGetValue(formula, out var cached))
            return cached;
        var b = Parser.Parse(formula);
        var M = Solution.MolarMassFromAtomBag(b);
        _molarMassCache[formula] = M;
        return M;
    }
}
