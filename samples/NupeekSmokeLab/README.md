# NupeekSmokeLab

Small .NET console sample for Nupeek/Copilot A/B smoke testing with Polly retry behavior.

## Quick run

```bash
cd samples/NupeekSmokeLab
dotnet run
```

## Copilot A/B prompts

### A) Without Nupeek skill

Use this exact prompt:

```text
In samples/NupeekSmokeLab/Program.cs, explain how Polly's Policy.Handle<Exception>().WaitAndRetry probably works internally. Focus on retry counting, delay calculation, exception flow, and when onRetry runs. Do not inspect package internals or decompiled code; answer from general knowledge only and call out any uncertainty.
```

### B) With Nupeek skill

Use this exact prompt:

```text
Using the Nupeek skill, inspect Polly internals used by samples/NupeekSmokeLab/Program.cs and explain the exact behavior of Policy.Handle<Exception>().WaitAndRetry for this sample. Trace the real decompiled code paths for policy construction and execution, including retry counting, sleepDurationProvider usage, exception handling, and when onRetry is invoked. Cite the relevant decompiled types and methods you inspected.
```

## Nupeek commands for Polly internals

Run from the repo root:

```bash
dotnet run --project src/Nupeek.Cli -- type --package Polly --type Polly.Retry.RetryEngine --out deps-src --dry-run false
dotnet run --project src/Nupeek.Cli -- type --package Polly --type Polly.Retry.RetryPolicy --out deps-src --dry-run false
dotnet run --project src/Nupeek.Cli -- type --package Polly --type Polly.PolicyBuilder --out deps-src --dry-run false
dotnet run --project src/Nupeek.Cli -- find --package Polly --symbol Polly.Policy.Handle --out deps-src
```
