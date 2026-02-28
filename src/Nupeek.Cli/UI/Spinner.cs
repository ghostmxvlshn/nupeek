using System.Diagnostics;

namespace Nupeek.Cli;

internal sealed class Spinner : IDisposable
{
    private const string ClearToEndOfLine = "\x1b[K";
    private static readonly char[] Frames = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];

    private readonly TextWriter _writer;
    private readonly string _label;
    private readonly Stopwatch _stopwatch = new();
    private readonly Lock _sync = new();
    private CancellationTokenSource? _cts;
    private Task? _task;
    private bool _isRunning;
    private bool _isStopped;

    public Spinner(string label, TextWriter writer)
    {
        _label = label;
        _writer = writer;
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_isRunning)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _isRunning = true;
            _isStopped = false;
            _stopwatch.Restart();
            _task = Task.Run(() => RenderLoopAsync(_cts.Token));
        }
    }

    public void Stop(string status)
    {
        StopInternal(status, writeStatus: true);
    }

    public void Dispose()
    {
        try
        {
            StopInternal(string.Empty, writeStatus: false);
        }
        catch
        {
            // Dispose should be best-effort and never throw.
        }
        finally
        {
            lock (_sync)
            {
                _cts?.Dispose();
                _cts = null;
                _task = null;
            }
        }
    }

    private void StopInternal(string status, bool writeStatus)
    {
        CancellationTokenSource? cts;
        Task? task;

        lock (_sync)
        {
            if (_isStopped)
            {
                return;
            }

            _isStopped = true;
            _isRunning = false;
            cts = _cts;
            task = _task;
        }

        cts?.Cancel();

        try
        {
            task?.GetAwaiter().GetResult();
        }
        catch (Exception ex) when (writeStatus is false || ex is OperationCanceledException or TaskCanceledException)
        {
            // swallow in Dispose() path, and ignore cancellation-only completion
        }

        lock (_sync)
        {
            _stopwatch.Stop();
            _writer.Write($"\r{ClearToEndOfLine}");

            if (writeStatus)
            {
                _writer.Write($"{status} ({_stopwatch.Elapsed.TotalSeconds:F1}s)\n");
            }

            _writer.Flush();
        }
    }

    private async Task RenderLoopAsync(CancellationToken token)
    {
        var index = 0;

        while (!token.IsCancellationRequested)
        {
            var frame = Frames[index];

            lock (_sync)
            {
                _writer.Write($"\r{frame} {_label}...{ClearToEndOfLine}");
                _writer.Flush();
            }

            index = (index + 1) % Frames.Length;

            try
            {
                await Task.Delay(80, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
