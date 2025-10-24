using System.Text.Json.Serialization;

namespace ASP_421.Data.Entities
{
    public class User
    {
        public Guid      Id           { get; set; }
        public String    Name         { get; set; } = null!;
        public String    Email        { get; set; } = null!;
        public String    Avatar       { get; set; } = string.Empty;
        public DateTime? Birthdate    { get; set; }
        public DateTime  RegisteredAt { get; set; }
        public DateTime  RegisterDt   { get; set; } // Для совместимости
        public DateTime? DeletedAt    { get; set; }
        public DateTime? UpdatedAt    { get; set; }

        // Inverse Navi props
        [JsonIgnore]
        public List<UserAccess> Accesses { get; set; } = new();
    }
}

