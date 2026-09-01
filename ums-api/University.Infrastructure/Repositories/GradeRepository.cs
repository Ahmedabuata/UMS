using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;
using University.Shared.Enums;

namespace University.Infrastructure.Repositories;

public class GradeRepository : GenericRepository<Grade>, IGradeRepository
{
    public GradeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Grade?> GetByEnrollmentIdAsync(Guid enrollmentId) =>
        await _dbSet.FirstOrDefaultAsync(g => g.EnrollmentId == enrollmentId);

    public async Task<IEnumerable<Grade>> GetByStudentIdAsync(Guid studentId) =>
        await _dbSet
            .Include(g => g.Enrollment).ThenInclude(e => e!.Section).ThenInclude(s => s!.Course)
            .Where(g => g.Enrollment!.StudentId == studentId)
            .ToListAsync();

    public async Task<decimal> CalculateGPAAsync(Guid studentId)
    {
        var grades = await _dbSet
            .Include(g => g.Enrollment)
            .Where(g => g.Enrollment!.StudentId == studentId &&
                        g.Enrollment.Status == EnrollmentStatus.COMPLETED &&
                        g.GradePoints.HasValue)
            .Select(g => new
            {
                Points = g.GradePoints!.Value,
                Credits = g.Enrollment!.Section!.Course!.CreditHours
            })
            .ToListAsync();

        var totalCredits = grades.Sum(x => x.Credits);
        if (totalCredits == 0)
        {
            return 0m;
        }

        var weighted = grades.Sum(x => x.Points * x.Credits) / totalCredits;
        return decimal.Round(weighted, 2);
    }

    public async Task<IEnumerable<Grade>> GetLockedGradesAsync(Guid studentId) =>
        await _dbSet
            .Include(g => g.Enrollment).ThenInclude(e => e!.Section).ThenInclude(s => s!.Course)
            .Where(g => g.Enrollment!.StudentId == studentId && g.IsLocked)
            .ToListAsync();
}
