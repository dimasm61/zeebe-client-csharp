using Zeebe.Client;
using Zeebe.Client.Api.Worker;

namespace Client.GracefulStopping.Example.Workers;

public class WorkerInstance
{
    public IZeebeClient ZeebeClient;

    public IJobWorker JobWorker;
}