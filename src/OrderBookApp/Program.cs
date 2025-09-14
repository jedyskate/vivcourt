using OrderBookApp.Processors;

namespace OrderBookApp;

class Program
{
    static void Main(string[] args)
    {
#if DEBUG
        var inputPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "items", "input1.stream");
        args = [inputPath, "5"];
#endif

        try
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: OrderBookProcessor.exe <input_file_path> <N>");
                return;
            }

            var inputFilePath = args[0];
            if (!int.TryParse(args[1], out var n))
            {
                Console.WriteLine("Invalid value for N. Please provide an integer.");
                return;
            }
                
            using var fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
            var processor = new OrderBookProcessor(n, Console.Out);
            processor.ProcessStream(fileStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
