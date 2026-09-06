using HR.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HR.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AdministrativeDepartmentsController : ControllerBase
{
    private readonly HrDbContext _db;
    public AdministrativeDepartmentsController(HrDbContext db) => _db = db;
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _db.AdministrativeDepartments.ToListAsync());
}
