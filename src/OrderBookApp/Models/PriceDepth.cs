using System.Collections.Generic;

namespace OrderBookApp.Models;

// Represents the price depth for a specific symbol.
public class PriceDepth
{
    public Dictionary<int, long> Bids { get; set; } = new();
    public Dictionary<int, long> Asks { get; set; } = new();

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
