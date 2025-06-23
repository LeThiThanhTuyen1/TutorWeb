namespace TutorWebAPI.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmail(string email, string code);
    }
}