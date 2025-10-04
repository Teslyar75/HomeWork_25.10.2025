namespace ASP_421.Services.Random
{
    public class DefaultRandomService : IRandomService
    {
        private System.Random _random = new();

        public String Otp(int length)
        {
            return String.Join("",
                (new byte[length])
                .Select((_) => "0123456789"[_random.Next(10)])
            );
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }
    }
}
