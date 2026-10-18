# Assignment 5: Composing Functions Using Pipelining
(see READMEs for the next assignments below)

_Introduction to Functional Programming_

---
**Topic:**
 Music release distribution management, cost calculation for independent artists

**Target users**: Independent artists, music label administration

**Planned Team:** Individual project (Dmitriy Rochshupkin).

**Repo** https://github.com/DmitryRoschupkin/MusicLabel

## 1. Project Overview & Workflow
**Business Workflow:**
  1. **Input & Validation:** Raw release applications are parsed and validated against domain rules (positive track counts, valid platform numbers, non-empty names).
  2. **Stage 1 (Normalization & Filtering):** Validated records are filtered and normalized (e.g., handling explicit content tags, formatting titles).
  3. **Stage 2 (Cost & Priority Calculation):** Computes base fees and coefficients depending on the selected distribution policy (Indie vs. Major).
  4. **Stage 3 (Quote Generation):** Produces final immutable quote records ready for review.

## 2. Input Fields & Domain Rules
### Input Fields (`ReleaseRequest`)
* `ArtistName` (string): Name of the artist or band.
* `ReleaseTitle` (string): Title of the album/single.
* `TracksCount` (int): Number of tracks included in the release (must be > 0).
* `HasExplicitContent` (bool): Indicates whether the release contains explicit lyrics.
* `TargetPlatformsCount` (int): Number of streaming platforms to distribute to (1 to 25).
* `Tier` (string): Selected service level (*Indie* or *Major*).

### Business Rules
* Releases with invalid track counts ($\le 0$) or out-of-range platform counts are rejected at the boundary.
* Explicit content incurs an additional compliance processing fee.
* Policies dynamically modify the cost calculation function via higher-order functions without mutating core pipeline state.

## 3. Dataset & Expected Results
The application runs on a pre-defined dataset of 5+ records covering normal, boundary, and failure cases:

| ID | Artist | Title | Tracks | Platforms | Explicit | Expected Result (Indie Policy) | Expected Result (Major Policy) |
|----|--------|-------|--------|-----------|----------|--------------------------------|--------------------------------|
| 1  | Friendly Thug 52 NGG| COW | 1 | 5 | True | Valid Quote (~$50) | Valid Quote (~$150) |
| 2  | Автоспорт | Береги себя | 8 | 15 | False | Valid Quote (~$120) | Valid Quote (~$350) |
| 3  | Mnogoznaal | Гостиница Космос | 0 | 5 | False | **Rejected:** Tracks > 0 | **Rejected:** Tracks > 0 |
| 4  | Test Artist | Boundary Test | 1 | 1 | False | Valid Quote (Min bound) | Valid Quote (Min bound) |
| 5  | dmitry_rspkn | Kolutonskiy tape | 3 | 20 | False | Valid Quote (Explicit fee) | Valid Quote (Explicit fee) |

## How to run?

I submited MusicLabes.Tests folder inside the main archive. Open MusicLabel in terminal and just write
`dotnet run` as usual.
The move to tests folder:
```bash
cd ..
cd MusicLabel.Tests
dotnet test
```
Or just open folder as separate projects in IDE and run them.

# Assignment 6: Handling Temporal Coupling

## 1. The Hidden Dependency (Unsafe Before Example)

### The Problem
In earlier versions of the pipeline, records were processed via direct instantiation:
```csharp
// UNSAFE BEFORE EXAMPLE (Do not use in active application path)
var raw = new RawReleaseRecord("Friendly Thug", "COW", "1", "true", "5", "Indie");
// Direct constructor call could create an object without validation:
var release = new ValidRelease(raw.ArtistName, raw.ReleaseTitle, -5, ...); 
var cost = PipelineStages.CalculateBaseCost(release); // The program can fall
```

### The Observable Failure
1. Implicit Order: Correctness relied on the developer remembering to invoke ReleaseValidator.ParseAndValidate() before passing the object to pipeline stages.

2. Invalid State Leakage: If a developer instantiated ValidRelease directly, negative track counts or invalid platform bounds bypassed checks, resulting in incorrect financial calculations (TotalCost) or runtime exceptions.

3. Temporal Coupling: Methods had to be invoked in a strict, undocumented sequence.

**Safe version (this project):** `ValidRelease.Create` must succeed before a `Validated` state can exist, and
`ProcessStep` accepts only a `Validated` state; a `Pending` state is rejected with
`"Prerequisite not met: Release must be validated before processing."`.
The pricing rule is derived from the release tier, so there is no separate "load rules" step to forget.

| Requirement | Where |
|---|---|
| Task 1: hidden dependency | This README, section above |
| Task 2: validated factory | `Models.cs`: `ValidRelease.Create` (5 checks); used by `ReleaseValidator.ParseAndValidate`, which is called from `WorkflowTransitions.ValidateStep` |
| Task 2: no unchecked construction | `Models.cs`: `private` constructor, `private init` properties, `sealed record` (so `with` cannot bypass the factory from outside) |
| Task 3: distinct cases | `Models.cs`: `ReleaseResult` (`Valid`, `Invalid`); `WorkflowState.cs`: `WorkflowState` (`Pending`, `Validated`, `Processed`, `Failed`) |
| Task 4: pattern matching | `WorkflowTransitions.ValidateStep`, `WorkflowTransitions.ProcessStep`, `Program.Render` (all cases explicit, fallback rejects) |
| Task 5: safe operations | `WorkflowTransitions.*` are pure and return a new state; prerequisite is checked by state; failures become `Failed`, nothing is mutated |
| Repeated operation | `ProcessStep` / `ValidateStep` return the existing state for already-completed steps |

**Compile-time note:** the code `new ValidRelease(...)` or `release with { TracksCount = -1 }` from outside the class
does not compile (private constructor, private init). It is described here instead of being added as uncompilable code.