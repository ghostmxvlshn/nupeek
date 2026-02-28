# Nupeek Plan (MVP)

## Goal
Build a focused CLI that decompiles specific NuGet types into readable C# for coding agents.

## Tech baseline
- .NET 10 (`net10.0`) everywhere
- Solution split: CLI + Core + Tests
- CI: GitHub Actions build + test on every PR/push

## MVP scope
1. `nupeek type`
   - Inputs: package id/version/tfm/type/out
   - Output: decompiled type file + index/manifest update
2. `nupeek find`
   - Inputs: package id/version/tfm/symbol/out
   - Behavior: derive type from symbol, then run `type` pipeline
3. Core pipeline primitives
   - TFM selection heuristic
   - Symbol parsing (`Namespace.Type.Method` -> `Namespace.Type`)
   - Output path planning + index/manifest models

## Non-goals (for MVP)
- Full transitive dependency dumps
- Member-only decompilation
- Advanced assembly resolver tuning

## Definition of done (MVP)
- `nupeek type` and `nupeek find` commands exist and are test-covered for input/pipeline wiring
- index + manifest model and write path defined
- CI green (build + tests)
- Demo target documented: `Azure.Messaging.ServiceBus.ServiceBusSender`

## Milestones
- M1: Scaffold + CI + tests baseline
- M2: Core parsing/path/heuristics + tests
- M3: Command wiring + dry-run output contract
- M4: NuGet acquisition + assembly type discovery
- M5: ILSpy decompilation + index/manifest updates
