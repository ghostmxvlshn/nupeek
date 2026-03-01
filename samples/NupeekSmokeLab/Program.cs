using Polly;

const int retries = 5;
const int failuresBeforeSuccess = 3;

var attempt = 0;

var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetry(
        retryCount: retries,
        sleepDurationProvider: retryAttempt =>
        {
            var baseDelay = TimeSpan.FromMilliseconds(250 * retryAttempt);
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(50, 200));
            return baseDelay + jitter;
        },
        onRetry: (exception, delay, retryNumber, _) =>
        {
            Console.WriteLine(
                $"Retry {retryNumber}/{retries} in {delay.TotalMilliseconds:N0} ms after: {exception.Message}");
        });

try
{
    var response = retryPolicy.Execute(() =>
    {
        attempt++;
        return SimulateUnstableCall(attempt);
    });

    Console.WriteLine(response);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Operation failed after {attempt} attempts: {ex.Message}");
    return 1;
}

return 0;

string SimulateUnstableCall(int currentAttempt)
{
    Console.WriteLine($"Calling unstable dependency, attempt {currentAttempt}...");

    if (currentAttempt <= failuresBeforeSuccess)
    {
        throw new InvalidOperationException(
            $"Transient failure on attempt {currentAttempt}. Expected success after attempt {failuresBeforeSuccess}.");
    }

    return $"Recovered successfully on attempt {currentAttempt}.";
}
