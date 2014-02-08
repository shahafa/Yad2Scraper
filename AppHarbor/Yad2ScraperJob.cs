using Quartz;

namespace AppHarbor
{
    class Yad2ScraperJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            Yad2Scraper.Program.Main();
        }
    }
}