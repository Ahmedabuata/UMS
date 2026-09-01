using University.Shared.DTOs.Enrollments;

namespace University.Application.Validators.Enrollments;

public class EnrollRequestDtoValidator
{
    public IDictionary<string, string[]> Validate(EnrollRequestDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (dto.StudentId == Guid.Empty)
        {
            errors["StudentId"] = new[] { "StudentId is required." };
        }

        if (dto.SectionId == Guid.Empty)
        {
            errors["SectionId"] = new[] { "SectionId is required." };
        }

        if (dto.SemesterId == Guid.Empty)
        {
            errors["SemesterId"] = new[] { "SemesterId is required." };
        }

        return errors;
    }
}
