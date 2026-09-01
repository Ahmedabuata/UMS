using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.AdministrativeDepartments;

namespace University.Infrastructure.Services;

public class AdministrativeDepartmentService : IAdministrativeDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public AdministrativeDepartmentService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<AdministrativeDepartmentResponseDto>> GetByIdAsync(Guid id)
    {
        var dept = await _context.AdministrativeDepartments
            .Include(d => d.Branch)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (dept == null)
        {
            return Result<AdministrativeDepartmentResponseDto>.NotFound("DEPT_NOT_FOUND", "Department not found.");
        }

        return Result<AdministrativeDepartmentResponseDto>.Success(AdministrativeDepartmentMapper.ToResponse(dept));
    }

    public async Task<Result<AdministrativeDepartmentResponseDto>> CreateAsync(CreateAdministrativeDepartmentRequestDto dto)
    {
        if (await _context.AdministrativeDepartments.AnyAsync(d => d.DepartmentCode == dto.DepartmentCode))
        {
            return Result<AdministrativeDepartmentResponseDto>.Conflict("DEPT_CODE_EXISTS", "Department code already exists.");
        }

        var branchExists = await _context.Branches.AnyAsync(b => b.Id == dto.BranchId);
        if (!branchExists)
        {
            return Result<AdministrativeDepartmentResponseDto>.Validation("BRANCH_NOT_FOUND", "Branch not found.");
        }

        var dept = new University.Core.Entities.AdministrativeDepartment
        {
            BranchId = dto.BranchId,
            DepartmentName = dto.DepartmentName,
            DepartmentCode = dto.DepartmentCode,
            Description = dto.Description,
            IsActive = true
        };

        await _unitOfWork.AdministrativeDepartments.AddAsync(dept);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(dept.Id);
    }

    public async Task<Result<AdministrativeDepartmentResponseDto>> UpdateAsync(Guid id, UpdateAdministrativeDepartmentRequestDto dto)
    {
        var dept = await _context.AdministrativeDepartments.FindAsync(id);
        if (dept == null)
        {
            return Result<AdministrativeDepartmentResponseDto>.NotFound("DEPT_NOT_FOUND", "Department not found.");
        }

        if (dto.DepartmentName != null) dept.DepartmentName = dto.DepartmentName;
        if (dto.DepartmentCode != null) dept.DepartmentCode = dto.DepartmentCode;
        if (dto.Description != null) dept.Description = dto.Description;
        if (dto.IsActive.HasValue) dept.IsActive = dto.IsActive.Value;
        dept.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.AdministrativeDepartments.UpdateAsync(dept);
        await _unitOfWork.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var dept = await _context.AdministrativeDepartments.FindAsync(id);
        if (dept == null)
        {
            return Result<bool>.NotFound("DEPT_NOT_FOUND", "Department not found.");
        }

        await _unitOfWork.AdministrativeDepartments.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<AdministrativeDepartmentResponseDto>>> GetAllAsync()
    {
        var depts = await _context.AdministrativeDepartments
            .Include(d => d.Branch)
            .Where(d => d.IsActive)
            .ToListAsync();
        return Result<IEnumerable<AdministrativeDepartmentResponseDto>>.Success(
            depts.Select(AdministrativeDepartmentMapper.ToResponse).ToList());
    }
}
