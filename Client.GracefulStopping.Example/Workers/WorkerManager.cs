using Zeebe.Client;
using Zeebe.Client.Api.Commands;
using Zeebe.Client.Api.Responses;
using Zeebe.Client.Api.Worker;

namespace Client.GracefulStopping.Example.Workers;

public class WorkerManager
{
    private WorkerHandlerCounter _workerHandlerCounter;
    private readonly IServiceProvider _serviceProvider;
    private ILogger<WorkerManager> _logger;

    public WorkerManager(WorkerHandlerCounter workerHandlerCounter, IServiceProvider serviceProvider, ILogger<WorkerManager> logger)
    {
        _workerHandlerCounter = workerHandlerCounter;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartWorker(
        string jobType,
        Func<IJob, ICompleteJobCommandStep1, CancellationToken, Task> handleJobAsync,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    using var zeebeClient = scope.ServiceProvider.GetRequiredService<IZeebeClient>();

                    using var worker = zeebeClient
                        .NewWorker()
                        .JobType(jobType)
                        .Handler((client, job) => HandleJobAsync(client, job, handleJobAsync, cancellationToken))
                        .MaxJobsActive(StaticSettings.MaxJobsActive)
                        .Name($"{jobType}[{Environment.MachineName}][{Environment.CurrentManagedThreadId}]")
                        .AutoCompletion()
                        .PollingTimeout(TimeSpan.FromSeconds(StaticSettings.PollingTimeoutSec))
                        .PollInterval(TimeSpan.FromSeconds(StaticSettings.PollIntervalSec))
                        .Timeout(TimeSpan.FromSeconds(StaticSettings.TimeoutSec))
                        .HandlerThreads(StaticSettings.HandlerThreads)
                        .Open();

                    _logger.LogInformation($"Worker ({jobType}) thread Started");

                    // wait app stop signal
                    cancellationToken.WaitHandle.WaitOne();

                    _logger.LogInformation($"Service stop signal");

                    // stop pulling new jobs, but zeebe client is still active,
                    // job be able to report by active task
                    worker.StopPooling();

                    // wait all handlers finish
                    _workerHandlerCounter.WaitForActiveHandlersAsync().GetAwaiter().GetResult();

                    _logger.LogInformation($"Worker ({jobType}) thread finished");

                    // here is zeebe client disposed
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            });
    }

    private async Task HandleJobAsync(
        IJobClient jobClient,
        IJob job,
        Func<IJob, ICompleteJobCommandStep1, CancellationToken, Task> handleJobAsync,
        CancellationToken workerCancellationToken)
    {
        // ignore service stop signal, the worker should be complete task and do report
        var cancellationToken = CancellationToken.None;

        _workerHandlerCounter.Increment();

        try
        {
            var commandStep1 = jobClient.NewCompleteJobCommand(job.Key);

            await handleJobAsync(job, commandStep1, cancellationToken);

            // reporting
            _logger.LogInformation($"Worker ({job.Type}) report zeebe successful...");

            await commandStep1.Send(cancellationToken);

            _logger.LogInformation($"Worker ({job.Type}) report zeebe successful. Ok");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            _logger.LogInformation($"Worker ({job.Type}) report zeebe error...");

            await jobClient.NewThrowErrorCommand(job.Key)
                .ErrorCode($"{job.Type}Error")
                .ErrorMessage(ex.Message)
                .Send(cancellationToken);

            _logger.LogInformation($"Worker ({job.Type}) report zeebe error. Ok");
        }
        finally
        {
            _workerHandlerCounter.Decrement();
        }
    }
}