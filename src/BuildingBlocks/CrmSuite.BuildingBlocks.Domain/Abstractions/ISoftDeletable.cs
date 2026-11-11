namespace CrmSuite.BuildingBlocks.Domain.Abstractions;

/// <summary>Soft deletion keeps financial history and audit trails intact.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAtUtc { get; set; }

    Guid? DeletedBy { get; set; }
}
