using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Menu;

/// <summary>
/// Anything that can be ordered from the menu (abstract base of the hierarchy).
/// Holds what every item shares and leaves the price calculation to each subclass:
/// callers simply ask <see cref="GetPrice"/> without knowing the concrete type (polymorphism).
/// </summary>
public abstract class MenuItem : AggregateRoot<Guid>
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 100;

    protected MenuItem(Guid id, string name)
        : base(id)
    {
        Name = ValidateName(name);
        IsAvailable = true;
    }

    // Parameterless constructor for EF Core materialization only.
    protected MenuItem()
    {
        Name = string.Empty; // Set by EF Core when loading from the database.
    }

    public string Name { get; private set; }

    /// <summary>False when the kitchen ran out of it: it stays on the menu but cannot be ordered.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Flag used by the "2-for-1" discount (phase 2).</summary>
    public bool IsTwoForOneEligible { get; private set; }

    /// <summary>
    /// Whether an order may include this item right now. A dish only needs to be available;
    /// a combo also needs every component to be orderable (Composite).
    /// </summary>
    public virtual bool CanBeOrdered => IsAvailable;

    /// <summary>The price of one unit of this item. Each kind of item calculates it differently.</summary>
    public abstract Money GetPrice();

    /// <summary>
    /// Whether <paramref name="item"/> is part of this item's tree. A leaf contains nothing;
    /// a combo searches its components recursively (used to prevent cycles).
    /// </summary>
    public virtual bool Contains(MenuItem item) => false;

    public void Rename(string newName) => Name = ValidateName(newName);

    public void MarkAvailable() => IsAvailable = true;

    public void MarkUnavailable() => IsAvailable = false;

    public void EnableTwoForOne() => IsTwoForOneEligible = true;

    public void DisableTwoForOne() => IsTwoForOneEligible = false;

    private static string ValidateName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length is < NameMinLength or > NameMaxLength)
        {
            throw new DomainException(
                $"Menu item name must be between {NameMinLength} and {NameMaxLength} characters (was '{name}').");
        }

        return trimmed;
    }
}
