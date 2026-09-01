using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface IEnrollmentRepository
{
    Task<IEnumerable<CourseEnrollment>> GetByStudentAndSemesterAsync(Guid studentId, Guid semesterId);
    Task<bool> ExistsAsync(Guid studentId, Guid sectionId, Guid semesterId);
    Task<IEnumerable<CourseEnrollment>> GetWithGradesAsync(Guid studentId);
    Task<CourseEnrollment?> GetWithDetailsAsync(Guid id);
    Task<CourseEnrollment?> GetByIdWithLockAsync(Guid id);
}
