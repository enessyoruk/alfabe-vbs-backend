namespace Alfabe.Services
{
    public interface ISmsSender
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }
}
