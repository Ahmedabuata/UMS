using University.Shared.Common;
using University.Shared.DTOs.Grades;

namespace University.Core.Interfaces.Services;

public interface IGradeService
{
    Task<Result<GradeResponseDto>> SubmitGradeAsync(SubmitGradeRequestDto dto);
    Task<Result<GradeResponseDto>> UpdateGradeAsync(Guid gradeId, UpdateGradeRequestDto dto);
    Task<Result<GradeResponseDto>> LockGradeAsync(Guid gradeId);
    Task<Result<IEnumerable<GradeResponseDto>>> GetStudentGradesAsync(Guid studentId);
    Task<Result<decimal>> CalculateGPAAsync(Guid studentId);
}
