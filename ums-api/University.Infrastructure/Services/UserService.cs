using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Infrastructure.Security;
using University.Shared.Common;
using University.Shared.DTOs.Users;

namespace University.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ApplicationDbContext _context;

    public UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _context = context;
    }

    public async Task<Result<UserResponseDto>> GetUserByIdAsync(Guid id)
    {
        var user = await _unitOfWork.UserRepository.GetWithRoleAsync(id);
        if (user == null)
        {
            return Result<UserResponseDto>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        return Result<UserResponseDto>.Success(UserMapper.ToResponse(user));
    }

    public async Task<Result<UserResponseDto>> CreateUserAsync(CreateUserRequestDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return Result<UserResponseDto>.Conflict("EMAIL_EXISTS", "Email already registered.");
        }

        var user = new University.Core.Entities.User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            BranchId = dto.BranchId,
            RoleId = dto.RoleId,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.UserRepository.GetWithRoleAsync(user.Id);
        return Result<UserResponseDto>.Success(UserMapper.ToResponse(saved!));
    }

    public async Task<Result<UserResponseDto>> UpdateUserAsync(Guid id, UpdateUserRequestDto dto)
    {
        var user = await _unitOfWork.UserRepository.GetWithRoleAsync(id);
        if (user == null)
        {
            return Result<UserResponseDto>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.BranchId.HasValue) user.BranchId = dto.BranchId;
        if (dto.RoleId.HasValue) user.RoleId = dto.RoleId.Value;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.UserRepository.GetWithRoleAsync(id);
        return Result<UserResponseDto>.Success(UserMapper.ToResponse(saved!));
    }

    public async Task<Result<bool>> DeleteUserAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        await _unitOfWork.Users.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> AssignRoleAsync(Guid userId, Guid roleId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Result<bool>.NotFound("USER_NOT_FOUND", "User not found.");
        }

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<UserResponseDto>>> GetByBranchAsync(Guid branchId)
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .Where(u => u.BranchId == branchId && u.IsActive)
            .ToListAsync();
        return Result<IEnumerable<UserResponseDto>>.Success(users.Select(UserMapper.ToResponse).ToList());
    }
}
