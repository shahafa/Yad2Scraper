﻿using System;
using Quartz;
using Quartz.Impl;

namespace AppHarbor
{
    class Program
    {
        static void Main()
        {
            // construct a scheduler 
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = schedulerFactory.GetScheduler();
            scheduler.Start();

            var job = JobBuilder.Create<Yad2ScraperJob>().Build();

            var trigger = TriggerBuilder.Create()
                            .WithCronSchedule("0 0 */4 * * ?")
                            .Build();

            scheduler.ScheduleJob(job, trigger);
        }

        public class Yad2ScraperJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                Console.WriteLine(DateTimeOffset.Now + " Yad2Scraper job started");

                Yad2Scraper.Program.Main();
                
                Console.WriteLine(DateTimeOffset.Now + " Yad2Scraper job ended");
            }
        }
    }
}
