namespace TimeSaver;

public class Program
{
    public static async Task Main(string[] args)
    {
        var scheduler = new Scheduler();

        await scheduler.StartScheduler();
    }
}
