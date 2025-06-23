using TutorWebAPI.Models.DTOs;

namespace TutorWebAPI.Services
{
    public interface IScoringService
    {
        float PredictCompatibility(CompatibilityInput input);
    }
}
