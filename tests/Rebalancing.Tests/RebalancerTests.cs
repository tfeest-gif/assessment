using Rebalancing;

namespace Rebalancing.Tests;

public class RebalancerTests
{
    // Account ABC from the assignment: $100,000, 100% vested
    private const decimal Total = 100_000m;

    private static readonly Position[] AccountAbc =
    [
        new("IBM", 20, 10, 150),
        new("MSFT", 20, 20, 90),
        new("ORCL", 20, 30, 220),
        new("AAPL", 20, 20, 450),
        new("HD", 20, 20, 70),
    ];

    private static Dictionary<string, (TradeAction Action, decimal Shares)> Run(
        decimal total, ShareMode mode, params Position[] positions) =>
        Rebalancer.Rebalance(total, positions, mode).ToDictionary(t => t.Symbol, t => (t.Action, t.Shares));

    // TC-01  IBM: buy $10,000 / $150 = 66.67   ORCL: sell $10,000 / $220 = 45.45
    [Theory]
    [InlineData("IBM", TradeAction.Buy, 66.6667)]
    [InlineData("MSFT", TradeAction.Hold, 0)]
    [InlineData("ORCL", TradeAction.Sell, 45.4545)]
    [InlineData("AAPL", TradeAction.Hold, 0)]
    [InlineData("HD", TradeAction.Hold, 0)]
    public void AccountAbc_fractional_shares(string symbol, TradeAction action, double shares)
    {
        var result = Run(Total, ShareMode.Fractional, AccountAbc);

        Assert.Equal((action, (decimal)shares), result[symbol]);
    }

    // TC-02  Whole shares: sells round up, buys round down -> ORCL 46, IBM 66
    [Theory]
    [InlineData("IBM", TradeAction.Buy, 66)]
    [InlineData("MSFT", TradeAction.Hold, 0)]
    [InlineData("ORCL", TradeAction.Sell, 46)]
    [InlineData("AAPL", TradeAction.Hold, 0)]
    [InlineData("HD", TradeAction.Hold, 0)]
    public void AccountAbc_whole_shares(string symbol, TradeAction action, int shares)
    {
        var result = Run(Total, ShareMode.Whole, AccountAbc);

        Assert.Equal((action, (decimal)shares), result[symbol]);
    }

    // TC-03
    [Fact]
    public void AccountAbc_target_variance()
    {
        var variances = Rebalancer.Rebalance(Total, AccountAbc).Select(t => t.VariancePct);

        Assert.Equal(new decimal[] { -10, 0, 10, 0, 0 }, variances);
    }

    // TC-04  Applying the fractional trades brings every security to exactly its target.
    [Fact]
    public void Fractional_trades_reach_zero_variance()
    {
        var trades = Rebalancer.Rebalance(Total, AccountAbc, ShareMode.Fractional);

        foreach (var (p, t) in AccountAbc.Zip(trades))
        {
            decimal signedShares = t.Action == TradeAction.Sell ? -t.Shares : t.Shares;
            decimal newValue = p.CurrentPct / 100 * Total + signedShares * p.Price;
            Assert.InRange(newValue, 19_999.99m, 20_000.01m);
        }
    }

    // TC-05  The account has no cash: whole-share purchases must be paid for by the sales.
    [Fact]
    public void Whole_share_buys_are_paid_for_by_the_sells()
    {
        var trades = Rebalancer.Rebalance(Total, AccountAbc, ShareMode.Whole);

        decimal sold = AccountAbc.Zip(trades).Where(x => x.Second.Action == TradeAction.Sell).Sum(x => x.Second.Shares * x.First.Price);
        decimal bought = AccountAbc.Zip(trades).Where(x => x.Second.Action == TradeAction.Buy).Sum(x => x.Second.Shares * x.First.Price);

        Assert.Equal(10_120m, sold);   // 46 x $220
        Assert.Equal(9_900m, bought);  // 66 x $150 -> $220 stays as cash
    }

    // TC-06
    [Fact]
    public void Balanced_account_needs_no_trades()
    {
        var balanced = AccountAbc.Select(p => p with { CurrentPct = p.TargetPct }).ToArray();

        var trades = Rebalancer.Rebalance(Total, balanced);

        Assert.All(trades, t => Assert.Equal((TradeAction.Hold, 0m), (t.Action, t.Shares)));
    }

    // TC-07  Several buys and sells; exact whole numbers are not rounded
    [Fact]
    public void Several_buys_and_sells()
    {
        var result = Run(200_000, ShareMode.Whole,
            new("AAA", 30, 10, 100),   // buy  $40,000 / 100 = 400
            new("BBB", 30, 20, 250),   // buy  $20,000 / 250 = 80
            new("CCC", 20, 40, 80),    // sell $40,000 / 80  = 500
            new("DDD", 20, 30, 125));  // sell $20,000 / 125 = 160

        Assert.Equal((TradeAction.Buy, 400m), result["AAA"]);
        Assert.Equal((TradeAction.Buy, 80m), result["BBB"]);
        Assert.Equal((TradeAction.Sell, 500m), result["CCC"]);
        Assert.Equal((TradeAction.Sell, 160m), result["DDD"]);
    }

    // TC-08  Target 0% sells the whole position; current 0% opens a new one
    [Fact]
    public void Full_liquidation_and_new_position()
    {
        var result = Run(50_000, ShareMode.Whole,
            new("OLD", 0, 50, 100),    // sell all $25,000 / 100 = 250
            new("NEW", 100, 50, 50));  // buy $25,000 / 50 = 500

        Assert.Equal((TradeAction.Sell, 250m), result["OLD"]);
        Assert.Equal((TradeAction.Buy, 500m), result["NEW"]);
    }

    // TC-09  Needed trade is less than one share -> nothing to buy in whole-share mode
    [Fact]
    public void Trade_smaller_than_one_share_is_a_hold()
    {
        var result = Run(10_000, ShareMode.Whole,
            new("PRICY", 50, 49, 5000),  // buy $100 / 5000 = 0.02
            new("CHEAP", 50, 51, 20));   // sell $100 / 20 = 5

        Assert.Equal((TradeAction.Hold, 0m), result["PRICY"]);
        Assert.Equal((TradeAction.Sell, 5m), result["CHEAP"]);
    }

    // TC-10  Rounding a sell up must never sell more than the account holds
    [Fact]
    public void Rounding_up_never_sells_more_than_held()
    {
        var result = Run(1_000, ShareMode.Whole,
            new("AAA", 3, 19, 100),   // holds 1.9 shares, needs to sell 1.6 -> 2 would be too many
            new("BBB", 97, 81, 10));  // buy $160 / 10 = 16

        Assert.Equal((TradeAction.Sell, 1.9m), result["AAA"]);
        Assert.Equal((TradeAction.Buy, 16m), result["BBB"]);
    }

    // TC-11  Decimal percentages: decimal arithmetic, no floating-point errors
    [Fact]
    public void Decimal_percentages()
    {
        var result = Run(90_000, ShareMode.Fractional,
            new("AAA", 33.34m, 40, 12.50m),  // sell $5,994 / 12.50 = 479.52
            new("BBB", 33.33m, 30, 7.25m),   // buy  $2,997 / 7.25  = 413.3793
            new("CCC", 33.33m, 30, 300));    // buy  $2,997 / 300   = 9.99

        Assert.Equal((TradeAction.Sell, 479.52m), result["AAA"]);
        Assert.Equal((TradeAction.Buy, 413.3793m), result["BBB"]);
        Assert.Equal((TradeAction.Buy, 9.99m), result["CCC"]);
    }
}
