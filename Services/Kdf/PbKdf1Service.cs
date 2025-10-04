namespace ASP_421.Services.Kdf
{
    // By sec. 5.1. RFC 2898
    public class PbKdf1Service : IKdfService
    {
        const int c = 3;        // iteration count
        const int dkLen = 20;   // DK length in symbols

        public String Dk(String password, String salt)
        {
            String t = Hash(password + salt);
            for(int i = 0; i < c - 1; i += 1)
            {
                t = Hash(t);
            }
            return t[0..dkLen];
        }

        private static String Hash(String input) => Convert.ToHexString(
            System.Security.Cryptography.SHA1.HashData(
                System.Text.Encoding.UTF8.GetBytes(input) 
            )
        );
    }
}
