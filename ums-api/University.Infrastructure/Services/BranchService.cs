using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Branches;

namespace University.Infrastructure.Services;

public class BranchService : IBranchService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public BranchService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<IEnumerable<BranchResponseDto>>> GetAllAsync()
    {
        var branches = await _context.Branches
            .OrderBy(b => b.BranchName)
            .ToListAsync();
        return Result<IEnumerable<BranchResponseDto>>.Success(branches.Select(ToResponse).ToList());
    }

    public async Task<Result<BranchResponseDto>> GetByIdAsync(Guid id)
    {
        var branch = await _context.Branches.FindAsync(id);
        if (branch == null)
        {
            return Result<BranchResponseDto>.NotFound("BRANCH_NOT_FOUND", "Branch not found.");
        }
        return Result<BranchResponseDto>.Success(ToResponse(branch));
    }

    public async Task<Result<BranchResponseDto>> CreateAsync(CreateBranchRequestDto dto)
    {
        var code = dto.BranchCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _context.Branches.AnyAsync(b => b.BranchCode == code))
        {
            return Result<BranchResponseDto>.Conflict("BRANCH_CODE_EXISTS", "Branch code already exists.");
        }

        var branch = new Branch
        {
            BranchName = dto.BranchName,
            BranchCode = code,
            BranchLocation = dto.BranchLocation,
            BranchDescription = dto.BranchDescription,
            IsActive = true
        };

        await _unitOfWork.Branches.AddAsync(branch);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(branch.Id);
    }

    public async Task<Result<BranchResponseDto>> UpdateAsync(Guid id, CreateBranchRequestDto dto)
    {
        var branch = await _context.Branches.FindAsync(id);
        if (branch == null)
        {
            return Result<BranchResponseDto>.NotFound("BRANCH_NOT_FOUND", "Branch not found.");
        }

        var code = dto.BranchCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _context.Branches.AnyAsync(b => b.BranchCode == code && b.Id != id))
        {
            return Result<BranchResponseDto>.Conflict("BRANCH_CODE_EXISTS", "Branch code already exists.");
        }

        branch.BranchName = dto.BranchName;
        branch.BranchCode = code;
        branch.BranchLocation = dto.BranchLocation;
        branch.BranchDescription = dto.BranchDescription;
        branch.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Branches.UpdateAsync(branch);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var branch = await _context.Branches.FindAsync(id);
        if (branch == null)
        {
            return Result<bool>.NotFound("BRANCH_NOT_FOUND", "Branch not found.");
        }

        _context.Branches.Remove(branch);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static BranchResponseDto ToResponse(Branch branch)
    {
        return new BranchResponseDto
        {
            Id = branch.Id,
            BranchName = branch.BranchName,
            BranchCode = branch.BranchCode,
            BranchLocation = branch.BranchLocation,
            BranchDescription = branch.BranchDescription,
            IsActive = branch.IsActive,
            CreatedAt = branch.CreatedAt
        };
    }
}