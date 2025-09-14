namespace OrderBookApp.Models;

// Represents a single order in the order book.
public class Order
{
    public long OrderId { get; set; }
    public char Side { get; set; } // 'B' for Bid, 'S' for Ask
    public string Symbol { get; set; }
    public long Size { get; set; }
    public int Price { get; set; }
}
