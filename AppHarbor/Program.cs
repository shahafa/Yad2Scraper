using Quartz;
using Quartz.Impl;

namespace AppHarbor
{
    class Program
    {
        static void Main()
        {
            Yad2ScraperJob.InitializeLogger();

            // construct a scheduler 
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = schedulerFactory.GetScheduler();
            scheduler.Start();

            var job = JobBuilder.Create<Yad2ScraperJob>().Build();

            var trigger = TriggerBuilder.Create()
                            .WithSimpleSchedule(x => x.WithIntervalInHours(4).RepeatForever())
                            .Build();

            scheduler.ScheduleJob(job, trigger);
        }
    }
}
