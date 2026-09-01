using University.Shared.Constants;
using University.Shared.DTOs.Grades;

namespace University.Application.Validators.Grades;

public class SubmitGradeRequestDtoValidator
{
    public IDictionary<string, string[]> Validate(SubmitGradeRequestDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (dto.EnrollmentId == Guid.Empty)
        {
            errors["EnrollmentId"] = new[] { "EnrollmentId is required." };
        }

        if (dto.MidtermScore.HasValue &&
            (dto.MidtermScore.Value < GradeConstants.MinScore || dto.MidtermScore.Value > GradeConstants.MaxScore))
        {
            errors["MidtermScore"] = new[] { $"MidtermScore must be between {GradeConstants.MinScore} and {GradeConstants.MaxScore}." };
        }

        if (dto.FinalScore.HasValue &&
            (dto.FinalScore.Value < GradeConstants.MinScore || dto.FinalScore.Value > GradeConstants.MaxScore))
        {
            errors["FinalScore"] = new[] { $"FinalScore must be between {GradeConstants.MinScore} and {GradeConstants.MaxScore}." };
        }

        if (!dto.MidtermScore.HasValue && !dto.FinalScore.HasValue)
        {
            errors["Scores"] = new[] { "At least one of MidtermScore or FinalScore is required." };
        }

        return errors;
    }
}
