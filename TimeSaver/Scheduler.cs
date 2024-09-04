using Quartz;
using Quartz.Impl;

namespace TimeSaver
{
    public class Scheduler
    {
        public async Task StartScheduler()
        {
            // Grab the Scheduler instance from the Factory
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();

            // Define the job and tie it to our ScheduledTaskJob class
            var jobDetail = JobBuilder.Create<ScheduledTaskJob>()
                .WithIdentity("scheduledTaskJob", "group1")
                .Build();

            // Get your local time zone
            var localTimeZone = TimeZoneInfo.Local;

            // Trigger the job to run daily at 7 AM
            var trigger = TriggerBuilder.Create()
                .WithIdentity("scheduledTaskTrigger", "group1")
                .WithDailyTimeIntervalSchedule(builder => builder
                    .OnEveryDay()
                    .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(7, 0))
                    .WithIntervalInHours(24) // This sets the trigger to repeat every 24 hours
                    .InTimeZone(localTimeZone)) // Set the trigger time zone to your local time zone
                .Build();

            // Tell Quartz to schedule the job using our trigger
            await scheduler.ScheduleJob(jobDetail, trigger);

            // Manually trigger the job now to execute it immediately upon starting the application
            await scheduler.TriggerJob(jobDetail.Key);

            // Start the scheduler
            await scheduler.Start();

            Console.WriteLine("Scheduler started. The job will run at 7 AM daily.");

            // Keep the console application running indefinitely
            await Task.Delay(-1);
        }
    }
}
