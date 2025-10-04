using ASP_421.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ASP_421.Data.Configuration
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.HasData(new UserRole
            {
                Id = "Admin",
                Description = "Root System Administration",
                CanCreate = true,
                CanDelete = true,
                CanRead = true,
                CanUpdate = true,
            });
            builder.HasData(new UserRole
            {
                Id = "Guest",
                Description = "Self Registered User",
                CanCreate = false,
                CanDelete = false,
                CanRead = false,
                CanUpdate = false,
            });
            builder.HasData(new UserRole
            {
                Id = "Editor",
                Description = "Content Editor and Moderator",
                CanCreate = false,
                CanDelete = true,
                CanRead = true,
                CanUpdate = true,
            });
        }
    }
}
