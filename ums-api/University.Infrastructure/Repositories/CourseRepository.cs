using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;

namespace University.Infrastructure.Repositories;

public class CourseRepository : GenericRepository<Course>, ICourseRepository
{
    public CourseRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Course?> GetByCodeAsync(string courseCode) =>
        await _dbSet.FirstOrDefaultAsync(c => c.CourseCode == courseCode);

    public async Task<Course?> GetWithPrerequisitesAsync(Guid id) =>
        await _dbSet.Include(c => c.Prerequisites).ThenInclude(p => p.PrerequisiteCourse)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IEnumerable<CourseSection>> GetSectionsBySemesterAsync(Guid semesterId) =>
        await _context.CourseSections
            .Include(s => s.Course)
            .Where(s => s.SemesterId == semesterId && s.IsActive)
            .ToListAsync();

    public async Task<IEnumerable<Course>> GetAvailableForStudentAsync(Guid studentId) =>
        await _dbSet.Where(c => c.IsActive).ToListAsync();
}
