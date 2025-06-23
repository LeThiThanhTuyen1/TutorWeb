using Microsoft.ML.Data;

namespace TutorWebAPI.Models.DTOs
{
    public class CompatibilityInput
    {
        public float ClassLevel { get; set; } 
        public float LocationMatch { get; set; } 
        public float SubjectMatch { get; set; }
        public float Experience { get; set; }
        public float Rating { get; set; }
    }

    public class CompatibilityPrediction
    {
        [ColumnName("Score")]
        public float Score { get; set; }
    }
}
