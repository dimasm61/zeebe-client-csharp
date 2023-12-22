namespace Client.GracefulStopping.Example;

public static class StaticSettings
{
    public static string ZeebeUrl = "http://you-zeebe-domain.com:26500";

    public static int MaxJobsActive = 10;

    public static byte HandlerThreads = 1;

    public static int PollingTimeoutSec = 10;

    public static int PollIntervalSec = 2;

    // the zeebe is waiting for the worker task to complete, should be less then ShutdownTimeoutSec
    public static int TimeoutSec = 30;

    // the asp.net core engine wait before abort workers threads
    public static int ShutdownTimeoutSec = 35;
}