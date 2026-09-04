using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Semesters;
using University.Shared.Enums;

namespace University.Infrastructure.Services;

public class SemesterService : ISemesterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public SemesterService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<IEnumerable<SemesterResponseDto>>> GetAllAsync()
    {
        var semesters = await _context.Semesters
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartDate)
            .ToListAsync();
        return Result<IEnumerable<SemesterResponseDto>>.Success(
            semesters.Select(ToResponse).ToList());
    }

    public async Task<Result<SemesterResponseDto>> GetByIdAsync(Guid id)
    {
        var semester = await _context.Semesters.FindAsync(id);
        if (semester == null)
        {
            return Result<SemesterResponseDto>.NotFound("SEMESTER_NOT_FOUND", "Semester not found.");
        }

        return Result<SemesterResponseDto>.Success(ToResponse(semester));
    }

    public async Task<Result<SemesterResponseDto>> CreateAsync(CreateSemesterRequestDto dto)
    {
        if (await _context.Semesters.AnyAsync(s => s.SemesterCode == dto.SemesterCode))
        {
            return Result<SemesterResponseDto>.Conflict("SEMESTER_CODE_EXISTS", "Semester code already exists.");
        }

        var semester = new Semester
        {
            SemesterName = dto.SemesterName,
            SemesterCode = dto.SemesterCode,
            AcademicYear = dto.AcademicYear,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            RegistrationStart = dto.RegistrationStart,
            RegistrationEnd = dto.RegistrationEnd,
            Status = dto.Status ?? SemesterStatus.UPCOMING,
            IsActive = true
        };

        await _unitOfWork.Semesters.AddAsync(semester);
        await _unitOfWork.SaveChangesAsync();

        return Result<SemesterResponseDto>.Success(ToResponse(semester));
    }

    public async Task<Result<SemesterResponseDto>> UpdateAsync(Guid id, CreateSemesterRequestDto dto)
    {
        var semester = await _context.Semesters.FindAsync(id);
        if (semester == null)
        {
            return Result<SemesterResponseDto>.NotFound("SEMESTER_NOT_FOUND", "Semester not found.");
        }

        if (await _context.Semesters.AnyAsync(s => s.SemesterCode == dto.SemesterCode && s.Id != id))
        {
            return Result<SemesterResponseDto>.Conflict("SEMESTER_CODE_EXISTS", "Semester code already exists.");
        }

        semester.SemesterName = dto.SemesterName;
        semester.SemesterCode = dto.SemesterCode;
        semester.AcademicYear = dto.AcademicYear;
        semester.StartDate = dto.StartDate;
        semester.EndDate = dto.EndDate;
        semester.RegistrationStart = dto.RegistrationStart;
        semester.RegistrationEnd = dto.RegistrationEnd;
        semester.Status = dto.Status ?? semester.Status;
        semester.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Semesters.UpdateAsync(semester);
        await _unitOfWork.SaveChangesAsync();

        return Result<SemesterResponseDto>.Success(ToResponse(semester));
    }

    public async Task<Result<SemesterResponseDto>> OpenAsync(Guid id)
    {
        var semester = await _context.Semesters.FindAsync(id);
        if (semester == null)
        {
            return Result<SemesterResponseDto>.NotFound("SEMESTER_NOT_FOUND", "Semester not found.");
        }

        semester.Status = SemesterStatus.OPEN;
        semester.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Semesters.UpdateAsync(semester);
        await _unitOfWork.SaveChangesAsync();

        return Result<SemesterResponseDto>.Success(ToResponse(semester));
    }

    public async Task<Result<SemesterResponseDto>> CloseAsync(Guid id)
    {
        var semester = await _context.Semesters.FindAsync(id);
        if (semester == null)
        {
            return Result<SemesterResponseDto>.NotFound("SEMESTER_NOT_FOUND", "Semester not found.");
        }

        semester.Status = SemesterStatus.CLOSED;
        semester.IsCurrent = false;
        semester.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Semesters.UpdateAsync(semester);
        await _unitOfWork.SaveChangesAsync();

        return Result<SemesterResponseDto>.Success(ToResponse(semester));
    }

    public async Task<Result<SemesterResponseDto>> SetCurrentAsync(Guid id)
    {
        var semester = await _context.Semesters.FindAsync(id);
        if (semester == null)
        {
            return Result<SemesterResponseDto>.NotFound("SEMESTER_NOT_FOUND", "Semester not found.");
        }

        await _context.Semesters.ExecuteUpdateAsync(s => s.SetProperty(x => x.IsCurrent, false));
        semester.IsCurrent = true;
        semester.Status = SemesterStatus.OPEN;
        semester.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Semesters.UpdateAsync(semester);
        await _unitOfWork.SaveChangesAsync();

        return Result<SemesterResponseDto>.Success(ToResponse(semester));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var semester = await _context.Semesters.FindAsync(id);
        if (semester == null)
        {
            return Result<bool>.NotFound("SEMESTER_NOT_FOUND", "Semester not found.");
        }

        _context.Semesters.Remove(semester);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static SemesterResponseDto ToResponse(Semester s)
    {
        return new SemesterResponseDto
        {
            Id = s.Id,
            SemesterName = s.SemesterName,
            SemesterCode = s.SemesterCode,
            AcademicYear = s.AcademicYear,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            RegistrationStart = s.RegistrationStart,
            RegistrationEnd = s.RegistrationEnd,
            Status = s.Status,
            IsCurrent = s.IsCurrent,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}