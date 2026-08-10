using ScientificFormulaLab;
using ScientificFormulaLab.Chemistry;

var lab = new CrossDomainLab();
lab.RunAll();
foreach (var line in lab.Log)
    Console.WriteLine(line);
Console.WriteLine();
Console.WriteLine($"R={ChemistryConstants.RGas} J/mol·K, N_A~={ChemistryConstants.Avo:E3} mol^-1, k_B={1.380649e-23} J/K.");
Console.WriteLine("Done.");
