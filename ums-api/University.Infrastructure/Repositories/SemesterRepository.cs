using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;

namespace University.Infrastructure.Repositories;

public class SemesterRepository : GenericRepository<Semester>, ISemesterRepository
{
    public SemesterRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Semester?> GetCurrentAsync() =>
        await _dbSet.FirstOrDefaultAsync(s => s.IsCurrent && s.IsActive);

    public async Task<IEnumerable<Semester>> GetByYearAsync(string academicYear) =>
        await _dbSet.Where(s => s.AcademicYear == academicYear && s.IsActive).ToListAsync();

    public async Task<bool> IsValidForEnrollmentAsync(Guid semesterId)
    {
        var semester = await _dbSet.FindAsync(semesterId);
        if (semester == null || !semester.IsActive)
        {
            return false;
        }

        if (semester.IsCurrent)
        {
            return true;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return today >= semester.StartDate && today <= semester.EndDate;
    }
}
