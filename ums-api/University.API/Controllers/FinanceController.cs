using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using University.Core.Interfaces.Services;
using University.Shared.Common;
using University.Shared.DTOs.Finance;

namespace University.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IFinancialService _financialService;

    public FinanceController(IFinancialService financialService)
    {
        _financialService = financialService;
    }

    [HttpGet("balance/{studentId:guid}")]
    public async Task<IActionResult> GetBalance(Guid studentId, Guid? semesterId)
    {
        var result = await _financialService.GetBalanceAsync(studentId, semesterId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _financialService.GetFinancialRecordAsync(id);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost("payments")]
    public async Task<IActionResult> RecordPayment([FromBody] PaymentRequestDto dto)
    {
        var result = await _financialService.RecordPaymentAsync(dto);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost("students/{studentId:guid}/scholarships/{scholarshipId:guid}")]
    public async Task<IActionResult> ApplyScholarship(Guid studentId, Guid scholarshipId)
    {
        var result = await _financialService.ApplyScholarshipAsync(studentId, scholarshipId);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, [FromQuery] decimal amount, [FromQuery] string reason)
    {
        var result = await _financialService.RefundAsync(id, amount, reason);
        return result.IsFailure ? ErrorResult(result) : Ok(result.Value);
    }

    private IActionResult ErrorResult<T>(Result<T> result)
    {
        if (result.Error is null)
        {
            return BadRequest();
        }
        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Unauthorized => Unauthorized(result.Error),
            ErrorType.Forbidden => Forbid(),
            _ => BadRequest(result.Error)
        };
    }
}
