using CrmSuite.BuildingBlocks.Domain.Abstractions;
using CrmSuite.BuildingBlocks.Domain.Entities;

namespace CrmSuite.Modules.Foundation.Domain.Entities;

/// <summary>
/// Foundation entity base with UTC audit fields and soft-delete state.
/// The API projects these values to UTC and Jalali fields; the database stores UTC only.
/// </summary>
public abstract class BaseEntity : Entity<Guid>, IAuditableEntity, ISoftDeletable
{
    protected BaseEntity()
        : this(Guid.NewGuid())
    {
    }

    protected BaseEntity(Guid id)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CreatedAt = DateTime.UtcNow;
    }

    public DateTime CreatedAt { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }

    public Guid? CreatedByUserId { get; protected set; }

    public Guid? UpdatedByUserId { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public DateTime? DeletedAt { get; protected set; }

    public Guid? DeletedByUserId { get; protected set; }

    DateTime IAuditableEntity.CreatedAtUtc
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    Guid? IAuditableEntity.CreatedBy
    {
        get => CreatedByUserId;
        set => CreatedByUserId = value;
    }

    DateTime? IAuditableEntity.UpdatedAtUtc
    {
        get => UpdatedAt;
        set => UpdatedAt = value;
    }

    Guid? IAuditableEntity.UpdatedBy
    {
        get => UpdatedByUserId;
        set => UpdatedByUserId = value;
    }

    bool ISoftDeletable.IsDeleted
    {
        get => IsDeleted;
        set => IsDeleted = value;
    }

    DateTime? ISoftDeletable.DeletedAtUtc
    {
        get => DeletedAt;
        set => DeletedAt = value;
    }

    Guid? ISoftDeletable.DeletedBy
    {
        get => DeletedByUserId;
        set => DeletedByUserId = value;
    }

    public void MarkCreated(Guid? userId, DateTime utcNow)
    {
        CreatedAt = utcNow;
        CreatedByUserId = userId;
    }

    public void MarkUpdated(Guid? userId, DateTime utcNow)
    {
        UpdatedAt = utcNow;
        UpdatedByUserId = userId;
    }

    public void MarkDeleted(Guid? userId, DateTime utcNow)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = utcNow;
        DeletedByUserId = userId;
        MarkUpdated(userId, utcNow);
    }

    public void Restore(Guid? userId, DateTime utcNow)
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedByUserId = null;
        MarkUpdated(userId, utcNow);
    }
}
