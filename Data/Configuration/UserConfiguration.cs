using ASP_421.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ASP_421.Data.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasData(new User
            {
                Id = Guid.Parse("53759101-7DE4-4E04-833A-884752290FA0"),
                Name = "Root Administrator",
                Email = "admin@i.ua",
                Birthdate = DateTime.UnixEpoch,
                RegisteredAt = DateTime.UnixEpoch,
            });
        }
    }
}
