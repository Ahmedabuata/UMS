using DotNetCore.CAP;
using HR.Infrastructure.Data;
using HR.Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HR.Infrastructure.Messaging;

public class UserCreatedEventConsumer : ICapSubscribe
{
    private readonly HrDbContext _db;
    private readonly ILogger<UserCreatedEventConsumer> _logger;
    
    public UserCreatedEventConsumer(HrDbContext db, ILogger<UserCreatedEventConsumer> logger)
    {
        _db = db; 
        _logger = logger;
    }
    
    [CapSubscribe("ums.user.created")]
    public async Task HandleUserCreated(UserCreatedEvent @event)
    {
        if (await _db.Employees.AnyAsync(e => e.ExternalUserId == @event.ExternalUserId)) return;
        
        var emp = new HR.Domain.Entities.Employee
        {
            ExternalUserId = @event.ExternalUserId,
            FullName = @event.FullName,
            Email = @event.Email,
            Phone = @event.Phone,
            Status = "Active",
            EmployeeNumber = $"ADM-HR-{DateTime.UtcNow:yyyyMM}-{await _db.Employees.CountAsync() + 1:D5}"
        };
        _db.Employees.Add(emp);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Auto-created {Number} for {UserId}", emp.EmployeeNumber, @event.ExternalUserId);
    }
}
