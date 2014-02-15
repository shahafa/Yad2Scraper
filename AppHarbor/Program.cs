using System.Threading;

namespace AppHarbor
{
    class Program
    {
        private const int SleepTimeOut = 14400000;

        static void Main()
        {
            while (true)
            {
                Yad2Scraper.Program.Execute();

                // Goto Sleep
                Thread.Sleep(SleepTimeOut);
            }
        }
    }
}
