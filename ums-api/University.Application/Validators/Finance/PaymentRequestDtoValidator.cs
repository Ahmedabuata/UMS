using University.Shared.Constants;
using University.Shared.DTOs.Finance;

namespace University.Application.Validators.Finance;

public class PaymentRequestDtoValidator
{
    public IDictionary<string, string[]> Validate(PaymentRequestDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (dto.FinancialId == Guid.Empty)
        {
            errors["FinancialId"] = new[] { "FinancialId is required." };
        }

        if (dto.Amount <= 0)
        {
            errors["Amount"] = new[] { $"Amount must be greater than {PaymentConstants.MinAmount}." };
        }

        var paymentMethods = Enum.GetValues<University.Shared.Enums.PaymentMethod>();
        if (!paymentMethods.Contains(dto.PaymentMethod))
        {
            errors["PaymentMethod"] = new[] { "PaymentMethod is invalid." };
        }

        return errors;
    }
}
