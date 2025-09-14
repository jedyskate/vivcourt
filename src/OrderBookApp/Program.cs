using System.Text;
using OrderBookApp.Extensions;
using OrderBookApp.Models;

namespace OrderBookApp;

class Program
{
    private static Dictionary<string, Dictionary<long, Order>> _orderBooks = new();
    private static Dictionary<string, PriceDepth> _priceDepths = new();
    private static Dictionary<string, string> _lastSnapshots = new();

    static void Main(string[] args)
    {
        args = ["C:\\Users\\jedyp\\Repos\\1. Code Challanges\\vivcourt\\items\\input1.stream", "5"];

        if (args.Length != 2)
        {
            Console.WriteLine("Usage: OrderBookProcessor.exe <input_file_path> <N>");
            return;
        }

        string inputFilePath = args[0];
        if (!int.TryParse(args[1], out int N))
        {
            Console.WriteLine("Invalid value for N. Please provide an integer.");
            return;
        }

        try
        {
            using (var fs = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // Read header
                    int sequenceNo = reader.ReadInt32(); // Little-endian
                    int messageSize = reader.ReadInt32(); // Little-endian

                    long messageStart = reader.BaseStream.Position;
                    char messageType = reader.ReadChar();

                    string symbol = Encoding.ASCII.GetString(reader.ReadBytes(3)).Trim();

                    if (!_orderBooks.ContainsKey(symbol))
                    {
                        _orderBooks[symbol] = new Dictionary<long, Order>();
                        _priceDepths[symbol] = new PriceDepth();
                        _lastSnapshots[symbol] = "";
                    }

                    var orderBook = _orderBooks[symbol];
                    var priceDepth = _priceDepths[symbol];

                    bool changed = false;

                    switch (messageType)
                    {
                        case 'A': // Order Added
                            long addedOrderId = reader.ReadInt64();
                            char addedSide = reader.ReadChar();
                            reader.ReadBytes(3); // Reserved
                            long addedSize = reader.ReadInt64();
                            int addedPriceRaw = reader.ReadInt32();
                            reader.ReadBytes(4); // Reserved

                            int addedPrice = addedPriceRaw;

                            var newOrder = new Order
                            {
                                OrderId = addedOrderId,
                                Side = addedSide,
                                Symbol = symbol,
                                Size = addedSize,
                                Price = addedPrice
                            };

                            orderBook[addedOrderId] = newOrder;
                            priceDepth.AddOrder(newOrder);
                            changed = true;
                            break;

                        case 'U': // Order Updated
                            long updatedOrderId = reader.ReadInt64();
                            char updatedSide = reader.ReadChar();
                            reader.ReadBytes(3); // Reserved
                            long updatedSize = reader.ReadInt64();
                            int updatedPriceRaw = reader.ReadInt32();
                            reader.ReadBytes(4); // Reserved

                            int updatedPrice = updatedPriceRaw;

                            if (orderBook.ContainsKey(updatedOrderId))
                            {
                                var oldOrder = orderBook[updatedOrderId];
                                long oldSize = oldOrder.Size;
                                int oldPrice = oldOrder.Price;

                                oldOrder.Size = updatedSize;
                                oldOrder.Price = updatedPrice;

                                priceDepth.UpdateOrder(oldOrder, oldSize, oldPrice);
                                changed = true;
                            }

                            break;

                        case 'D': // Order Deleted
                            long deletedOrderId = reader.ReadInt64();
                            char deletedSide = reader.ReadChar();
                            reader.ReadBytes(3); // Reserved

                            if (orderBook.ContainsKey(deletedOrderId))
                            {
                                var deletedOrder = orderBook[deletedOrderId];
                                priceDepth.RemoveOrder(deletedOrder);
                                orderBook.Remove(deletedOrderId);
                                changed = true;
                            }

                            break;

                        case 'E': // Order Executed
                            long executedOrderId = reader.ReadInt64();
                            char executedSide = reader.ReadChar();
                            reader.ReadBytes(3); // Reserved
                            long tradedQuantity = reader.ReadInt64();

                            if (orderBook.ContainsKey(executedOrderId))
                            {
                                var executedOrder = orderBook[executedOrderId];
                                priceDepth.ExecuteOrder(executedOrder, tradedQuantity);
                                executedOrder.Size -= tradedQuantity;

                                if (executedOrder.Size <= 0)
                                {
                                    orderBook.Remove(executedOrderId);
                                }

                                changed = true;
                            }

                            break;
                    }

                    // Check if the change is visible in the top N levels and print snapshot.
                    if (changed)
                    {
                        var sortedBids = priceDepth.Bids.OrderByDescending(p => p.Key).Take(N).ToList();
                        var sortedAsks = _priceDepths[symbol].Asks.OrderBy(p => p.Key).Take(N).ToList();

                        string currentSnapshot = SnapshotExtensions.FormatSnapshot(sequenceNo, symbol, sortedBids, sortedAsks);

                        if (currentSnapshot != _lastSnapshots[symbol])
                        {
                            Console.WriteLine(currentSnapshot);
                            _lastSnapshots[symbol] = currentSnapshot;
                        }
                    }

                    // Ensure the reader is at the start of the next message
                    reader.BaseStream.Seek(messageStart + messageSize, SeekOrigin.Begin);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
