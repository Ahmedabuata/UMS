using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.AcademicDepartments;

namespace University.Infrastructure.Services;

public class AcademicDepartmentService : IAcademicDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public AcademicDepartmentService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<IEnumerable<AcademicDepartmentResponseDto>>> GetAllAsync(Guid? facultyId)
    {
        var query = _context.AcademicDepartments
            .Include(d => d.Faculty)
            .AsQueryable();
        if (facultyId is not null)
        {
            query = query.Where(d => d.FacultyId == facultyId.Value);
        }
        var departments = await query.OrderBy(d => d.DepartmentName).ToListAsync();
        return Result<IEnumerable<AcademicDepartmentResponseDto>>.Success(
            departments.Select(ToResponse).ToList());
    }

    public async Task<Result<AcademicDepartmentResponseDto>> GetByIdAsync(Guid id)
    {
        var department = await _context.AcademicDepartments
            .Include(d => d.Faculty)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (department == null)
        {
            return Result<AcademicDepartmentResponseDto>.NotFound("DEPARTMENT_NOT_FOUND", "Academic department not found.");
        }
        return Result<AcademicDepartmentResponseDto>.Success(ToResponse(department));
    }

    public async Task<Result<AcademicDepartmentResponseDto>> CreateAsync(CreateAcademicDepartmentRequestDto dto)
    {
        var code = dto.DepartmentCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _context.AcademicDepartments.AnyAsync(d => d.DepartmentCode == code))
        {
            return Result<AcademicDepartmentResponseDto>.Conflict("DEPARTMENT_CODE_EXISTS", "Department code already exists.");
        }

        if (!await _context.Faculties.AnyAsync(f => f.Id == dto.FacultyId))
        {
            return Result<AcademicDepartmentResponseDto>.Validation("INVALID_FACULTY", "Faculty must be valid.");
        }

        var department = new AcademicDepartment
        {
            FacultyId = dto.FacultyId,
            DepartmentName = dto.DepartmentName,
            DepartmentCode = code,
            HeadName = dto.HeadName,
            Description = dto.Description,
            IsActive = true
        };

        await _unitOfWork.AcademicDepartments.AddAsync(department);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(department.Id);
    }

    public async Task<Result<AcademicDepartmentResponseDto>> UpdateAsync(Guid id, CreateAcademicDepartmentRequestDto dto)
    {
        var department = await _context.AcademicDepartments.FindAsync(id);
        if (department == null)
        {
            return Result<AcademicDepartmentResponseDto>.NotFound("DEPARTMENT_NOT_FOUND", "Academic department not found.");
        }

        var code = dto.DepartmentCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _context.AcademicDepartments.AnyAsync(d => d.DepartmentCode == code && d.Id != id))
        {
            return Result<AcademicDepartmentResponseDto>.Conflict("DEPARTMENT_CODE_EXISTS", "Department code already exists.");
        }

        if (!await _context.Faculties.AnyAsync(f => f.Id == dto.FacultyId))
        {
            return Result<AcademicDepartmentResponseDto>.Validation("INVALID_FACULTY", "Faculty must be valid.");
        }

        department.FacultyId = dto.FacultyId;
        department.DepartmentName = dto.DepartmentName;
        department.DepartmentCode = code;
        department.HeadName = dto.HeadName;
        department.Description = dto.Description;
        department.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.AcademicDepartments.UpdateAsync(department);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var department = await _context.AcademicDepartments.FindAsync(id);
        if (department == null)
        {
            return Result<bool>.NotFound("DEPARTMENT_NOT_FOUND", "Academic department not found.");
        }

        _context.AcademicDepartments.Remove(department);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static AcademicDepartmentResponseDto ToResponse(AcademicDepartment d)
    {
        return new AcademicDepartmentResponseDto
        {
            Id = d.Id,
            FacultyId = d.FacultyId,
            FacultyName = d.Faculty?.FacultyName,
            DepartmentName = d.DepartmentName,
            DepartmentCode = d.DepartmentCode,
            HeadName = d.HeadName,
            Description = d.Description,
            IsActive = d.IsActive,
            CreatedAt = d.CreatedAt
        };
    }
}