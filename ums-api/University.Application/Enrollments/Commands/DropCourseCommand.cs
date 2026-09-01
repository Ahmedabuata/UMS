using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;

namespace University.Application.Enrollments.Commands;

public class DropCourseCommand : ICommand<bool>
{
    public Guid EnrollmentId { get; set; }
    public string? Reason { get; set; }
}

public class DropCourseCommandHandler : ICommandHandler<DropCourseCommand, bool>
{
    private readonly IEnrollmentService _enrollmentService;

    public DropCourseCommandHandler(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    public Task<Result<bool>> HandleAsync(
        DropCourseCommand command, CancellationToken cancellationToken = default)
        => _enrollmentService.DropAsync(command.EnrollmentId, command.Reason);
}
