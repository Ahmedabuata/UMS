using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface ISemesterRepository
{
    Task<Semester?> GetCurrentAsync();
    Task<IEnumerable<Semester>> GetByYearAsync(string academicYear);
    Task<bool> IsValidForEnrollmentAsync(Guid semesterId);
}
