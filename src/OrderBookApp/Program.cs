using System.Text;

namespace OrderBookApp;

// Represents a single order in the order book.
public class Order
{
    public long OrderId { get; set; }
    public char Side { get; set; } // 'B' for Bid, 'S' for Ask
    public string Symbol { get; set; }
    public long Size { get; set; }
    public int Price { get; set; }
}

// Represents the price depth for a specific symbol.
public class PriceDepth
{
    public Dictionary<int, long> Bids { get; set; } = new Dictionary<int, long>();
    public Dictionary<int, long> Asks { get; set; } = new Dictionary<int, long>();

    public void AddOrder(Order order)
    {
        if (order.Side == 'B')
        {
            if (Bids.ContainsKey(order.Price))
            {
                Bids[order.Price] += order.Size;
            }
            else
            {
                Bids[order.Price] = order.Size;
            }
        }
        else // 'S'
        {
            if (Asks.ContainsKey(order.Price))
            {
                Asks[order.Price] += order.Size;
            }
            else
            {
                Asks[order.Price] = order.Size;
            }
        }
    }

    public void UpdateOrder(Order updatedOrder, long oldSize, int oldPrice)
    {
        if (updatedOrder.Side == 'B')
        {
            // Remove old volume from old price level
            if (Bids.ContainsKey(oldPrice))
            {
                Bids[oldPrice] -= oldSize;
                if (Bids[oldPrice] <= 0)
                {
                    Bids.Remove(oldPrice);
                }
            }
            // Add new volume to new price level
            if (Bids.ContainsKey(updatedOrder.Price))
            {
                Bids[updatedOrder.Price] += updatedOrder.Size;
            }
            else
            {
                Bids[updatedOrder.Price] = updatedOrder.Size;
            }
        }
        else // 'S'
        {
            // Remove old volume from old price level
            if (Asks.ContainsKey(oldPrice))
            {
                Asks[oldPrice] -= oldSize;
                if (Asks[oldPrice] <= 0)
                {
                    Asks.Remove(oldPrice);
                }
            }
            // Add new volume to new price level
            if (Asks.ContainsKey(updatedOrder.Price))
            {
                Asks[updatedOrder.Price] += updatedOrder.Size;
            }
            else
            {
                Asks[updatedOrder.Price] = updatedOrder.Size;
            }
        }
    }

    public void RemoveOrder(Order order)
    {
        if (order.Side == 'B')
        {
            if (Bids.ContainsKey(order.Price))
            {
                Bids[order.Price] -= order.Size;
                if (Bids[order.Price] <= 0)
                {
                    Bids.Remove(order.Price);
                }
            }
        }
        else // 'S'
        {
            if (Asks.ContainsKey(order.Price))
            {
                Asks[order.Price] -= order.Size;
                if (Asks[order.Price] <= 0)
                {
                    Asks.Remove(order.Price);
                }
            }
        }
    }

    public void ExecuteOrder(Order order, long tradedQuantity)
    {
        if (order.Side == 'B')
        {
            if (Bids.ContainsKey(order.Price))
            {
                Bids[order.Price] -= tradedQuantity;
                if (Bids[order.Price] <= 0)
                {
                    Bids.Remove(order.Price);
                }
            }
        }
        else // 'S'
        {
            if (Asks.ContainsKey(order.Price))
            {
                Asks[order.Price] -= tradedQuantity;
                if (Asks[order.Price] <= 0)
                {
                    Asks.Remove(order.Price);
                }
            }
        }
    }
}

class Program
{
    private static Dictionary<string, Dictionary<long, Order>> orderBooks = new Dictionary<string, Dictionary<long, Order>>();
    private static Dictionary<string, PriceDepth> priceDepths = new Dictionary<string, PriceDepth>();
    private static Dictionary<string, string> lastSnapshots = new Dictionary<string, string>();

    static void Main(string[] args)
    {
        args = ["items/input1.stream", "5"];
            
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

                    if (!orderBooks.ContainsKey(symbol))
                    {
                        orderBooks[symbol] = new Dictionary<long, Order>();
                        priceDepths[symbol] = new PriceDepth();
                        lastSnapshots[symbol] = "";
                    }

                    var orderBook = orderBooks[symbol];
                    var priceDepth = priceDepths[symbol];

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
                        var sortedAsks = priceDepths[symbol].Asks.OrderBy(p => p.Key).Take(N).ToList();

                        string currentSnapshot = FormatSnapshot(sequenceNo, symbol, sortedBids, sortedAsks);

                        if (currentSnapshot != lastSnapshots[symbol])
                        {
                            Console.WriteLine(currentSnapshot);
                            lastSnapshots[symbol] = currentSnapshot;
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

    private static string FormatSnapshot(int sequenceNo, string symbol, List<KeyValuePair<int, long>> bids, List<KeyValuePair<int, long>> asks)
    {
        var bidStrings = bids.Select(b => $"({b.Key}, {b.Value})");
        var askStrings = asks.Select(a => $"({a.Key}, {a.Value})");

        string bidsFormatted = string.Join(", ", bidStrings);
        string asksFormatted = string.Join(", ", askStrings);

        return $"{sequenceNo}, {symbol}, [{bidsFormatted}], [{asksFormatted}]";
    }
}