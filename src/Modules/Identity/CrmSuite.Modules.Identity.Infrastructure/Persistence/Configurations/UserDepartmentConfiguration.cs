using CrmSuite.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrmSuite.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserDepartmentConfiguration : IEntityTypeConfiguration<UserDepartment>
{
    public void Configure(EntityTypeBuilder<UserDepartment> builder)
    {
        builder.ToTable("user_departments");

        builder.HasKey(userDepartment => new { userDepartment.UserId, userDepartment.DepartmentId });

        builder.Property(userDepartment => userDepartment.UserId)
            .HasColumnName("user_id");

        builder.Property(userDepartment => userDepartment.DepartmentId)
            .HasColumnName("department_id");

        builder.HasOne(userDepartment => userDepartment.User)
            .WithMany(user => user.UserDepartments)
            .HasForeignKey(userDepartment => userDepartment.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(userDepartment => userDepartment.Department)
            .WithMany(department => department.UserDepartments)
            .HasForeignKey(userDepartment => userDepartment.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(userDepartment => userDepartment.DepartmentId)
            .HasDatabaseName("ix_user_departments_department_id");

        builder.HasQueryFilter(userDepartment =>
            !userDepartment.User.IsDeleted && !userDepartment.Department.IsDeleted);
    }
}
