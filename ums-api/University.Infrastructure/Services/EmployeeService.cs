using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Entities;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Infrastructure.Security;
using University.Shared.Common;
using University.Shared.DTOs.Employees;
using University.Shared.DTOs.Security;
using University.Shared.Enums;

namespace University.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IIdentifierGeneratorService _identifierGenerator;

    public EmployeeService(
        ApplicationDbContext context,
        IIdentifierGeneratorService identifierGenerator)
    {
        _context = context;
        _identifierGenerator = identifierGenerator;
    }

    public async Task<Result<IEnumerable<EmployeeResponseDto>>> GetAllAsync(
        string? department = null, string? branch = null,
        string? contractType = null, string? status = null, string? search = null)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Branch)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(department))
        {
            var term = department.Trim();
            query = query.Where(e => e.Department != null && (e.Department.DepartmentCode == term || e.Department.DepartmentName == term));
        }
        if (!string.IsNullOrWhiteSpace(branch))
        {
            var term = branch.Trim();
            query = query.Where(e => e.Branch != null && e.Branch.BranchName == term);
        }
        if (!string.IsNullOrWhiteSpace(contractType))
        {
            var term = contractType.Trim();
            query = query.Where(e => e.ContractType != null && e.ContractType.ToLower() == term.ToLower());
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            var term = status.Trim();
            query = query.Where(e => e.Status != null && e.Status.ToLower() == term.ToLower());
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e => e.FullName.ToLower().Contains(term) || e.EmployeeNumber.ToLower().Contains(term) || e.Email.ToLower().Contains(term) || (e.Phone != null && e.Phone.ToLower().Contains(term)));
        }

        var emps = await query.OrderBy(e => e.EmployeeNumber).ToListAsync();
        var result = new List<EmployeeResponseDto>();
        foreach(var e in emps)
        {
            var instructor = await _context.Instructors.Include(i=>i.Faculty).FirstOrDefaultAsync(i=>i.Id==e.Id);
            var dto = EmployeeMapper.ToResponse(e, instructor);
            dto.Category = ResolveCategory(e, instructor);
            result.Add(dto);
        }
        return Result<IEnumerable<EmployeeResponseDto>>.Success(result);
    }

    public async Task<Result<EmployeeResponseDto>> GetByIdAsync(Guid id)
    {
        var emp = await _context.Employees.Include(e => e.Department).Include(e => e.Branch).AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        if (emp == null) return Result<EmployeeResponseDto>.NotFound("EMPLOYEE_NOT_FOUND", "Employee not found.");
        
        var instructor = await _context.Instructors.Include(i=>i.Faculty).AsNoTracking().FirstOrDefaultAsync(i=>i.Id==id);
        var dto = EmployeeMapper.ToResponse(emp, instructor);
        dto.Category = ResolveCategory(emp, instructor);
        return Result<EmployeeResponseDto>.Success(dto);
    }

    // HR-ONLY create: creates an Employee (and, if academic/Both, a linked Instructor with the
    // SAME UUID). HR NEVER creates User accounts / roles / passwords — that is Security's
    // responsibility only (V6 C2). FullName may repeat; Email and Phone must be unique.
    public async Task<Result<EmployeeResponseDto>> CreateAsync(CreateEmployeeRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email))
            return Result<EmployeeResponseDto>.Validation("FIELDS_REQUIRED", "Email is required.");
        if (!dto.DepartmentId.HasValue || !await _context.AdministrativeDepartments.AnyAsync(d => d.Id == dto.DepartmentId.Value))
            return Result<EmployeeResponseDto>.Validation("DEPARTMENT_INVALID", "Department is required and must exist.");
        if (dto.BranchId.HasValue && !await _context.Branches.AnyAsync(b => b.Id == dto.BranchId.Value))
            return Result<EmployeeResponseDto>.Validation("BRANCH_INVALID", "Branch does not exist.");

        string employeeNumber;
        try { employeeNumber = await _identifierGenerator.GenerateEmployeeNumberAsync(dto.DepartmentId.Value); }
        catch (InvalidOperationException ex) { return Result<EmployeeResponseDto>.Validation("DEPARTMENT_CODE_MISSING", ex.Message); }

        if (await _context.Employees.AnyAsync(e => e.EmployeeNumber == employeeNumber))
            return Result<EmployeeResponseDto>.Conflict("EMPLOYEE_NUMBER_EXISTS", "Employee number already exists.");

        var email = dto.Email.Trim();
        if (await _context.Employees.AnyAsync(e => e.Email == email))
            return Result<EmployeeResponseDto>.Conflict("EMAIL_EXISTS", "Email already registered.");
        var phone = dto.Phone?.Trim();
        if (!string.IsNullOrWhiteSpace(phone) && await _context.Employees.AnyAsync(e => e.Phone == phone))
            return Result<EmployeeResponseDto>.Conflict("PHONE_EXISTS", "Phone already registered.");

        // FullName may be empty/duplicate (V6: no default "super user", no uniqueness on name).
        var employeeId = Guid.NewGuid();
        var emp = new Employee
        {
            Id = employeeId, // Source UUID (employee-first)
            EmployeeNumber = employeeNumber,
            FullName = dto.FullName?.Trim() ?? string.Empty,
            Email = email,
            Phone = phone,
            DepartmentId = dto.DepartmentId,
            BranchId = dto.BranchId,
            ContractType = dto.ContractType,
            Status = dto.Status,
            HireDate = dto.HireDate,
            IsActive = true
        };
        _context.Employees.Add(emp);

        // Academic / Both: create a linked Instructor row with the SAME UUID.
        var academicRank = AcademicTitleToRank(dto.AcademicTitle?.Trim());
        if (academicRank.HasValue)
        {
            _context.Instructors.Add(new Instructor
            {
                Id = employeeId, // SAME UUID as the employee.
                InstructorNumber = employeeNumber, // unique per employee (mirrors employee's number)
                AcademicRank = academicRank.Value,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(employeeId);
    }

    public async Task<Result<EmployeeResponseDto>> UpdateAsync(Guid id, UpdateEmployeeRequestDto dto)
    {
        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (emp == null) return Result<EmployeeResponseDto>.NotFound("EMPLOYEE_NOT_FOUND", "Employee not found.");

        Guid? newDepartmentId = dto.DepartmentId.HasValue && dto.DepartmentId.Value != Guid.Empty ? dto.DepartmentId.Value : (Guid?)null;
        if (newDepartmentId.HasValue && newDepartmentId.Value != emp.DepartmentId)
        {
            if (!await _context.AdministrativeDepartments.AnyAsync(d => d.Id == newDepartmentId.Value))
                return Result<EmployeeResponseDto>.Validation("DEPARTMENT_INVALID", "Department must exist.");
            try { emp.EmployeeNumber = await _identifierGenerator.GenerateEmployeeNumberAsync(newDepartmentId.Value); }
            catch (InvalidOperationException ex) { return Result<EmployeeResponseDto>.Validation("DEPARTMENT_CODE_MISSING", ex.Message); }
            emp.DepartmentId = newDepartmentId.Value;
        }
        else if (newDepartmentId.HasValue) emp.DepartmentId = newDepartmentId.Value;

        if (dto.BranchId.HasValue)
        {
            var branchId = dto.BranchId.Value != Guid.Empty ? dto.BranchId.Value : (Guid?)null;
            if (branchId.HasValue && !await _context.Branches.AnyAsync(b => b.Id == branchId.Value))
                return Result<EmployeeResponseDto>.Validation("BRANCH_INVALID", "Branch does not exist.");
            emp.BranchId = branchId;
        }
        if (!string.IsNullOrWhiteSpace(dto.FullName)) emp.FullName = dto.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var email = dto.Email.Trim();
            if (await _context.Employees.AnyAsync(e => e.Email == email && e.Id != id))
                return Result<EmployeeResponseDto>.Conflict("EMAIL_EXISTS", "Email already registered.");
            emp.Email = email;
        }
        if (dto.Phone != null) emp.Phone = dto.Phone;
        if (dto.ContractType != null) emp.ContractType = dto.ContractType;
        if (!string.IsNullOrWhiteSpace(dto.Status)) emp.Status = dto.Status;
        if (dto.HireDate.HasValue) emp.HireDate = dto.HireDate;
        if (dto.IsActive.HasValue) emp.IsActive = dto.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(dto.AcademicTitle))
        {
            var rank = AcademicTitleToRank(dto.AcademicTitle.Trim());
            if (rank.HasValue)
            {
                var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.Id == id);
                if (instructor != null)
                {
                    instructor.AcademicRank = rank.Value;
                }
                else
                {
                    // Upsert: create the Instructor row (existing tables) so the employee
                    // becomes/includes an Academic role ("Academic" or "Both").
                    _context.Instructors.Add(new Instructor
                    {
                        Id = id, // SAME UUID as the employee.
                        InstructorNumber = emp.EmployeeNumber,
                        AcademicRank = rank.Value,
                        IsActive = true
                    });
                }
            }
        }
        emp.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, Guid currentUserId)
    {
        if (!await IsSuperAdminAsync(currentUserId))
            return Result<bool>.Forbidden("HR_CANNOT_DELETE", "HR Manager cannot delete. Only change status.");

        var emp = await _context.Employees
            .Include(e => e.User)
                .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (emp == null) return Result<bool>.NotFound("EMPLOYEE_NOT_FOUND", "Employee not found.");

        if (IsProtectedEmployee(emp))
            return Result<bool>.Forbidden("SUPER_ADMIN_PROTECTED", "Cannot delete/deactivate Super Admin - Protected account.");

        if (id == currentUserId)
            return Result<bool>.Forbidden("SELF_DELETE", "Cannot delete your own account.");

        emp.IsActive = false; emp.Status = "Inactive"; emp.UpdatedAt = DateTime.UtcNow;
        _context.Employees.Update(emp); await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }
    public async Task<Result<EmployeeResponseDto>> DeactivateAsync(Guid id, Guid currentUserId)
    {
        if (!await IsSuperAdminAsync(currentUserId))
            return Result<EmployeeResponseDto>.Forbidden("HR_CANNOT_DEACTIVATE", "HR Manager cannot deactivate. Only change status.");

        var emp = await _context.Employees
            .Include(e => e.User)
                .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (emp == null) return Result<EmployeeResponseDto>.NotFound("EMPLOYEE_NOT_FOUND", "Employee not found.");

        if (IsProtectedEmployee(emp))
            return Result<EmployeeResponseDto>.Forbidden("SUPER_ADMIN_PROTECTED", "Cannot delete/deactivate Super Admin - Protected account.");

        if (id == currentUserId)
            return Result<EmployeeResponseDto>.Forbidden("SELF_DEACTIVATE", "Cannot deactivate your own account.");

        emp.Status = "Inactive"; emp.IsActive = false; emp.UpdatedAt = DateTime.UtcNow;
        _context.Employees.Update(emp); await _context.SaveChangesAsync(); return await GetByIdAsync(id);
    }
    public async Task<Result<EmployeeResponseDto>> ActivateAsync(Guid id) { var emp = await _context.Employees.FindAsync(id); if (emp == null) return Result<EmployeeResponseDto>.NotFound("EMPLOYEE_NOT_FOUND", "Employee not found."); emp.Status = "Active"; emp.IsActive = true; emp.UpdatedAt = DateTime.UtcNow; _context.Employees.Update(emp); await _context.SaveChangesAsync(); return await GetByIdAsync(id); }
    public async Task<Result<EmployeeResponseDto>> RestoreAsync(Guid id) { var emp = await _context.Employees.FindAsync(id); if (emp == null) return Result<EmployeeResponseDto>.NotFound("EMPLOYEE_NOT_FOUND", "Employee not found."); emp.IsActive = true; emp.Status = "Active"; emp.UpdatedAt = DateTime.UtcNow; _context.Employees.Update(emp); await _context.SaveChangesAsync(); return await GetByIdAsync(id); }

    public async Task<Result<IEnumerable<EmployeeResponseDto>>> GetByAcademicTitleAsync(string? academicTitle = null)
    {
        var instructorsQuery = _context.Instructors.Include(i=>i.Faculty).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(academicTitle))
        {
            var target = AcademicTitleToRank(academicTitle.Trim());
            if (target.HasValue) instructorsQuery = instructorsQuery.Where(i => i.AcademicRank == target.Value);
        }
        var instructorIds = await instructorsQuery.Select(i=>i.Id).ToListAsync();
        var emps = await _context.Employees.Include(e=>e.Department).Include(e=>e.Branch).Where(e=>instructorIds.Contains(e.Id)).AsNoTracking().OrderBy(e=>e.FullName).ToListAsync();
        var rows = new List<EmployeeResponseDto>();
        foreach(var e in emps)
        {
            var instructor = await _context.Instructors.Include(i=>i.Faculty).FirstOrDefaultAsync(i=>i.Id==e.Id);
            var dto = EmployeeMapper.ToResponse(e, instructor);
            dto.Category = ResolveCategory(e, instructor);
            rows.Add(dto);
        }
        return Result<IEnumerable<EmployeeResponseDto>>.Success(rows);
    }

    // Read-only derived category from existing data (no DB column):
    // has linked Instructor => academic; has an admin Department => administrative; both => "Both".
    private static string ResolveCategory(Employee emp, Instructor? instructor)
    {
        bool hasInstructor = instructor != null;
        bool hasAdminDept = emp.DepartmentId.HasValue && emp.DepartmentId.Value != Guid.Empty;
        if (hasInstructor && hasAdminDept) return "Both";
        return hasInstructor ? "Academic" : "Administrative";
    }

    // Only SuperAdmin may delete/deactivate accounts (Golden Rule: HR cannot delete, only change status).
    private async Task<bool> IsSuperAdminAsync(Guid userId)
    {
        if (userId == Guid.Empty) return false;
        var roleName = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Role!.RoleName)
            .FirstOrDefaultAsync();
        return string.Equals(roleName, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase);
    }

    // Super Admin is a protected account (role SUPER_ADMIN or the seeded admin@ums.com email).
    private static bool IsProtectedEmployee(Employee emp)
    {
        var roleName = emp.User?.Role?.RoleName;
        bool isSuperAdmin = string.Equals(roleName, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)
            || string.Equals(emp.Email, "admin@ums.com", StringComparison.OrdinalIgnoreCase);
        return isSuperAdmin;
    }

    private static AcademicRank? AcademicTitleToRank(string title) => title switch
    {
        "محاضر" => AcademicRank.LECTURER,
        "أستاذ مساعد" => AcademicRank.ASSISTANT_PROFESSOR,
        "أستاذ مشارك" => AcademicRank.ASSOCIATE_PROFESSOR,
        "أستاذ دكتور" => AcademicRank.PROFESSOR,
        _ => null
    };
    public async Task<Result<string>> PreviewEmployeeNumberAsync(Guid administrativeDepartmentId)
    {
        try { var number = await _identifierGenerator.GenerateEmployeeNumberAsync(administrativeDepartmentId); return Result<string>.Success(number); }
        catch (InvalidOperationException ex) { return Result<string>.Validation("DEPARTMENT_CODE_MISSING", ex.Message); }
    }

    public async Task<Result<IEnumerable<EmployeeAccountOptionDto>>> GetWithoutUserAccountsAsync()
    {
        var employeeIdsWithUser = _context.Users
            .AsNoTracking()
            .Select(u => u.Id);

        var options = await _context.Employees
            .AsNoTracking()
            .Where(e => !employeeIdsWithUser.Contains(e.Id))
            .OrderBy(e => e.EmployeeNumber)
            .Select(e => new EmployeeAccountOptionDto
            {
                Id = e.Id,
                EmployeeNumber = e.EmployeeNumber,
                FullName = e.FullName,
                Email = e.Email
            })
            .ToListAsync();

        return Result<IEnumerable<EmployeeAccountOptionDto>>.Success(options);
    }
}