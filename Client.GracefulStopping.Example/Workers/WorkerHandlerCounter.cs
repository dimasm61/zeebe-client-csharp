using System.Diagnostics;

namespace Client.GracefulStopping.Example.Workers;

public class WorkerHandlerCounter
{
    private readonly ILogger<WorkerHandlerCounter> _logger;

    private static int _currentHandlersActive = 0;

    private static readonly SemaphoreSlim _waitSemaphore = new(0, 1);

    private static int _waitStarted = 0;

    private static readonly Stopwatch CloseStopWatch = new();

    private readonly int _shutdownTimeoutSeconds = StaticSettings.ShutdownTimeoutSec;

    public WorkerHandlerCounter(ILogger<WorkerHandlerCounter> logger)
    {
        _logger = logger;
    }

    public void Increment() => Interlocked.Increment(ref _currentHandlersActive);
    public void Decrement() => Interlocked.Decrement(ref _currentHandlersActive);


    /// <summary>
    /// Wait until the active worker handlers end
    /// </summary>
    public async Task WaitForActiveHandlersAsync()
    {
        if (_currentHandlersActive <= 0) return;

        if (Interlocked.CompareExchange(ref _waitStarted, 1, 0) == 0)
        {
            await WaitingInLoopAsync();
        }
        else
        {
            await Wait();
        }
    }

    private async Task Wait()
    {
        await _waitSemaphore.WaitAsync();
        _waitSemaphore.Release();
    }

    private async Task WaitingInLoopAsync()
    {
        try
        {
            _logger.LogInformation(
                $"Waiting complete handlers... Active handlers: {_currentHandlersActive}");

            CloseStopWatch.Start();

            var sw = Stopwatch.StartNew();

            while (
                _currentHandlersActive > 0
                && CloseStopWatch.Elapsed.TotalSeconds < _shutdownTimeoutSeconds)
            {
                await Task.Delay(10);

                if (sw.Elapsed.TotalSeconds < 1) continue;

                _logger.LogInformation(
                    $"Waiting {CloseStopWatch.Elapsed.TotalSeconds:#0.0}sec... " +
                    $"Active handlers: {_currentHandlersActive}");

                sw = Stopwatch.StartNew();
            }

            sw.Stop();

            _logger.LogInformation(
                $"Waiting finished. {CloseStopWatch.Elapsed.TotalSeconds:#0.0}sec. " +
                $"Active handlers: {_currentHandlersActive}");
        }
        finally
        {
            _waitSemaphore.Release();
        }
    }
}