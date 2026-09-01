using University.Core.Entities;
using University.Shared.DTOs.Courses;

namespace University.Application.Mapping;

public static class CourseMapper
{
    public static CourseResponseDto ToResponse(Course course) => new()
    {
        Id = course.Id,
        CourseCode = course.CourseCode,
        CourseName = course.CourseName,
        Description = course.Description,
        CreditHours = course.CreditHours,
        LectureHours = course.LectureHours,
        LabHours = course.LabHours,
        MaxStudents = course.MaxStudents,
        IsActive = course.IsActive,
        CreatedAt = course.CreatedAt,
        Prerequisites = course.Prerequisites
            .Where(p => p.PrerequisiteCourse != null)
            .Select(p => new CourseDto
            {
                Id = p.PrerequisiteCourseId,
                CourseCode = p.PrerequisiteCourse!.CourseCode,
                CourseName = p.PrerequisiteCourse.CourseName,
                CreditHours = p.PrerequisiteCourse.CreditHours
            }).ToList()
    };
}
