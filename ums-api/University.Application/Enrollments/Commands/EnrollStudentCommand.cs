using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Enrollments;

namespace University.Application.Enrollments.Commands;

public class EnrollStudentCommand : ICommand<EnrollmentResponseDto>
{
    public EnrollRequestDto Dto { get; set; } = new();
}

public class EnrollStudentCommandHandler : ICommandHandler<EnrollStudentCommand, EnrollmentResponseDto>
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollStudentCommandHandler(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    public Task<Result<EnrollmentResponseDto>> HandleAsync(
        EnrollStudentCommand command, CancellationToken cancellationToken = default)
        => _enrollmentService.EnrollAsync(command.Dto);
}
