using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;

namespace University.Infrastructure.Repositories;

public class StudentRepository : GenericRepository<Student>, IStudentRepository
{
    public StudentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Student?> GetByNumberAsync(string studentNumber) =>
        await _dbSet.FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);

    // A student links to a user via Student.UserId (NOT a shared primary key).
    public async Task<Student?> GetByUserIdAsync(Guid userId) =>
        await _dbSet.FirstOrDefaultAsync(s => s.UserId == userId);

    public async Task<Student?> GetWithMajorAsync(Guid id) =>
        await _dbSet.Include(s => s.Major).FirstOrDefaultAsync(s => s.Id == id);

    public async Task UpdateGPAAsync(Guid studentId, decimal gpa)
    {
        var student = await _dbSet.FindAsync(studentId);
        if (student != null)
        {
            student.Gpa = gpa;
            student.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<IEnumerable<Student>> GetActiveStudentsAsync() =>
        await _dbSet.Where(s => s.IsActive).ToListAsync();
}
