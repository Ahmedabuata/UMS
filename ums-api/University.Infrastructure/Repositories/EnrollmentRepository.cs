using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;

namespace University.Infrastructure.Repositories;

public class EnrollmentRepository : GenericRepository<CourseEnrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<CourseEnrollment>> GetByStudentAndSemesterAsync(Guid studentId, Guid semesterId) =>
        await _dbSet
            .Include(e => e.Section).ThenInclude(s => s.Course)
            .Include(e => e.Semester)
            .Where(e => e.StudentId == studentId && e.SemesterId == semesterId && e.IsActive)
            .ToListAsync();

    public async Task<bool> ExistsAsync(Guid studentId, Guid sectionId, Guid semesterId) =>
        await _dbSet.AnyAsync(e =>
            e.StudentId == studentId && e.SectionId == sectionId && e.SemesterId == semesterId && e.IsActive);

    public async Task<IEnumerable<CourseEnrollment>> GetWithGradesAsync(Guid studentId) =>
        await _dbSet
            .Include(e => e.Section).ThenInclude(s => s.Course)
            .Include(e => e.Grade)
            .Where(e => e.StudentId == studentId && e.IsActive)
            .ToListAsync();

    public async Task<CourseEnrollment?> GetWithDetailsAsync(Guid id) =>
        await _dbSet
            .Include(e => e.Student).ThenInclude(s => s!)
            .Include(e => e.Section).ThenInclude(s => s.Course)
            .Include(e => e.Semester)
            .FirstOrDefaultAsync(e => e.Id == id);

    // FOR UPDATE for concurrency - locks the row in the transaction
    public async Task<CourseEnrollment?> GetByIdWithLockAsync(Guid id) =>
        await _dbSet.FromSqlRaw(
            "SELECT * FROM course_enrollments WHERE \"id\" = {0} FOR UPDATE", id)
            .AsNoTracking()
            .FirstOrDefaultAsync();
}

