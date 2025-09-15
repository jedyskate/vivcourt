using OrderBookApp.Processors;

namespace OrderBookApp;

class Program
{
    static void Main(string[] args)
    {

//TODO::REMOVE LATTER, COMMENTED OUT FOR REVIEW
// #if DEBUG
//         var inputPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "items", "input1.stream");
//         args = [inputPath, "5"];
// #endif

        try
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: OrderBookProcessor.exe <input_file_path> <N>");
                return;
            }

            var inputFilePath = args[0];
            if (!int.TryParse(args[1], out var priceDepth))
            {
                Console.WriteLine("Invalid value for Price Depth. Please provide an integer.");
                return;
            }
                
            using var fileStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read);
            var processor = new OrderBookProcessor(priceDepth, Console.Out);
            processor.ProcessStream(fileStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
