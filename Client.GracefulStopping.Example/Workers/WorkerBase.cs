using Zeebe.Client.Api.Commands;
using Zeebe.Client.Api.Responses;

namespace Client.GracefulStopping.Example.Workers;

public abstract class WorkerBase : IHostedService
{
    private WorkerManager _workerManager;

    private Task workTask;

    private string _jobType;

    private CancellationTokenSource _workerCancellationTokenSource;

    protected WorkerBase(WorkerManager workerManager, string jobType)
    {
        _workerManager = workerManager;
        _jobType = jobType;
    }

    protected abstract Task WorkTask(IJob job, ICompleteJobCommandStep1 cmd, CancellationToken cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _workerCancellationTokenSource = new CancellationTokenSource();

        workTask = _workerManager.StartWorker(_jobType, WorkTask, _workerCancellationTokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // stop own cancellation token
        _workerCancellationTokenSource.Cancel();

        // wait task finish with ignore global token
        await workTask.WaitAsync(CancellationToken.None);
    }
}