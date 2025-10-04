namespace ASP_421.Services.Random
{
    public interface IRandomService
    {
        String Otp(int length);   // One Time Password (code)
        int Next(int maxValue);   // Random number from 0 to maxValue-1
    }
}
