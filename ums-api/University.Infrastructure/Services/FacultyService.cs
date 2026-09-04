using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.AcademicDepartments;
using University.Shared.DTOs.Faculties;

namespace University.Infrastructure.Services;

public class FacultyService : IFacultyService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public FacultyService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<IEnumerable<FacultyResponseDto>>> GetAllAsync()
    {
        var faculties = await _context.Faculties
            .Include(f => f.Branch)
            .Include(f => f.AcademicDepartments)
            .OrderBy(f => f.FacultyName)
            .ToListAsync();
        return Result<IEnumerable<FacultyResponseDto>>.Success(faculties.Select(ToResponse).ToList());
    }

    public async Task<Result<FacultyResponseDto>> GetByIdAsync(Guid id)
    {
        var faculty = await _context.Faculties
            .Include(f => f.Branch)
            .Include(f => f.AcademicDepartments)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (faculty == null)
        {
            return Result<FacultyResponseDto>.NotFound("FACULTY_NOT_FOUND", "Faculty not found.");
        }
        return Result<FacultyResponseDto>.Success(ToResponse(faculty));
    }

    public async Task<Result<IEnumerable<AcademicDepartmentResponseDto>>> GetAcademicDepartmentsAsync(Guid facultyId)
    {
        var exists = await _context.Faculties.AnyAsync(f => f.Id == facultyId);
        if (!exists)
        {
            return Result<IEnumerable<AcademicDepartmentResponseDto>>.NotFound("FACULTY_NOT_FOUND", "Faculty not found.");
        }

        var departments = await _context.AcademicDepartments
            .Include(d => d.Faculty)
            .Where(d => d.FacultyId == facultyId)
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();
        return Result<IEnumerable<AcademicDepartmentResponseDto>>.Success(
            departments.Select(ToDepartmentResponse).ToList());
    }

    public async Task<Result<FacultyResponseDto>> CreateAsync(CreateFacultyRequestDto dto)
    {
        var code = dto.FacultyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _context.Faculties.AnyAsync(f => f.FacultyCode == code))
        {
            return Result<FacultyResponseDto>.Conflict("FACULTY_CODE_EXISTS", "Faculty code already exists.");
        }

        if (!await _context.Branches.AnyAsync(b => b.Id == dto.BranchId))
        {
            return Result<FacultyResponseDto>.Validation("INVALID_BRANCH", "Branch must be valid.");
        }

        var faculty = new Faculty
        {
            FacultyName = dto.FacultyName,
            FacultyCode = code,
            DeanName = dto.DeanName,
            BranchId = dto.BranchId,
            Location = dto.Location,
            Description = dto.Description,
            IsActive = true
        };

        await _unitOfWork.Faculties.AddAsync(faculty);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(faculty.Id);
    }

    public async Task<Result<FacultyResponseDto>> UpdateAsync(Guid id, CreateFacultyRequestDto dto)
    {
        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty == null)
        {
            return Result<FacultyResponseDto>.NotFound("FACULTY_NOT_FOUND", "Faculty not found.");
        }

        var code = dto.FacultyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _context.Faculties.AnyAsync(f => f.FacultyCode == code && f.Id != id))
        {
            return Result<FacultyResponseDto>.Conflict("FACULTY_CODE_EXISTS", "Faculty code already exists.");
        }

        if (!await _context.Branches.AnyAsync(b => b.Id == dto.BranchId))
        {
            return Result<FacultyResponseDto>.Validation("INVALID_BRANCH", "Branch must be valid.");
        }

        faculty.FacultyName = dto.FacultyName;
        faculty.FacultyCode = code;
        faculty.DeanName = dto.DeanName;
        faculty.BranchId = dto.BranchId;
        faculty.Location = dto.Location;
        faculty.Description = dto.Description;
        faculty.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Faculties.UpdateAsync(faculty);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var faculty = await _context.Faculties.FindAsync(id);
        if (faculty == null)
        {
            return Result<bool>.NotFound("FACULTY_NOT_FOUND", "Faculty not found.");
        }

        _context.Faculties.Remove(faculty);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static FacultyResponseDto ToResponse(Faculty faculty)
    {
        return new FacultyResponseDto
        {
            Id = faculty.Id,
            FacultyName = faculty.FacultyName,
            FacultyCode = faculty.FacultyCode,
            DeanName = faculty.DeanName,
            BranchId = faculty.BranchId,
            BranchName = faculty.Branch?.BranchName,
            BranchCode = faculty.Branch?.BranchCode,
            Location = faculty.Location,
            Description = faculty.Description,
            DepartmentsCount = faculty.AcademicDepartments?.Count(d => d.IsActive) ?? 0,
            IsActive = faculty.IsActive,
            CreatedAt = faculty.CreatedAt
        };
    }

    private static AcademicDepartmentResponseDto ToDepartmentResponse(AcademicDepartment d)
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