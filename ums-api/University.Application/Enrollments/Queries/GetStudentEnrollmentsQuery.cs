using University.Application.Abstractions;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Enrollments;

namespace University.Application.Enrollments.Queries;

public class GetStudentEnrollmentsQuery : IQuery<IEnumerable<EnrollmentResponseDto>>
{
    public Guid StudentId { get; set; }
    public Guid SemesterId { get; set; }
}

public class GetStudentEnrollmentsQueryHandler
    : IQueryHandler<GetStudentEnrollmentsQuery, IEnumerable<EnrollmentResponseDto>>
{
    private readonly IEnrollmentService _enrollmentService;

    public GetStudentEnrollmentsQueryHandler(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    public async Task<Result<IEnumerable<EnrollmentResponseDto>>> HandleAsync(
        GetStudentEnrollmentsQuery query, CancellationToken cancellationToken = default)
        => await _enrollmentService.GetStudentEnrollmentsAsync(query.StudentId, query.SemesterId);
}
