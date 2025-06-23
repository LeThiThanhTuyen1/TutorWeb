namespace TutorWebAPI.DTOs
{
    public class ProfileDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Image { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string School { get; set; }
        public string Location { get; set; }
        public string Gender { get; set; }
        public string Role { get; set; }
        public StudentInfoDTO? StudentInfo { get; set; }
        public TutorInfoDTO? TutorInfo { get; set; }
    }

    public class TutorInfoDTO
    {
        public float Experience { get; set; }
        public string Subjects { get; set; }
        public string Introduction { get; set; }
        public double Rating { get; set; }
    }

    public class StudentInfoDTO
    {
        public int? Class { get; set; }
    }
}
