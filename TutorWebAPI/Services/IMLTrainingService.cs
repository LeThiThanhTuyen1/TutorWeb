using Microsoft.ML;

namespace TutorWebAPI.Services
{
    public interface IMLTrainingService
    {
        void TrainModel();
        ITransformer LoadModel();
    }
}
