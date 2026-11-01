# Assignment 8: Extension Methods and LINQ

## Reports (Task 2)
| | Report 1: Priority | Report 2: Tier revenue | Report 3: Tier service fee |
|---|---|---|---|
| User | label A&R manager | label finance | label finance |
| Input | priced releases from `catalog.json`, a score function, a threshold | priced releases | tier totals from Report 2 + native fee function |
| Stages | Select, Where, OrderByPriority (OrderByDescending, ThenBy, ThenBy), ToList | GroupBy, Select (Count, Sum), OrderByDescending, ThenBy | Select, Sequence |
| Output | id, artist, title, score, cost | tier, count, total | tier, total, rate (bps), fee (cents) |
| Expected (6-record catalog, REL-003 rejected) | reach score >= 20: REL-002 (83), REL-001 and REL-006 (26, tie broken by title), REL-005 (22) | Major 1 / 455.00; Indie 4 / 395.00 | Major 5460 cents; Indie 1975 cents |

Calculations (`PriorityReport`, `TierReport`, `FeeReport`) never print or touch files; printing is in `Program`, file output in `ReportExporter`.

## Custom extensions (Task 1)
| Extension | Receiver, return | Why on this receiver | Used in |
|---|---|---|---|
| `Partition` | `IEnumerable<Result<T>>` -> `(values, errors)` | only meaningful for a sequence of results; keeps rejected records instead of aborting | `ReportData.Load`, `Sequence` |
| `MapError` | `Result<T>` -> `Result<T>` | counterpart of `Map` for the error side; adds the record id to errors; never called on success | `ReportData.Analyze` |
| `OrderByPriority` | `IEnumerable<PriorityRow>` -> `IOrderedEnumerable<PriorityRow>` | the ordering rule depends on `PriorityRow` fields; total order (score, title, id) | `PriorityReport.Build` |
| `Sequence` | `IEnumerable<Result<T>>` -> `Result<IReadOnlyList<T>>` | all-or-nothing combination, first error wins | `FeeReport.Build` |

## Configured functions (Task 3)
`Scoring.CreateScore : ScoreConfig -> (ValidRelease -> int)` is curried. Calling it with a config is the partial application.
`CreateScore(ReachConfig)` and `CreateScore(AlbumConfig)` are passed into `PriorityReport.Build`; no global state is changed.
Same release (12 tracks, 2 platforms): reach = 22, album = 122, so the top release differs.

## Deferred execution (Task 4)
`ReportData.AnalyzeLazily` is an iterator (`yield return`): creating it runs nothing, iteration analyses one record at a time.
`ReportData.Load` materialises it once with `ToList` (the snapshot); both reports and the export reuse the snapshot.
Repeated enumeration before materialisation would repeat the analysis for every pass (test counters show this);
after materialisation it only reads memory. No file is read or written during enumeration of the analysis.
`ReportCsv.TierFeeLines` is also lazy; the exporter pulls the lines while the output stream is open.

## Resource ownership (Task 5)
| Resource | Owner | Release point |
|---|---|---|
| file read in `CatalogLoader.Load` | `File.ReadAllText` (convenience API) | released inside the call, also when an exception is thrown (test: exclusive reopen succeeds) |
| `Stream` + `StreamWriter` in `ReportExporter.Write` | `Write` | end of the `using` scopes: success, return or exception (tests assert `Disposed`) |
| `.tmp` file in `WriteFile` | `Write`, then `File.Move` | target is replaced only after a full successful write; deleted on failure |
Only specific IO exceptions are caught; other exceptions are not swallowed. Data is never yielded from an open stream
(the file is read inside a bounded scope and a snapshot is returned), so early termination cannot leak a handle.

## Resource note: retention and allocation (Task 6)
- **Where datasets are materialised:** file text and `List<CatalogEntry>` in `CatalogLoader` (temporary, garbage after parsing);
  `ReleaseCatalog` dictionary (immutable, lives for the program); `ToList` snapshot in `ReportData.Load`; report row lists.
- **Deferred queries / closures:** `AnalyzeLazily` captures its source and the analyse function only until it is enumerated;
  it is enumerated immediately in `Load`. Reports return lists, so no query keeps `items` alive; score closures capture only a small immutable `ScoreConfig`.
- **Caches, long-lived collections, event subscriptions:** not used. Static data (`Scoring` configs, loyalty and fee tables) is small and fixed.
- **Design decision:** analyse once and share one snapshot (no re-analysis per report), and load files inside a bounded scope so no handle outlives the load.
- **Trade-off:** streaming would lower peak memory for huge files but keep the file open for the whole pass and could not be re-enumerated;
  a snapshot costs memory proportional to the catalog but allows reuse and immediate release. The catalog is small, so a snapshot is chosen.
  No memory leak is claimed or fixed.

## Native library (Tasks 7-9)
| | |
|---|---|
| Function | `int64_t ml_service_fee_cents(int64_t amount_cents, int32_t fee_bps)` |
| C# binding | `DllImport("musiclabel")`, `long ServiceFeeCents(long, int)`, `Cdecl`, `ExactSpelling` (`NativeFee.cs`) |
| Units | amount in cents (currency units x 100); rate in basis points (1 bps = 0.01 %) |
| Rounding | half up: `(amount * bps + 5000) / 10000` |
| Valid range | amount 0..100000000000000, rate 0..10000 |
| Invalid input | C returns `-1`; the adapter rejects invalid input first (`INVALID_FEE_INPUT`) and maps `-1` to `NATIVE_REJECTED_INPUT` |
| Manual example | 45500 cents x 1200 bps = 54 600 000; / 10 000 = **5460 cents** (54.60); asserted in `RealNativeCall_...` |
| Used in | `FeeReport` -> `ReportWorkflow` -> `Program` (service fee per tier, exported to CSV) |
| Memory | simple values only, no native allocation, nothing to free |

- **Platform:** <OS and version, e.g. from /etc/os-release>, **architecture:** <x86_64 = linux-x64>, **compiler:** <output of `gcc --version`>.
- **Build:** `sh native/build.sh` (`gcc -std=c11 -O2 -Wall -Wextra -Werror -fPIC -shared -fvisibility=hidden`).
- **Binary:** `native/lib/linux-x64/libmusiclabel.so`, copied next to `MusicLabel.dll` by one `<None>` item in `MusicLabel.csproj`
  (and to the test output through `ProjectReference`). No special project settings (no `AllowUnsafeBlocks`).
- **Failures:** missing library -> `NATIVE_LIBRARY_MISSING`; missing export or wrong architecture -> `NATIVE_BINDING_FAILED`
  (both operational errors; tested with an injected failing call because a loaded library cannot be "unloaded" by deleting the file).
- **Difficulty and resolution:** <write the real one. Candidates: the `.so` was not copied to the output folder, so `DllNotFoundException`
  until the `<None>` item was added; `.gitignore` excluded the binary, fixed with `git add -f`; C `long` has a platform-dependent width, so `int64_t` is used.>

## Requirement map (Assignment 8)
| Requirement | Where |
|---|---|
| Task 1: extensions | `ResultExtensions.cs` (`Partition`, `MapError`, `Sequence`), `ReportExtensions.cs` (`OrderByPriority`) |
| Task 2: LINQ reports | `Reports.cs` (`PriorityReport`, `TierReport`), `FeeReport.cs`, data prep in `ReportData.cs` |
| Task 3: currying | `Scoring.cs` (`CreateScore`, `ReachConfig`, `AlbumConfig`) |
| Task 4: deferred execution | `ReportData.AnalyzeLazily`, `ReportData.Load` (materialisation), `ReportCsv.TierFeeLines` |
| Task 5: resources | `ReportExporter.Write` / `WriteFile`, `CatalogLoader.Load` |
| Task 6: retention note | section "Resource note" above |
| Tasks 7-9: native | `native/musiclabel.c`, `native/musiclabel.h`, `native/build.sh`, `NativeFee.cs`, `MusicLabel.csproj` |
| Integration | `ReportWorkflow.BuildAndExport`, `Program.RunIntegratedWorkflow` |

## Commands
```bash
sh native/build.sh        # builds native/lib/linux-x64/libmusiclabel.so
cd MusicLabel && dotnet build && dotnet run
cd ../MusicLabel.Tests && dotnet test
```
Working directory for `dotnet run`: the `MusicLabel/` project folder (the program reads `sample-data/` and writes `report-output/` relatively).

## Tests (Assignment 8)
| Area | Tests | Expected |
|---|---|---|
| Reports | `ReportTests.*` | normal, empty, no match, tie order, grouping and totals |
| Extensions | `ExtensionTests.*`, `FeeReportTests.FeeReport_AnyFeeFailure_...` | split, error mapping, ordering, all-or-nothing |
| Configured functions | `ScoringTests.*` | reach 22 vs album 122 on the same release; top release differs |
| Deferred execution | `DeferredTests.*` | 0 calls before iteration; 1 after `First()`; snapshot reuse adds no calls |
| Resource lifetime | `ResourceTests.*` | stream disposed after success and after failure; handle released after load |
| Native | `NativeIntegrationTests.*` | real call: 5460, 1, 25, 0; failures become `Result` errors |
| Complete workflows | `IntegratedWorkflowTests.*` | normal path writes the CSV; native failure writes nothing |
| File boundaries | `CatalogLoaderTests.*` | missing file and malformed content stay safe failures |

**Latest test outcome:**
```
  dmitriy@dmitriy:~/Documents/aitu/2-year/4-trim/ifp/MusicLabel$ cd ../MusicLabel.Tests; dotnet test
  Восстановление завершено (0,6 с)
  MusicLabel net10.0 успешно выполнено (0,4 с) → /home/dmitriy/Documents/aitu/2-year/4-trim/ifp/MusicLabel/bin/Debug/net10.0/MusicLabel.dll
  MusicLabel.Tests net10.0 успешно выполнено (0,2 с) → bin/Debug/net10.0/MusicLabel.Tests.dll
  [xUnit.net 00:00:00.00] xUnit.net VSTest Adapter v3.1.4+50e68bbb8b (64-bit .NET 10.0.13)
  [xUnit.net 00:00:00.08]   Discovering: MusicLabel.Tests
  [xUnit.net 00:00:00.15]   Discovered:  MusicLabel.Tests
  [xUnit.net 00:00:00.18]   Starting:    MusicLabel.Tests
  [xUnit.net 00:00:00.35]   Finished:    MusicLabel.Tests
  MusicLabel.Tests (тест) net10.0 успешно выполнено (1,0 с)
  
  Сводка теста: всего: 105; сбой: 0; успешно: 105; пропущено: 0; длительность: 1,0 с
  Сборка успешно выполнено через 2,7 с
```

## Contribution
Dmitriy Rochshupkin, group IT-2505 (individual): all code, tests and documentation of this stage.