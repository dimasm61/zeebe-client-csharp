using Zeebe.Client.Api.Commands;
using Zeebe.Client.Api.Responses;

namespace Client.GracefulStopping.Example.Workers;

public class Worker1 : WorkerBase
{
    private ILogger<Worker1> _logger;

    public Worker1(WorkerManager workerManager, ILogger<Worker1> logger) : base(workerManager, "Worker1")
    {
        _logger = logger;
    }

    protected override async Task WorkTask(IJob job, ICompleteJobCommandStep1 cmd, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"{nameof(Worker1)} handler start");

        await Task.Delay(20_000, cancellationToken);

        _logger.LogInformation($"{nameof(Worker1)} handler finish");
    }
}