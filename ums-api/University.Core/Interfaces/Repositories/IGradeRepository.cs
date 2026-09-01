using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface IGradeRepository
{
    Task<Grade?> GetByEnrollmentIdAsync(Guid enrollmentId);
    Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId);
    Task<decimal> CalculateGPAAsync(Guid studentId);
    Task<IEnumerable<Grade>> GetLockedGradesAsync(Guid studentId);
}
