using System;
using System.Collections.Generic;

namespace TutorWebAPI.DTOs
{
    public class AdminStatsDTO
    {
        public int TotalTutors { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public int TotalSchedules { get; set; }
        public int PendingEnrollments { get; set; }
        public int TutorsChange { get; set; } 
        public int StudentsChange { get; set; }
        public int CoursesChange { get; set; }
    }

    public class CourseStatusDTO
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class MonthlyActivityDTO
    {
        public string Month { get; set; }
        public int NewTutors { get; set; }
        public int NewStudents { get; set; }
        public int NewCourses { get; set; }
    }

    public class RecentEnrollmentDTO
    {
        public long Id { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public string Status { get; set; }
        public DateTime EnrolledAt { get; set; }
    }

    public class AdminDashboardResponse
    {
        public AdminStatsDTO Stats { get; set; }
        public List<CourseStatusDTO> CourseStatuses { get; set; }
        public List<MonthlyActivityDTO> MonthlyActivities { get; set; }
        public List<RecentEnrollmentDTO> RecentEnrollments { get; set; }
    }
}