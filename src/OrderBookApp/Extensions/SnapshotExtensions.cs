using System.Collections.Generic;
using System.Linq;

namespace OrderBookApp.Extensions;

public static class SnapshotExtensions
{
    public static string FormatSnapshot(int sequenceNo, string symbol, List<KeyValuePair<int, long>> bids,
        List<KeyValuePair<int, long>> asks)
    {
        var bidStrings = bids.Select(b => $"({b.Key}, {b.Value})");
        var askStrings = asks.Select(a => $"({a.Key}, {a.Value})");

        string bidsFormatted = string.Join(", ", bidStrings);
        string asksFormatted = string.Join(", ", askStrings);

        return $"{sequenceNo}, {symbol}, [{bidsFormatted}], [{asksFormatted}]";
    }
}
