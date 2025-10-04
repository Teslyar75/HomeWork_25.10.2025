namespace ASP_421.Models.User
{
    public class UserProfileViewModel
    {
        public  Data.Entities.User? User{ get; set; }
        
        public bool IsPersonal { get; set; }
    }
}
