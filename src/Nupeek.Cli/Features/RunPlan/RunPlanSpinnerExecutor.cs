namespace Nupeek.Cli;

internal static class RunPlanSpinnerExecutor
{
    public static async Task<CliOutcome> ExecuteAsync(Func<Task<CliOutcome>> action)
    {
        var spinner = new Spinner("Executing Nupeek", Console.Error);
        spinner.Start();

        CliOutcome? outcome = null;

        try
        {
            outcome = await action().ConfigureAwait(false);
            return outcome;
        }
        finally
        {
            var status = outcome is not null && outcome.ExitCode == ExitCodes.Success ? "Done" : "Failed";

            try
            {
                await spinner.StopAsync(status).ConfigureAwait(false);
            }
            finally
            {
                await spinner.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
