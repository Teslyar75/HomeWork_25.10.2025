using System.Text.Json.Serialization;

namespace ASP_421.Data.Entities
{
    public class User
    {
        public Guid      Id           { get; set; }
        public String    Name         { get; set; } = null!;
        public String    Email        { get; set; } = null!;
        public DateTime? Birthdate    { get; set; }
        public DateTime  RegisteredAt { get; set; }
        public DateTime? DeletedAt    { get; set; }

        // Inverse Navi props
        [JsonIgnore]
        public List<UserAccess> Accesses { get; set; } = new();
    }
}

