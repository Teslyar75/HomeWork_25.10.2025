using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace ASP_421.Data.Configuration
{
    public class UserAccessConfiguration : IEntityTypeConfiguration<Entities.UserAccess>
    {
        public void Configure(EntityTypeBuilder<Entities.UserAccess> builder)
        {
            builder
                .HasIndex(ua => ua.Login)
                .IsUnique();

            builder
                .HasOne(ua => ua.User)
                .WithMany(u => u.Accesses);

            builder
                .HasOne(ua => ua.Role)
                .WithMany()
                .HasForeignKey(ua => ua.RoleId);

            builder.HasData(new Entities.UserAccess
            {
                Id = Guid.Parse("2570A0D2-FAB2-4DE0-8EFC-E2BD28DE2502"),
                UserId = Guid.Parse("53759101-7DE4-4E04-833A-884752290FA0"),
                RoleId = "Admin",
                Login = "Admin",
                Salt = "4FA5D20B-E546-4818-9381-B4BD9F327F4E",
                Dk = "1678112717E7AF0947F6"  // pass = Admin
            });
        }
    }
}
