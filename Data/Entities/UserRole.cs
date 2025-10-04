namespace ASP_421.Data.Entities
{
    public class UserRole
    {
        public String Id          { get; set; } = null!; 
        public String Description { get; set; } = null!;
        public bool   CanCreate   { get; set; }
        public bool   CanRead     { get; set; }
        public bool   CanUpdate   { get; set; }
        public bool   CanDelete   { get; set; }
    }
}
