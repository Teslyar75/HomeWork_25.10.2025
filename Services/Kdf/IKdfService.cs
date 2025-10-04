namespace ASP_421.Services.Kdf
{
    // Key Derivation Service by RFC 2898 https://datatracker.ietf.org/doc/html/rfc2898
    public interface IKdfService
    {
        String Dk(String password, String salt);   // Derived Key
    }
}
