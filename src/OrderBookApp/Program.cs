using OrderBookApp.Processors;

namespace OrderBookApp;

class Program
{
    static void Main(string[] args)
    {
        args = ["C:\\Users\\jedyp\\Repos\\1. Code Challanges\\vivcourt\\items\\input1.stream", "5"];

        if (args.Length != 2)
        {
            Console.WriteLine("Usage: OrderBookProcessor.exe <input_file_path> <N>");
            return;
        }

        string inputFilePath = args[0];
        if (!int.TryParse(args[1], out int n))
        {
            Console.WriteLine("Invalid value for N. Please provide an integer.");
            return;
        }

        try
        {
            var processor = new OrderBookProcessor(n);
            processor.ProcessStream(inputFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
