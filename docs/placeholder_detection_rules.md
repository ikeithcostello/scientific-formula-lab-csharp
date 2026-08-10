# Placeholder Detection Rules

This document defines rules to identify **placeholders** in code and config so tools and reviewers can find temporary, fake, or incomplete content. Each rule has a short description and concrete detection criteria.

---

## 1. Explicit Placeholder Markers

Content that explicitly declares itself as temporary, incomplete, or to be replaced.

- Comments or text containing `TODO`, `FIXME`, `PLACEHOLDER`, `TBD`, `WIP`, `stub`, or `replace with` are strong signals.
- Template-style tokens such as `{{VARIABLE}}` or `your_*_here` indicate fill-in spots.
- Filenames or section headers that include "placeholder", "dummy", or "example" help narrow scope.
- Easiest to detect with regex; low false-positive rate when matched in context.

**Example:** `// TODO: replace with real implementation`

---

## 2. Dummy or Constant Return Logic

Functions or methods that always return the same fixed or empty value regardless of input.

- Returning only literals (`0`, `null`, `{}`, `[]`, `"test"`, `true`) with no input-based logic suggests a stub.
- No use of parameters, dependencies, or branching usually means the function is not yet implemented.
- Common in scaffolding, new features, or interfaces that were satisfied with minimal code.
- Detecting this reduces risk of non-functional behavior in production paths.

**Example:** `function getUser() { return {}; }`

---

## 3. Naming Conventions That Signal Placeholders

Identifiers whose names indicate fake, temporary, or example data or behavior.

- Variables or functions named with `placeholder`, `dummy`, `mock`, `fake`, `stub`, `example`, `sample`, `temp`, or `xxx` are strong candidates.
- Names like `testValue`, `TBD`, or `placeholder*` in production code (not under test/) should be flagged.
- Helps catch placeholders that have no comment or special return value.
- Can be combined with pattern checks (e.g. only in source, not in test files).

**Example:** `const placeholderUserId = 'user-123';`

---

## 4. Mock or Fake Data in Non-Test Code

Hardcoded fake entities, simulated responses, or test-style data used outside test or fixture directories.

- Inline arrays or objects that look like single fake users, orders, or configs in application code.
- Simulated delays, fixed responses, or branches that never call real IO (DB, API, file system).
- High risk if such code is deployed; often leftover from local or exploratory work.
- Detection should consider path (e.g. exclude `*.test.*`, `__tests__`, `fixtures`) to avoid flagging intentional mocks.

**Example:** `const users = [{ id: 1, name: "John Doe" }];` in a service file.

---

## 5. Empty or Shape-Only Implementations

Code that defines a structure (signature, class, method) but has no real behavior.

- Empty function or method bodies, or bodies that only return a constant or call another stub.
- Classes or objects with methods that exist only to satisfy an interface or type.
- Indicates design-first or incomplete implementation; acceptable only when clearly intentional (e.g. documented adapter).
- Tools can flag empty or single-statement bodies in non-test code and suggest a follow-up.

**Example:** `class PaymentService { charge() {} }`

---

## 6. Placeholder and Fake Data Patterns

Known dummy values commonly used in development, configs, or examples.

- Strings like `"example"`, `"test"`, `"lorem ipsum"`, `"xxx"`, `"sample"`, or emails such as `test@example.com`, `user@test.com`.
- Credentials or keys such as `your_api_key_here`, `FIXTURE_PAYMENT_TOKEN_PLACEHOLDER_NOT_A_SECRET`, `replace_me`, or obvious UUIDs like all zeros.
- Often copied into env files, configs, or fixtures and then forgotten; detecting them improves security and consistency.
- Pattern lists (e.g. for emails, keys, and generic text) can be maintained and extended per project.

**Example:** `"email": "test@example.com"` or `API_KEY=your_api_key_here` in config.

---

## 7. Config and Environment Placeholders

Template or example values in environment files, config templates, or deployment manifests.

- In `.env.example`, `.env.sample`, or similar: values like `your_*`, `replace_with_*`, `xxx`, or placeholder URLs.
- Empty or commented values with instructions such as "set in production" or "obtain from dashboard".
- Helps ensure real secrets and URLs are not committed and that docs match actual config shape.
- Detection should focus on template/sample files and exclude real env files that may contain valid placeholders.

**Example:** `DATABASE_URL=postgresql://user:pass@localhost/dbname` in `.env.example`.

---

## 8. Permanently Disabled or Dead Code Paths

Code paths that are never executed under normal conditions.

- Conditions that are always false (e.g. `if (false)`), or feature flags that are never enabled in any env.
- Long-term commented-out blocks that represent abandoned or future behavior.
- Indicates placeholder or future intent without current value and adds maintenance and cognitive load.
- Tools can flag unreachable branches and suggest removal or proper feature-flag usage.

**Example:** `if (false) { enableNewFeature(); }`

---

## 9. Documentation and README Placeholders

Incomplete or template sections in docs that were never filled in.

- Empty sections under headers, or lines with only `TBD`, `Coming soon`, or `TODO`.
- Unresolved template variables (e.g. `{{PROJECT_NAME}}`) or copy-paste leftovers.
- Misleads users and automated doc tooling; common in generated or boilerplate READMEs.
- Simple keyword and structure checks (empty content under a heading) are usually enough to flag.

**Example:** A section containing only `## Usage` followed by `TBD`.

---

## Summary

| # | Rule | What is detected | Example |
|---|------|------------------|--------|
| 1 | Explicit placeholder markers | Comments, tokens, names that say "temporary" | `// TODO: replace with real auth` |
| 2 | Dummy or constant return logic | Functions that always return the same value | `return 0;`, `return {};` |
| 3 | Naming conventions | Identifiers suggesting fake/temp data | `dummyUserId`, `mockService` |
| 4 | Mock/fake data in non-test code | Hardcoded fake entities outside tests | `[{ id: 1, name: "John" }]` in app code |
| 5 | Empty or shape-only implementations | Structure without behavior | `function foo() {}` |
| 6 | Placeholder and fake data patterns | Known dummy strings and credentials | `test@example.com`, `your_api_key_here` |
| 7 | Config and environment placeholders | Template values in env/config | `API_KEY=your_key_here` in `.env.example` |
| 8 | Permanently disabled / dead code | Unreachable or always-off paths | `if (false) { ... }` |
| 9 | Documentation placeholders | Empty or TBD sections in docs | `## Usage` + `TBD` |

A **placeholder** is content that is intentionally incomplete or fake and is meant to be replaced or used only in non-production contexts. Reliable detection uses several of these rules together and can assign a type and confidence level rather than treating every match as a hard error.
