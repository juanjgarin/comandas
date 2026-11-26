using Comandas.SharedKernel.Domain;

namespace Comandas.SharedKernel.Tests;

public sealed class EntityAndAggregateRootTests
{
    [Fact]
    public void Equals_WithSameIdAndType_ReturnsTrue()
    {
        var id = Guid.NewGuid();

        var first = new TestAggregate(id, "first name");
        var second = new TestAggregate(id, "another name");

        first.Should().Be(second, "entities are compared by identity, not by their attributes");
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentIds_ReturnsFalse()
    {
        var first = new TestAggregate(Guid.NewGuid(), "same name");
        var second = new TestAggregate(Guid.NewGuid(), "same name");

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameIdButDifferentType_ReturnsFalse()
    {
        var id = Guid.NewGuid();

        Entity<Guid> aggregate = new TestAggregate(id, "name");
        Entity<Guid> otherEntity = new OtherEntity(id);

        aggregate.Should().NotBe(otherEntity);
    }

    [Fact]
    public void Raise_AddsEventToDomainEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid(), "name");

        aggregate.Rename("new name");

        aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(new TestRenamed("new name"));
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregate(Guid.NewGuid(), "name");
        aggregate.Rename("new name");

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    private sealed record TestRenamed(string NewName) : IDomainEvent;

    private sealed class TestAggregate(Guid id, string name) : AggregateRoot<Guid>(id)
    {
        public string Name { get; private set; } = name;

        public void Rename(string newName)
        {
            Name = newName;
            Raise(new TestRenamed(newName));
        }
    }

    private sealed class OtherEntity(Guid id) : Entity<Guid>(id);
}
