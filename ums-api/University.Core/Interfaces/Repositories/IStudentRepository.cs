using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface IStudentRepository
{
    Task<Student?> GetByNumberAsync(string studentNumber);
    Task<Student?> GetByUserIdAsync(Guid userId);
    Task<Student?> GetWithMajorAsync(Guid id);
    Task UpdateGPAAsync(Guid studentId, decimal gpa);
    Task<IEnumerable<Student>> GetActiveStudentsAsync();
}
