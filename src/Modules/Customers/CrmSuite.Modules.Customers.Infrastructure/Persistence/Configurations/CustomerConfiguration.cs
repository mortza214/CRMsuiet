using CrmSuite.Modules.Customers.Domain.Entities;
using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Customers.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : BaseEntityConfiguration<Customer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.Property(customer => customer.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(customer => customer.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100);

        builder.Property(customer => customer.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100);

        builder.Property(customer => customer.CompanyName)
            .HasColumnName("company_name")
            .HasMaxLength(200);

        builder.Property(customer => customer.EconomicCode)
            .HasColumnName("economic_code")
            .HasMaxLength(20);

        builder.Property(customer => customer.NationalId)
            .HasColumnName("national_id")
            .HasMaxLength(11)
            .IsRequired();

        builder.HasIndex(customer => customer.NationalId)
            .IsUnique()
            .HasDatabaseName("ux_customers_national_id");

        builder.Property(customer => customer.Mobile)
            .HasColumnName("mobile")
            .HasMaxLength(11)
            .IsRequired();

        builder.HasIndex(customer => customer.Mobile)
            .IsUnique()
            .HasDatabaseName("ux_customers_mobile");

        builder.Property(customer => customer.Phone)
            .HasColumnName("phone")
            .HasMaxLength(30);

        builder.Property(customer => customer.Email)
            .HasColumnName("email")
            .HasMaxLength(256);

        builder.Property(customer => customer.Address)
            .HasColumnName("address")
            .HasMaxLength(500);

        builder.Property(customer => customer.OwnerType)
            .HasColumnName("owner_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(customer => customer.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(200);

        builder.Property(customer => customer.PreferredNotificationChannel)
            .HasColumnName("preferred_notification_channel")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(customer => customer.OwnerUserId)
            .HasColumnName("owner_user_id");

        builder.HasIndex(customer => customer.OwnerUserId)
            .HasDatabaseName("ix_customers_owner_user_id");

        builder.Property(customer => customer.ParentCustomerId)
            .HasColumnName("parent_customer_id");

        builder.HasIndex(customer => customer.ParentCustomerId)
            .HasDatabaseName("ix_customers_parent_customer_id");

        builder.Property(customer => customer.IsReseller)
            .HasColumnName("is_reseller")
            .IsRequired();

        builder.HasIndex(customer => customer.IsReseller)
            .HasDatabaseName("ix_customers_is_reseller");

        builder.Property(customer => customer.GroupId)
            .HasColumnName("group_id");

        builder.HasOne(customer => customer.Group)
            .WithMany()
            .HasForeignKey(customer => customer.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(customer => customer.UserId)
            .HasColumnName("user_id");

        builder.HasIndex(customer => customer.UserId)
            .IsUnique()
            .HasDatabaseName("ux_customers_user_id");

        builder.HasMany(customer => customer.TagMaps)
            .WithOne(map => map.Customer)
            .HasForeignKey(map => map.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
