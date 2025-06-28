using System.Text;

namespace ConvertToSimfile
{
    public static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var converter = new ZorbaConverter();

                var simfile = converter.ConvertToSimfile();

                const string filename = "Zorba.sm";

                File.WriteAllText(filename, simfile);

                Console.WriteLine($"Simfile created: {filename}");
                Console.WriteLine($"Total steps: {FlashTimings.STEP_TIMES.Length}");
                Console.WriteLine("Conversion complete!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}