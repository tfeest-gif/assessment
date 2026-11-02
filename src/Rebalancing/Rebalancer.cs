namespace Rebalancing;

public enum ShareMode { Fractional, Whole }

public enum TradeAction { Hold, Buy, Sell }

/// <summary>One row of the account. Percentages are % of total assets.</summary>
public record Position(string Symbol, decimal TargetPct, decimal CurrentPct, decimal Price);

/// <summary>Output for one security. VariancePct = current% - target% (negative = buy, positive = sell).</summary>
public record Trade(string Symbol, decimal VariancePct, TradeAction Action, decimal Shares);

/// <summary>
/// Calculates how many shares to buy or sell so every security reaches its target %.
/// Stand-in for the application under test.
/// </summary>
public static class Rebalancer
{
    public static List<Trade> Rebalance(decimal totalAssets, IReadOnlyList<Position> positions, ShareMode mode = ShareMode.Fractional)
    {
        Validate(totalAssets, positions);
        return positions.Select(p => CalculateTrade(totalAssets, p, mode)).ToList();
    }

    private static Trade CalculateTrade(decimal total, Position p, ShareMode mode)
    {
        decimal variance = p.CurrentPct - p.TargetPct;
        decimal exactShares = Math.Abs(variance) / 100 * total / p.Price;

        decimal shares;
        if (mode == ShareMode.Fractional)
        {
            // Exact answer (zero variance), reported to 4 decimals.
            shares = Math.Round(exactShares, 4, MidpointRounding.AwayFromZero);
        }
        else if (variance > 0)
        {
            // Sell: round up, so the sales always cover the purchases (the account has no cash).
            // Never sell more than is held - that can only happen with a fractional holding.
            decimal heldShares = Math.Round(p.CurrentPct / 100 * total / p.Price, 4, MidpointRounding.AwayFromZero);
            shares = Math.Min(Math.Ceiling(exactShares), heldShares);
        }
        else
        {
            // Buy: round down, so a purchase never costs more than the sales raise.
            shares = Math.Floor(exactShares);
        }

        var action = shares == 0 ? TradeAction.Hold
            : variance > 0 ? TradeAction.Sell
            : TradeAction.Buy;
        return new Trade(p.Symbol, variance, action, shares);
    }

    private static void Validate(decimal total, IReadOnlyList<Position> positions)
    {
        if (total <= 0)
            throw new ArgumentException("Total assets must be greater than 0.");
        if (positions.Count == 0)
            throw new ArgumentException("The account holds no securities.");
        if (positions.Select(p => p.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).Count() != positions.Count)
            throw new ArgumentException("Each security may only appear once.");

        foreach (var p in positions)
        {
            if (p.Price <= 0)
                throw new ArgumentException($"{p.Symbol}: price must be greater than 0.");
            if (p.TargetPct is < 0 or > 100 || p.CurrentPct is < 0 or > 100)
                throw new ArgumentException($"{p.Symbol}: percentages must be between 0 and 100.");
        }

        if (positions.Sum(p => p.TargetPct) != 100)
            throw new ArgumentException("Target % must add up to 100.");
        if (positions.Sum(p => p.CurrentPct) != 100)
            throw new ArgumentException("Current % must add up to 100.");
    }
}
