namespace CrmSuite.BuildingBlocks.Domain.Abstractions;

/// <summary>Timestamps are stored in UTC; the API is responsible for Jalali presentation fields.</summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }

    Guid? CreatedBy { get; set; }

    DateTime? UpdatedAtUtc { get; set; }

    Guid? UpdatedBy { get; set; }
}
