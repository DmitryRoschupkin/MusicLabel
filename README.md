# Assignment 6: Handling Temporal Coupling

_Introduction to Functional Programming_

---
**Topic:**
 Music release distribution management, cost calculation for independent artists

**Target users**: Independent artists, music label administration

**Team:** Individual project (Dmitriy Rochshupkin).

**Repo** https://github.com/DmitryRoschupkin/MusicLabel
## Input Fields & Domain Rules

**Input:** `RawReleaseRecord` (artist, title, tracks count, explicit flag, platforms count, tier; all strings).

**Domain rules (all enforced in `ValidRelease.Create`):**

| Rule | Condition |
|---|---|
| Artist | not empty or whitespace |
| Title | not empty or whitespace |
| Tracks | strictly greater than 0 |
| Platforms | between 1 and 25 inclusive |
| Tier | a defined `MusicTier` (`Indie` or `Major`) |

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

## Tests

| Required case | Test(s) | Expected |
|---|---|---|
| Factory success | `Factory_ValidInput_ReturnsValidWithTrimmedData` | `Valid`, data trimmed and equal to input |
| Factory rejection | `Factory_InvalidInput_ReturnsInvalidWithReason` (6 cases), `Parser_MalformedInput_ReturnsInvalid`, `Parser_NullArtist_...` | `Invalid` with the reason for each rule |
| Distinct cases | `DistinctCases_ReleaseResult_...`, `DistinctCases_WorkflowStates_AllFourAreReachable` | each case reachable, correct match branch |
| Normal operation | `NormalOperation_IndieRelease_...` (60), `NormalOperation_MajorRelease_...` (455), `IndieRule_MoreThanTenPlatforms_...` (67.50) | correct `Processed` quote |
| Missing prerequisite | `MissingPrerequisite_ProcessWithoutValidation_IsRejected` | `Failed` containing "Prerequisite not met" |
| Repeated operation | `RepeatedOperation_ReturnsExistingOutcome` | second call returns an equal state |
| Boundary value | `Boundary_PlatformsCount_...` (0, 1, 25, 26), `Boundary_TracksCount_...` (0, 1) | exact limits follow the factory rule |
| State preservation | `StatePreservation_*` (3 tests) | original state equal to its snapshot after a failed call |

Total: 16 test methods (more cases counting `Theory` rows).

**Latest test outcome:**

```
dmitriy@dmitriy:~/Documents/aitu/2-year/4-trim/ifp/MusicLabel.Tests$ dotnet test
Восстановление завершено (0,9 с)
  MusicLabel net10.0 успешно выполнено (0,3 с) → /home/dmitriy/Documents/aitu/2-year/4-trim/ifp/MusicLabel/bin/Debug/net10.0/MusicLabel.dll
  MusicLabel.Tests net10.0 успешно выполнено (0,2 с) → bin/Debug/net10.0/MusicLabel.Tests.dll
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.11)
[xUnit.net 00:00:00.10]   Discovering: MusicLabel.Tests
[xUnit.net 00:00:00.16]   Discovered:  MusicLabel.Tests
[xUnit.net 00:00:00.21]   Starting:    MusicLabel.Tests
[xUnit.net 00:00:00.36]   Finished:    MusicLabel.Tests
  MusicLabel.Tests (тест) net10.0 успешно выполнено (1,1 с)

Сводка теста: всего: 30; сбой: 0; успешно: 30; пропущено: 0; длительность: 1,1 с
Сборка успешно выполнено через 2,9 с
```