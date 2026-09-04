using University.Shared.Common;
using University.Shared.DTOs.Branches;

namespace University.Core.Interfaces.Services;

public interface IBranchService
{
    Task<Result<IEnumerable<BranchResponseDto>>> GetAllAsync();
    Task<Result<BranchResponseDto>> GetByIdAsync(Guid id);
    Task<Result<BranchResponseDto>> CreateAsync(CreateBranchRequestDto dto);
    Task<Result<BranchResponseDto>> UpdateAsync(Guid id, CreateBranchRequestDto dto);
    Task<Result<bool>> DeleteAsync(Guid id);
}