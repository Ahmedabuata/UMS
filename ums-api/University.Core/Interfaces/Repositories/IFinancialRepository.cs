using University.Core.Entities;

namespace University.Core.Interfaces.Repositories;

public interface IFinancialRepository
{
    Task<FinancialRecord?> GetByStudentAndSemesterAsync(Guid studentId, Guid? semesterId);
    Task<FinancialRecord?> GetWithPaymentsAsync(Guid id);
    Task<IEnumerable<FinancialRecord>> GetOverdueAsync();
    Task<FinancialRecord?> GetByIdWithLockAsync(Guid id);
}
