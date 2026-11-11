using CrmSuite.BuildingBlocks.Domain.Exceptions;
using CrmSuite.Modules.Foundation.Domain.Entities;

namespace CrmSuite.Modules.Catalog.Domain.Entities;

public sealed class ServiceFormLink : BaseEntity
{
    private ServiceFormLink()
    {
    }

    public ServiceFormLink(
        Guid serviceId,
        Guid formDefinitionId,
        bool isRequired,
        int sortOrder,
        Guid? createdByUserId,
        DateTime createdAtUtc)
        : base(Guid.NewGuid())
    {
        if (serviceId == Guid.Empty || formDefinitionId == Guid.Empty)
        {
            throw new DomainException("اتصال فرم به سرویس معتبر نیست.", "CATALOG_FORM_LINK_INVALID");
        }

        ServiceId = serviceId;
        FormDefinitionId = formDefinitionId;
        IsRequired = isRequired;
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid ServiceId { get; private set; }

    public Guid FormDefinitionId { get; private set; }

    public bool IsRequired { get; private set; }

    public int SortOrder { get; private set; }

    public void Update(bool isRequired, int sortOrder, Guid? userId, DateTime utcNow)
    {
        IsRequired = isRequired;
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
        MarkUpdated(userId, utcNow);
    }
}
