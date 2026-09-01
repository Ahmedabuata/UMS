using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;
using University.Shared.Enums;

namespace University.Infrastructure.Repositories;

public class FinancialRepository : GenericRepository<FinancialRecord>, IFinancialRepository
{
    public FinancialRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<FinancialRecord?> GetByStudentAndSemesterAsync(Guid studentId, Guid? semesterId) =>
        await _dbSet.FirstOrDefaultAsync(f => f.StudentId == studentId && f.SemesterId == semesterId);

    public async Task<FinancialRecord?> GetWithPaymentsAsync(Guid id) =>
        await _dbSet.Include(f => f.Payments).FirstOrDefaultAsync(f => f.Id == id);

    public async Task<IEnumerable<FinancialRecord>> GetOverdueAsync() =>
        await _dbSet.Where(f => f.Balance > 0 && f.Status != FinancialRecordStatus.CLOSED).ToListAsync();

    public async Task<FinancialRecord?> GetByIdWithLockAsync(Guid id) =>
        await _dbSet.FromSqlRaw(
            "SELECT * FROM financial_records WHERE \"id\" = {0} FOR UPDATE", id)
            .AsNoTracking()
            .FirstOrDefaultAsync();
}
