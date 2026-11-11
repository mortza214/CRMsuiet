using CrmSuite.Modules.Foundation.Infrastructure.Persistence.Configurations;
using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : BaseEntityConfiguration<Department>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.Property(department => department.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(department => department.ParentId)
            .HasColumnName("parent_id");

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(department => department.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(department => new { department.ParentId, department.Name })
            .IsUnique()
            .HasDatabaseName("ux_departments_parent_name");

        builder.HasMany(department => department.UserDepartments)
            .WithOne(userDepartment => userDepartment.Department)
            .HasForeignKey(userDepartment => userDepartment.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
