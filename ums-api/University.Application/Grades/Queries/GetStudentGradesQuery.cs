using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Grades;

namespace University.Application.Grades.Queries;

public class GetStudentGradesQuery : IQuery<IEnumerable<GradeResponseDto>>
{
    public Guid StudentId { get; set; }
}

public class GetStudentGradesQueryHandler
    : IQueryHandler<GetStudentGradesQuery, IEnumerable<GradeResponseDto>>
{
    private readonly IGradeService _gradeService;

    public GetStudentGradesQueryHandler(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    public async Task<Result<IEnumerable<GradeResponseDto>>> HandleAsync(
        GetStudentGradesQuery query, CancellationToken cancellationToken = default)
        => await _gradeService.GetStudentGradesAsync(query.StudentId);
}
