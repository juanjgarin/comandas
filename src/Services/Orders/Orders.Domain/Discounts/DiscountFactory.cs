namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// Builds every candidate discount for an order and returns the one that saves the most
/// (decision D6: discounts are not combinable, the best one wins).
/// </summary>
/// <remarks>
/// Candidates, in tie-break order: the manual percentage (an explicit decision of the waiter), happy hour,
/// then 2-for-1. Adding a new kind of discount means adding one more candidate here; the strategies and the
/// order stay untouched.
/// </remarks>
public sealed class DiscountFactory : IDiscountFactory
{
    private readonly HappyHourDiscount _happyHour;
    private readonly TwoForOneDiscount _twoForOne = new();

    public DiscountFactory(TimeProvider clock, DiscountPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(policy);

        // Built once: an invalid configuration fails at startup, not in the middle of a customer's bill.
        _happyHour = new HappyHourDiscount(clock, policy.HappyHourStart, policy.HappyHourEnd, policy.HappyHourPercentage);
    }

    public IDiscountStrategy CreateBestFor(DiscountContext context, DiscountRequest request)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return Candidates(request)
            .Select(strategy => (Strategy: strategy, Amount: strategy.CalculateDiscount(context).Amount))
            .Where(candidate => candidate.Amount > 0)
            .OrderByDescending(candidate => candidate.Amount) // stable sort: on a tie, the earlier candidate wins
            .Select(candidate => candidate.Strategy)
            .FirstOrDefault()
            ?? NoDiscount.Instance;
    }

    private IEnumerable<IDiscountStrategy> Candidates(DiscountRequest request)
    {
        if (request.ManualPercentage is { } percentage)
        {
            yield return new PercentageDiscount(percentage); // validates the requested percentage
        }

        yield return _happyHour;
        yield return _twoForOne;
    }
}
