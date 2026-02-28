using Nupeek.Cli;

// Minimal top-level entrypoint delegates to CliApp for testable command wiring.
return await CliApp.RunAsync(args).ConfigureAwait(false);
