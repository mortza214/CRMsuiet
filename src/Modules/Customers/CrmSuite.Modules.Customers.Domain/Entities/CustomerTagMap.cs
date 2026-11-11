namespace CrmSuite.Modules.Customers.Domain.Entities;

public sealed class CustomerTagMap
{
    private CustomerTagMap()
    {
    }

    public CustomerTagMap(Guid customerId, Guid tagId)
    {
        CustomerId = customerId;
        TagId = tagId;
    }

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public Guid TagId { get; private set; }

    public CustomerTag Tag { get; private set; } = null!;
}
