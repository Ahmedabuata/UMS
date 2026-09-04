using University.Shared.Common;
using University.Shared.DTOs.Semesters;

namespace University.Core.Interfaces.Services;

public interface ISemesterService
{
    Task<Result<IEnumerable<SemesterResponseDto>>> GetAllAsync();
    Task<Result<SemesterResponseDto>> GetByIdAsync(Guid id);
    Task<Result<SemesterResponseDto>> CreateAsync(CreateSemesterRequestDto dto);
    Task<Result<SemesterResponseDto>> UpdateAsync(Guid id, CreateSemesterRequestDto dto);
    Task<Result<SemesterResponseDto>> OpenAsync(Guid id);
    Task<Result<SemesterResponseDto>> CloseAsync(Guid id);
    Task<Result<SemesterResponseDto>> SetCurrentAsync(Guid id);
    Task<Result<bool>> DeleteAsync(Guid id);
}