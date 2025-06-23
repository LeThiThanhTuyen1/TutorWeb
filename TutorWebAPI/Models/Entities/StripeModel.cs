namespace TutorWebAPI.Models.Entities
{
    public class StripeModel
    {
        public string SecretKey { get; set; }
        public string PublishableKey { get; set; }
        public string WebhookSecret { get; set; }
    }
}
