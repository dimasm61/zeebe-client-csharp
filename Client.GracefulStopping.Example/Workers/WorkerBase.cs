using Zeebe.Client;
using Zeebe.Client.Api.Commands;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Client.GracefulStopping.Example.Workers;

public abstract class WorkerBase : IHostedService
{
    private WorkerManager _workerManager;

    private ILogger<WorkerBase> _logger;

    private WorkerInstance workerInstance;

    private string _jobType;

    private CancellationTokenSource _workerCancellationTokenSource;

    private WorkerHandlerCounter _workerHandlerCounter;

    protected WorkerBase(WorkerManager workerManager, string jobType,
        ILogger<WorkerBase> logger, WorkerHandlerCounter workerHandlerCounter)
    {
        _workerManager = workerManager;
        _jobType = jobType;
        _logger = logger;
        _workerHandlerCounter = workerHandlerCounter;
    }

    protected abstract Task WorkTaskAsync(IJob job, ICompleteJobCommandStep1 cmd, CancellationToken cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start worker");
        workerInstance = _workerManager.StartWorker(_jobType, WorkTaskAsync, cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync. Start. Dispose worker");
        workerInstance.JobWorker.Dispose();

        _logger.LogInformation("StopAsync. Delay before close");
        await _workerHandlerCounter.WaitForActiveHandlersAsync();

        _logger.LogInformation("StopAsync. Finish");
    }
}