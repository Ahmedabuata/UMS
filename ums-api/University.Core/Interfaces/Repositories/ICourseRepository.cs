using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface ICourseRepository
{
    Task<Course?> GetByCodeAsync(string courseCode);
    Task<Course?> GetWithPrerequisitesAsync(Guid id);
    Task<IEnumerable<CourseSection>> GetSectionsBySemesterAsync(Guid semesterId);
    Task<IEnumerable<Course>> GetAvailableForStudentAsync(Guid studentId);
}
