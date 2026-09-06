using HR.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace HR.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly HrDbContext _db;
    public BranchesController(HrDbContext db) => _db = db;
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _db.Branches.ToListAsync());
}
