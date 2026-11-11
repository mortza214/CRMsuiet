namespace CrmSuite.BuildingBlocks.Domain.Abstractions;

/// <summary>Marker contract for facts that happened inside the domain.</summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
