using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Grades;

namespace University.Application.Grades.Commands;

public class SubmitGradeCommand : ICommand<GradeResponseDto>
{
    public SubmitGradeRequestDto Dto { get; set; } = new();
}

public class SubmitGradeCommandHandler : ICommandHandler<SubmitGradeCommand, GradeResponseDto>
{
    private readonly IGradeService _gradeService;

    public SubmitGradeCommandHandler(IGradeService gradeService)
    {
        _gradeService = gradeService;
    }

    public Task<Result<GradeResponseDto>> HandleAsync(
        SubmitGradeCommand command, CancellationToken cancellationToken = default)
        => _gradeService.SubmitGradeAsync(command.Dto);
}
