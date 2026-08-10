# TFX42 — Injected placeholder inventory (ground truth)

This file lists every intentional placeholder block added for **placeholder detection** testing. The marker **`TFX42`** appears in a comment on the line immediately before each block so humans (and heuristics) can correlate findings.

Sizes are **approximate line counts** of the placeholder body (comment + code + data), not the full file.

| # | File | ~Lines | `TFX42` location (anchor) | Rules from `placeholder_detection_rules.md` | What was injected (non-obvious summary) |
|---|------|--------|-----------------------------|---------------------------------------------|----------------------------------------|
| 1 | `src/ScientificFormulaLab/Math/ScientificMathEngine.cs` | ~38 | After `NearlyZero`, before `RealPolynomial` | **1** (TBD, TODO, WIP, stub in text), **3** (names like `sample-university`, `j.doe`, synthetic case ids), **4** (fictional inter-lab rows), **6** (example DOIs, lorem, `.test` / `.org` emails, all-zero checksums) | Readonly array of fake “provenance” rows for a benchmark spreadsheet that is never read by the engine; blank-assigned to avoid unused warnings. |
| 2 | `src/ScientificFormulaLab/Physics/AnalyticalPhysicsEngine.cs` | ~6 | After `AnalyticalPhysicsEngine` methods, before end of file | **1** (comment says “Stubs the full ISA…”), **2** (function always returns `1.225` and ignores altitude), **5** (shape of a real atmospheric API without behavior) | Exported `IsaStandardDryAirDensityAtAltitude` that looks like a profile helper but is a **constant-return stub**. |
| 3 | `src/ScientificFormulaLab/Chemistry/ChemistryStoichiometryEngine.cs` | ~11 | After `GasLawEngine` methods, before `StoichiometricReaction` | **1** (comment “placeholder workflow”), **3** (`batchId` + `SAMPLE` naming), **4** (fake vendor stub object), **6** (`fictitious-gases.test`, `REPLACE_ME`, “Pending signature” text) | Const object simulating a vendor receipt; not connected to the engine. |
| 4 | `src/ScientificFormulaLab/CrossDomainLab.cs` | ~20 | End of `CrossDomainLab` constructor | **1** (FIXME, WIP, TBD in strings/comments), **4** (invented people / orgs), **6** (test Stripe-style token, `test@`, example URL), **8** (entire block under `if (false)` is dead) | Fictive “Lab Cloud” handoff payload with **permanently disabled** execution path. |
| 5 | `.env.example` | 5 (file) | Top comment block in file | **1** (template `{{...}}` token), **6** (`your_api_key_here`, `replaceme`, `.test` email), **7** (environment template / “fill in” style values) | Example env with obvious template secrets and a bracketed host variable. |

## Distribution (by size)

- **Large (~20–40 lines):** `ScientificMathEngine.cs` (tabular fake metadata), `CrossDomainLab.cs` (dead `if (false)` object + nested comment).
- **Small (~2–6 lines):** `AnalyticalPhysicsEngine.cs` (single return stub), `.env.example` (short template keys).
- **Medium (~8–12 lines):** `ChemistryStoichiometryEngine.cs` (vendor stub const).

## How this ties to the rule doc

The summary table in `placeholder_detection_rules.md` lists nine rule families. The injections above are designed so that **rule-based detectors** can exercise:

- **Explicit markers (1),** string patterns (6), **dummy returns (2),** **naming (3),** **fake in-app data (4),** **empty/dead (5/8),** and **config templates (7).**

**Rule 9** (documentation TBD) is *not* exercised here; the only doc added is *this* registry, which is intentionally complete rather than a stub.

## Operational note

These blocks are **not** required for `dotnet run` or `dotnet test` to pass. The physics export is a stub; the math and chemistry consts are explicitly blank-assigned. The integrator’s `if (false)` block is unreachable.

## Public API surface (non-TFX42) coverage

All **real, non-placeholder** exports are exercised from `CrossDomainLab` suites: `RealPolynomial` and `ScientificMathEngine` methods, physics helpers `Mag` / `Add2` / `Scale2` and all public methods except the **intentional** TFX42 stub `IsaStandardDryAirDensityAtAltitude` (kept as a standard placeholder for detection). Private helpers in modules are an implementation detail and are not required to be called from the lab.

**Do not** merge these patterns into production without replacing or removing them—this set exists for **detection testing** only.
