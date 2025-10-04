namespace ASP_421.Models.User
{
    public class UserSignupViewModel
    {
        public UserSignupFormModel? FormModel { get; set; }
        public Dictionary<String, String>? ValidationErrors { get; set; }
    }
}
