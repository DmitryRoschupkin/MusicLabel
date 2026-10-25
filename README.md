# Assignment 7: Exception Handling in FP

_Introduction to Functional Programming_

---
**Topic:**
 Music release distribution management, cost calculation for independent artists

**Target users**: Independent artists, music label administration

**Team:** Individual project (Dmitriy Rochshupkin).

**Repo** https://github.com/DmitryRoschupkin/MusicLabel

## Environment and commands

- **.NET SDK:** 10.0 (built and tested with SDK 10.0.111, target `net10.0`, C# 14)
- **Prerequisites:** none besides the SDK (xUnit packages are restored by `dotnet test`)
- **Working directory for the demo:** the `MusicLabel/` project folder (the one with `MusicLabel.csproj`),
  because the program reads `sample-data/...` by relative path.

```bash
cd MusicLabel
dotnet build
dotnet run

cd ../MusicLabel.Tests
dotnet test
```

## Workflow

Input: a JSON catalog (`sample-data/catalog.json`) of release records. Each record has
`id`, `artistName`, `releaseTitle`, `tracksCount`, `hasExplicitContent`, `targetPlatformsCount`, `tier`
(all values are strings, validated later by the domain).

The quote workflow is a single `Result` chain (`QuoteWorkflow.Quote`):

```
catalog.Find(id)                 Maybe<RawReleaseRecord>
  .ToResult(ReleaseNotFound)     Result<RawReleaseRecord>     missing item -> failure
  .Bind(ParseAndValidate)        Result<ValidRelease>         may reject (domain rules)
  .Bind(Price)                   Result<ReleaseQuote>         state gate + pricing, may reject
  .Map(LoyaltyProgram.Apply)     Result<ReleaseQuote>         pure transformation
  .Map(Render)                   Result<string>               display model
```

The outcome is handled **once**, at the application boundary (`Program.Report`), with pattern matching.

**Domain rules (all enforced in `ValidRelease.Create`):**

| Rule | Condition |
|---|---|
| Artist | not empty or whitespace |
| Title | not empty or whitespace |
| Tracks | strictly greater than 0 |
| Platforms | between 1 and 25 inclusive |
| Tier | a defined `MusicTier` (`Indie` or `Major`) |

Pricing: base = 20 per track + 5 per platform (+15 if explicit). `Indie`: 10% discount when more than
10 platforms. `Major`: +100 priority fee + 15 per track. Optional loyalty discount per artist (see Task 2).

## Types and what they mean

| Type | Meaning |
|---|---|
| `Result<T>` (`Success` / `Failure`) | An operation either produced a value or failed with an `Error`. |
| `Error(Code, Message, Kind)` | Why it failed. `Kind` is `Domain` (a business rule or missing item) or `Operational` (file/data problem). |
| `Maybe<T>` (`Some` / `None`) | A value may be absent. Absence is not an error by itself; the context decides. |
| `WorkflowState` | Where a release is in the process (`Pending`, `Validated`, `Processed`, `Failed`). `Failed` carries the `Error`. |

A **validation failure** (`Failure` with an `INVALID_*` code) says the input broke a rule. A **workflow state**
says what stage the process has reached; it never replaces the reason for a failure.

## Requirement map

| Requirement | Where |
|---|---|
| Task 1: `Result` with distinct cases, no contradictory states | `Result.cs`: `Result<T>` (private base constructor, `Success`/`Failure` reject `null`); `Errors.cs`: `Error` |
| Task 1: three domain failures | invalid quantity: `Models.cs` `ValidRelease.Create`; missing item: `ReleaseCatalog.Find` + `ToResult`; state forbids operation: `WorkflowTransitions.RequireValidated` |
| Task 2: `Maybe` lookup | `Maybe.cs`; `ReleaseCatalog.Find` (required item), `LoyaltyProgram.DiscountFor` (optional item, `Some(0)` is legitimate) |
| Task 3: `Map` / `Bind` | `ResultExtensions.cs`; used in `QuoteWorkflow.Quote` and `QuoteWorkflow.Price` |
| Task 4: file boundary | `CatalogLoader.Load` (the only place with file access and `try/catch`) |
| Task 5: integration | `Program.cs` (7 demonstrated paths + earlier pipeline) |
| Earlier pipeline kept | `WorkflowTransitions.ValidateStep/ProcessStep`, `PipelineStages`, `PricingRules` |

## Task 1: expected outcomes

| Case | Success | Failure (`Code`) | Where |
|---|---|---|---|
| Invalid quantity | `Success(ValidRelease)` | `INVALID_TRACKS_COUNT`, `INVALID_PLATFORMS_COUNT`: message names the rule and the value | `ValidRelease.Create` |
| Missing required item | `Success(RawReleaseRecord)` | `RELEASE_NOT_FOUND`: message contains the id | `ReleaseCatalog` + `ToResult` |
| State does not permit the operation | `Success(ValidRelease)` | `PREREQUISITE_NOT_MET` | `WorkflowTransitions.RequireValidated` |

Other validation errors: `ARTIST_REQUIRED`, `TITLE_REQUIRED`, `INVALID_TIER`, `MALFORMED_INPUT` (a field that is not a number/bool;
the message shows the raw input).

## Task 2: missing values

- `ReleaseCatalog.Find(id)` returns `Maybe<RawReleaseRecord>`. The release is **required** for a quote, so `None` is converted
  into `Failure(RELEASE_NOT_FOUND)` with `ToResult`. Blank or `null` ids give `None`; no record is invented.
- `LoyaltyProgram.DiscountFor(artist)` returns `Maybe<decimal>`. The discount is **optional**:
  `None` (artist is not in the program) leaves the quote unchanged; `Some(0m)` (enrolled with a 0% discount) is a real value,
  keeps the price but marks the quote as `+Loyalty(...)`. Zero is never treated as "absent".

## Task 3: Map and Bind

| | Function | Use in the project | Result type |
|---|---|---|---|
| `Map` | `T -> U`, pure | `LoyaltyProgram.Apply`, `Render` | `Result<U>` |
| `Bind` | `T -> Result<U>`, may fail | `ParseAndValidate`, `Price` | `Result<U>` (not `Result<Result<U>>`) |

On `Failure` both return the same error and **do not call** the supplied function.
This is proven in `MapBindTests.MapAndBind_OnFailure_PropagateErrorAndDoNotInvokeFunctions` (call counter stays 0).

## Task 4: operational failures at the boundary

`CatalogLoader.Load(path)` is the only code that touches the file system. It catches **specific** exceptions
and converts them into `Operational` errors that contain the file path:

| Problem | Caught exception | Error code |
|---|---|---|
| File does not exist | `FileNotFoundException`, `DirectoryNotFoundException` | `FILE_NOT_FOUND` |
| Cannot read the file | `IOException`, `UnauthorizedAccessException` | `FILE_UNREADABLE` |
| Invalid JSON, `null` document, missing required property | `JsonException` | `MALFORMED_FILE` |
| Missing/duplicate catalog id | (no exception, checked by `ReleaseCatalog.From`) | `CATALOG_ID_MISSING`, `DUPLICATE_CATALOG_ID` |

- **No fallback:** if loading fails, the program reports the problem; it never substitutes an empty catalog.
- **No partial update:** `Load` returns either a complete, immutable `ReleaseCatalog` or a failure.
  A half-loaded catalog cannot exist, and the file itself is never modified.
- Domain code (`ValidRelease`, pricing, workflow) never touches files and never sees exceptions.
- A well-formed file with a bad *value* (for example `"tracksCount": "0"`) is a **domain** failure
  (`INVALID_TRACKS_COUNT`), not a file failure.

## Demonstrations (`dotnet run`)

| # | Scenario | Expected |
|---|---|---|
| Earlier pipeline | Assignments 5-6 flow, repeat is safe | `[SUCCESS]` x2, `[SAFE REPEAT]`, `[REJECTED]` for 0 tracks |
| A | Missing file | `[OPERATIONAL FAILURE] [FILE_NOT_FOUND]` |
| B | Malformed JSON (`sample-data/malformed.json`) | `[OPERATIONAL FAILURE] [MALFORMED_FILE]` |
| Lookup | `REL-001` / `REL-999` | `[FOUND]` / `[NOT FOUND]` |
| 1 | Normal path, `REL-002` | `[SUCCESS] Автоспорт - Береги себя: $455.00 [Major]` |
| 2 | Invalid quantity, `REL-003` | `[DOMAIN FAILURE] [INVALID_TRACKS_COUNT]` |
| 3 | Missing required item, `REL-999` | `[DOMAIN FAILURE] [RELEASE_NOT_FOUND]` |
| 4 | State forbids pricing (pending release) | `[DOMAIN FAILURE] [PREREQUISITE_NOT_MET]` |
| 5 | Optional value `Some(0)`, `REL-004` | `[SUCCESS] ...[Indie+Loyalty(0%)]`, price unchanged |

Sample data: `sample-data/catalog.json` (valid), `sample-data/malformed.json` (intentionally broken).


## Tests

| Required case | Test(s) | Expected |
|---|---|---|
| Complete success | `QuoteChainTests.Quote_ValidRelease_ProducesCorrectDisplayValue` | `Band - Album : 455.00 [Major]` |
| Domain failures (3) | `QuoteChainTests.Quote_InvalidTracks_FailsWithRuleCode`, `QuoteChainTests.Quote_UnknownRelease_FailsWithReleaseNotFound`, `QuoteChainTests.Price_PendingState_FailsWithPrerequisiteNotMet` | `INVALID_TRACKS_COUNT`, `RELEASE_NOT_FOUND` (message contains the id), `PREREQUISITE_NOT_MET` |
| Useful domain errors | `ValidReleaseTests.Create_InvalidInput_FailsWithExpectedCode`, `ValidReleaseTests.Create_InvalidTracks_ErrorNamesTheRuleAndTheValue`, `ReleaseValidatorTests.Parse_NonNumericTracks_FailsAndMessageShowsTheRawInput` | each rule has its own code; the message names the rule and the offending value |
| Lookup present / absent | `LookupTests.Find_ExistingId_ReturnsSome`, `LookupTests.Find_MissingOrBlankId_ReturnsNone` | `Some` for an existing id; `None` for unknown, blank and `null` ids |
| Primitive is not absence | `LookupTests.Loyalty_ZeroDiscount_IsPresent_NotAbsent`, `LookupTests.Apply_ZeroDiscount_KeepsCostButMarksEnrollment_NotEnrolledIsUntouched` | `Some(0m)` is present; price unchanged but marked as enrolled; `None` leaves the quote untouched |
| Missing item to failure (`ToResult`) | `LookupTests.ToResult_None_FailsWithReleaseNotFound` | `RELEASE_NOT_FOUND` containing the id |
| Map success | `MapBindTests.Map_Success_AppliesFunctionToValue` | `20` |
| Bind success | `MapBindTests.Bind_Success_ReceivesValueAndPreservesItsResult` | value passed to the next operation, its `Result` preserved (success and failure) |
| Failure propagation | `MapBindTests.MapAndBind_OnFailure_PropagateErrorAndDoNotInvokeFunctions` | call counter is 0, same error returned by both |
| Result model | `ResultTests.*` | `null` payload / `null` error rejected, `0` is a valid success, two distinct cases |
| State gate | `WorkflowTests.RequireValidated_*`, `WorkflowTests.Process_WithoutValidation_IsRejectedWithPrerequisiteNotMet` | `Validated` gives the release; `Pending` gives `PREREQUISITE_NOT_MET`; `Failed` propagates its own error; `Processed` gives `INVALID_TRANSITION` |
| Missing file | `CatalogLoaderTests.Load_MissingFile_FailsWithOperationalErrorNamingThePath` | `FILE_NOT_FOUND`, `Operational`, path in message |
| Malformed file | `CatalogLoaderTests.Load_MalformedJson_FailsAndLeavesFileUntouched`, `CatalogLoaderTests.Load_EntryWithMissingProperty_FailsAsMalformedFile` | `MALFORMED_FILE`, no catalog produced, file content unchanged |
| Duplicate ids / valid file | `CatalogLoaderTests.Load_DuplicateIds_FailsWithoutProducingCatalog`, `CatalogLoaderTests.Load_ValidFile_ReturnsCatalogUsableByTheChain` | `DUPLICATE_CATALOG_ID`; a valid file loads and the chain works on it |
| State preservation | `WorkflowTests.StatePreservation_FailedProcessStep_DoesNotMutateOriginalState` | original state equals its snapshot after a failed call |
| Earlier behaviour (pricing, boundaries, parser, repeat) | `WorkflowTests.Normal_*`,

**Latest test outcome:**

```
dmitriy@dmitriy:~/Documents/aitu/2-year/4-trim/ifp/MusicLabel.Tests$ dotnet test
Восстановление завершено (0,5 с)
  MusicLabel net10.0 успешно выполнено (0,1 с) → /home/dmitriy/Documents/aitu/2-year/4-trim/ifp/MusicLabel/bin/Debug/net10.0/MusicLabel.dll
  MusicLabel.Tests net10.0 успешно выполнено (0,2 с) → bin/Debug/net10.0/MusicLabel.Tests.dll
[xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.11)
[xUnit.net 00:00:00.09]   Discovering: MusicLabel.Tests
[xUnit.net 00:00:00.14]   Discovered:  MusicLabel.Tests
[xUnit.net 00:00:00.18]   Starting:    MusicLabel.Tests
[xUnit.net 00:00:00.31]   Finished:    MusicLabel.Tests
  MusicLabel.Tests (тест) net10.0 успешно выполнено (1,0 с)

Сводка теста: всего: 65; сбой: 0; успешно: 65; пропущено: 0; длительность: 1,0 с
Сборка успешно выполнено через 2,3 с
```