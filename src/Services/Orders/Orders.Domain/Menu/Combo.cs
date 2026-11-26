using System.Globalization;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Menu;

/// <summary>
/// A menu item made of other menu items (Composite pattern): a combo IS a <see cref="MenuItem"/>
/// and CONTAINS menu items, which can be dishes or other combos. Callers treat it exactly like a dish;
/// the combo answers by recursively asking its components.
/// Price = sum of the components' subtotals minus the combo discount percentage.
/// </summary>
public sealed class Combo : MenuItem
{
    /// <summary>A "combo" of a single unit would just be a dish.</summary>
    public const int MinimumUnits = 2;

    private readonly List<ComboComponent> _components = [];

    private Combo(Guid id, string name, decimal discountPercentage)
        : base(id, name)
    {
        DiscountPercentage = ValidateDiscount(discountPercentage);
    }

    // Parameterless constructor for EF Core materialization only.
    private Combo()
    {
    }

    /// <summary>Discount applied over the sum of the components, e.g. 15 means 15%.</summary>
    public decimal DiscountPercentage { get; private set; }

    public IReadOnlyCollection<ComboComponent> Components => _components.AsReadOnly();

    /// <summary>A combo can only be ordered if it is available AND every component can be ordered (recursive).</summary>
    public override bool CanBeOrdered => IsAvailable && _components.TrueForAll(component => component.Item.CanBeOrdered);

    public static Combo Create(
        string name,
        decimal discountPercentage,
        IReadOnlyCollection<(MenuItem Item, Quantity Quantity)> components)
    {
        ArgumentNullException.ThrowIfNull(components);

        var combo = new Combo(Guid.CreateVersion7(), name, discountPercentage);

        foreach (var (item, quantity) in components)
        {
            combo.AddComponentUnchecked(item, quantity);
        }

        combo.EnsureMinimumUnits();
        return combo;
    }

    public override Money GetPrice()
    {
        // Recursion happens here: a component's subtotal asks its item for GetPrice(),
        // which is either a Dish (leaf) or another Combo (composite).
        var subtotal = _components
            .Select(component => component.GetSubtotal())
            .Aggregate((total, next) => total + next);

        var discount = subtotal * (DiscountPercentage / 100m);
        return subtotal - discount;
    }

    public override bool Contains(MenuItem item) =>
        _components.Exists(component => component.Item == item || component.Item.Contains(item));

    public void AddComponent(MenuItem item, Quantity quantity) => AddComponentUnchecked(item, quantity);

    public void RemoveComponent(Guid menuItemId)
    {
        var component = _components.Find(c => c.Item.Id == menuItemId)
            ?? throw new DomainException($"Combo '{Name}' does not contain the menu item {menuItemId}.");

        if (TotalUnits() - component.Quantity.Value < MinimumUnits)
        {
            throw new DomainException($"A combo must contain at least {MinimumUnits} units; remove would leave too few.");
        }

        _components.Remove(component);
    }

    public void ChangeDiscount(decimal discountPercentage) =>
        DiscountPercentage = ValidateDiscount(discountPercentage);

    private void AddComponentUnchecked(MenuItem item, Quantity quantity)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(quantity);

        // Cycle guard: "A contains B contains A" would make GetPrice() recurse forever.
        if (item == this || item.Contains(this))
        {
            throw new DomainException($"Combo '{Name}' cannot contain itself, directly or through another combo.");
        }

        if (_components.Count > 0 && item.GetPrice().Currency != _components[0].Item.GetPrice().Currency)
        {
            throw new DomainException($"All components of combo '{Name}' must use the same currency.");
        }

        var existing = _components.Find(c => c.Item == item);
        if (existing is not null)
        {
            existing.Increase(quantity); // Same item twice: merge into one entry ("1 × Burger" + "1 × Burger" = "2 × Burger").
            return;
        }

        _components.Add(new ComboComponent(item, quantity));
    }

    private void EnsureMinimumUnits()
    {
        if (TotalUnits() < MinimumUnits)
        {
            throw new DomainException($"A combo must contain at least {MinimumUnits} units in total.");
        }
    }

    private int TotalUnits() => _components.Sum(component => component.Quantity.Value);

    private static decimal ValidateDiscount(decimal discountPercentage)
    {
        if (discountPercentage is < 0 or >= 100)
        {
            throw new DomainException(
                $"Combo discount must be at least 0% and below 100% (was {discountPercentage.ToString(CultureInfo.InvariantCulture)}%).");
        }

        return discountPercentage;
    }
}
