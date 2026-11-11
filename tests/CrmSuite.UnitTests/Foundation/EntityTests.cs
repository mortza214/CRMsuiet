using CrmSuite.BuildingBlocks.Domain.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Entities;
using FluentAssertions;

namespace CrmSuite.UnitTests.Foundation;

public sealed class EntityTests
{
    [Fact]
    public void Entities_with_same_id_and_type_are_equal()
    {
        var id = Guid.NewGuid();
        var left = new TestAggregate(id);
        var right = new TestAggregate(id);

        left.Should().Be(right);
        (left == right).Should().BeTrue();
    }

    [Fact]
    public void Transient_entities_are_not_equal()
    {
        var left = new TestAggregate(Guid.Empty);
        var right = new TestAggregate(Guid.Empty);

        left.Should().NotBe(right);
    }

    [Fact]
    public void Aggregate_records_and_clears_domain_events()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseForTest(new TestEvent());

        aggregate.DomainEvents.Should().ContainSingle();
        aggregate.ClearDomainEvents();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    private sealed class TestAggregate : AggregateRoot
    {
        public TestAggregate(Guid id)
        {
            Id = id;
        }

        public void RaiseForTest(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    private sealed record TestEvent : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }
}
