using Microsoft.ML;
using System;
using TutorWebAPI.Models.DTOs;

namespace TutorWebAPI.Services
{
    public class ScoringService : IScoringService
    {
        private readonly MLContext _mlContext;
        private readonly IMLTrainingService _mlTrainingService;
        private readonly PredictionEngine<CompatibilityInput, CompatibilityPrediction> _predictionEngine;

        public ScoringService(IMLTrainingService mlTrainingService)
        {
            _mlContext = new MLContext(seed: 0);
            _mlTrainingService = mlTrainingService;

            var model = _mlTrainingService.LoadModel();
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<CompatibilityInput, CompatibilityPrediction>(model);
        }

        public float PredictCompatibility(CompatibilityInput input)
        {
            var prediction = _predictionEngine.Predict(input);
            return prediction.Score;
        }
    }

    public class CompatibilityPrediction
    {
        public float Score { get; set; }
    }
}