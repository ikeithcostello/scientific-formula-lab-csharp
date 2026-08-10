# scientific-formula-lab (C#)

C# port of the TypeScript scientific-formula-lab fixture: math, physics, and chemistry engines with a cross-domain integrator.

## Quick start

```bash
dotnet run --project src/ScientificFormulaLab
dotnet test
```

## Layout

| Path | Role |
|------|------|
| `src/ScientificFormulaLab/Math/` | Polynomials, Simpson/Newton/Cardano, erf, RK4, logΓ |
| `src/ScientificFormulaLab/Physics/` | Ballistics, damped harmonic, circular motion, drag, relativity |
| `src/ScientificFormulaLab/Chemistry/` | Formula parser, stoichiometry, gas laws |
| `src/ScientificFormulaLab/CrossDomainLab.cs` | Integrator |
| `src/ScientificFormulaLab/Program.cs` | CLI entry |
| `tests/ScientificFormulaLab.Tests/` | Assertions mirroring TS `scripts/verify.cjs` |

## Constants

- `RGas` = 8.314462618 J/(mol·K)
- `Avo` = 6.02214076e23
- `GEarth` = 9.80665 m/s²
- `k_B` = 1.380649e-23 J/K

## Branches (fixture)

- `main` — core engines, no vulnerable deps, no Docker
- `test/add-vulnerability-package` — unused CVE NuGet pin + Dockerfile + GHCR workflow
- `test-placeholder` — formula sheets + extended TFX42 registry
