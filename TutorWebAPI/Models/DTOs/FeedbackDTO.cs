using System;
using System.ComponentModel.DataAnnotations;

namespace TutorWebAPI.DTOs
{
    /// <summary>
    /// Data Transfer Object for submitting feedback.
    /// </summary>
    public class FeedbackDTO
    {
        /// <summary>
        /// The unique identifier for the feedback.
        /// </summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier of the student submitting the feedback.
        /// </summary>
        /// <example>101</example>
        public int StudentId { get; set; }
        public string StudentImg { get; set; }

        /// <summary>
        /// The unique identifier of the tutor receiving the feedback.
        /// </summary>
        /// <example>202</example>
        [Required]
        public int TutorId { get; set; }

        /// <summary>
        /// The rating given to the tutor, must be between 1 and 5.
        /// </summary>
        /// <example>4</example>
        [Range(1, 5, ErrorMessage = "Rating must be from 1 to 5.")]
        public int Rating { get; set; }

        /// <summary>
        /// Comments regarding the tutor's performance. Must not exceed 500 characters.
        /// </summary>
        /// <example>"Great tutor, very helpful!"</example>
        [Required]
        [MaxLength(500, ErrorMessage = "Comments must not exceed 500 characters.")]
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// The date and time when the feedback was created.
        /// </summary>
        /// <example>2023-04-12T14:30:00</example>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// The name of the student who submitted the feedback.
        /// </summary>
        /// <example>John Doe</example>
        public string StudentName { get; set; } = string.Empty;
    }
}
