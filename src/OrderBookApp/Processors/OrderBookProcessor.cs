using System.Text;
using OrderBookApp.Extensions;
using OrderBookApp.Models;

namespace OrderBookApp.Processors;

public class OrderBookProcessor
{
    private readonly int _priceDepth;
    private readonly Dictionary<string, Dictionary<long, Order>> _orderBooks = new();
    private readonly Dictionary<string, PriceDepth> _priceDepths = new();
    private readonly Dictionary<string, (List<KeyValuePair<int, long>> Bids, List<KeyValuePair<int, long>> Asks)> _lastSnapshots = new();
    private readonly TextWriter _outputWriter;

    public OrderBookProcessor(int priceDepth, TextWriter outputWriter)
    {
        _priceDepth = priceDepth;
        _outputWriter = outputWriter;
    }

    public void ProcessStream(Stream stream)
    {
        using var reader = new BinaryReader(stream);

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            // Read header
            var sequenceNo = reader.ReadInt32();
            var messageSize = reader.ReadInt32();
            var messageStart = reader.BaseStream.Position;

            ProcessMessage(reader, sequenceNo);

            // Ensure the reader is at the start of the next message
            reader.BaseStream.Seek(messageStart + messageSize, SeekOrigin.Begin);
        }
    }

    private void ProcessMessage(BinaryReader reader, int sequenceNo)
    {
        var messageType = reader.ReadChar();
        var symbol = Encoding.ASCII.GetString(reader.ReadBytes(3)).Trim();

        InitializeSymbol(symbol);

        var changed = messageType switch
        {
            'A' => HandleAddOrder(reader, symbol),
            'U' => HandleUpdateOrder(reader, symbol),
            'D' => HandleDeleteOrder(reader, symbol),
            'E' => HandleExecuteOrder(reader, symbol),
            _ => false
        };

        if (changed)
        {
            PrintSnapshotIfChanged(sequenceNo, symbol);
        }
    }

    private void InitializeSymbol(string symbol)
    {
        if (_orderBooks.ContainsKey(symbol)) return;
        
        _orderBooks[symbol] = new Dictionary<long, Order>();
        _priceDepths[symbol] = new PriceDepth();
        _lastSnapshots[symbol] = (new List<KeyValuePair<int, long>>(), new List<KeyValuePair<int, long>>());
    }

    private bool HandleAddOrder(BinaryReader reader, string symbol)
    {
        var addedOrderId = reader.ReadInt64();
        var side = reader.ReadChar();
        reader.ReadBytes(3); // Reserved
        var addedSize = reader.ReadInt64();
        var addedPrice = reader.ReadInt32();
        reader.ReadBytes(4); // Reserved

        var newOrder = new Order
        {
            OrderId = addedOrderId,
            Side = side,
            Symbol = symbol,
            Size = addedSize,
            Price = addedPrice
        };

        _orderBooks[symbol][addedOrderId] = newOrder;
        _priceDepths[symbol].AddOrder(newOrder);
        return true;
    }

    private bool HandleUpdateOrder(BinaryReader reader, string symbol)
    {
        var updatedOrderId = reader.ReadInt64();
        reader.ReadChar();
        reader.ReadBytes(3); // Reserved
        var updatedSize = reader.ReadInt64();
        var updatedPrice = reader.ReadInt32();
        reader.ReadBytes(4); // Reserved

        var orderBook = _orderBooks[symbol];
        if (!orderBook.ContainsKey(updatedOrderId)) return false;
        
        var oldOrder = orderBook[updatedOrderId];
        var oldSize = oldOrder.Size;
        var oldPrice = oldOrder.Price;

        oldOrder.Size = updatedSize;
        oldOrder.Price = updatedPrice;

        _priceDepths[symbol].UpdateOrder(oldOrder, oldSize, oldPrice);
        return true;
    }

    private bool HandleDeleteOrder(BinaryReader reader, string symbol)
    {
        var deletedOrderId = reader.ReadInt64();
        reader.ReadChar();
        reader.ReadBytes(3); // Reserved

        var orderBook = _orderBooks[symbol];
        if (!orderBook.ContainsKey(deletedOrderId)) return false;
        
        var deletedOrder = orderBook[deletedOrderId];
        _priceDepths[symbol].RemoveOrder(deletedOrder);
        orderBook.Remove(deletedOrderId);
        return true;
    }

    private bool HandleExecuteOrder(BinaryReader reader, string symbol)
    {
        var executedOrderId = reader.ReadInt64();
        reader.ReadChar();
        reader.ReadBytes(3); // Reserved
        var tradedQuantity = reader.ReadInt64();

        var orderBook = _orderBooks[symbol];
        if (!orderBook.ContainsKey(executedOrderId)) return false;
        
        var executedOrder = orderBook[executedOrderId];
        _priceDepths[symbol].ExecuteOrder(executedOrder, tradedQuantity);
        executedOrder.Size -= tradedQuantity;

        if (executedOrder.Size <= 0)
        {
            orderBook.Remove(executedOrderId);
        }

        return true;
    }

    private void PrintSnapshotIfChanged(int sequenceNo, string symbol)
    {
        var priceDepth = _priceDepths[symbol];
        var topBids = priceDepth.Bids.Reverse().Take(_priceDepth).ToList();
        var topAsks = priceDepth.Asks.Take(_priceDepth).ToList();

        var lastSnapshot = _lastSnapshots[symbol];
        if (lastSnapshot.Bids.SequenceEqual(topBids) && lastSnapshot.Asks.SequenceEqual(topAsks))
        {
            return;
        }

        var currentSnapshot = SnapshotExtensions.FormatSnapshot(sequenceNo, symbol, topBids, topAsks);
        
        _outputWriter.WriteLine(currentSnapshot);
        _lastSnapshots[symbol] = (topBids, topAsks);
    }
}
