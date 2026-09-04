using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Classrooms;

namespace University.Infrastructure.Services;

public class ClassroomService : IClassroomService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public ClassroomService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<ClassroomResponseDto>> GetByIdAsync(Guid id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null)
        {
            return Result<ClassroomResponseDto>.NotFound("CLASSROOM_NOT_FOUND", "Classroom not found.");
        }

        return Result<ClassroomResponseDto>.Success(ToResponse(classroom));
    }

    public async Task<Result<ClassroomResponseDto>> CreateAsync(CreateClassroomRequestDto dto)
    {
        if (dto.BuildingId.HasValue)
        {
            var buildingExists = await _context.Buildings.AnyAsync(b => b.Id == dto.BuildingId.Value);
            if (!buildingExists)
            {
                return Result<ClassroomResponseDto>.Validation("BUILDING_NOT_FOUND", "Building not found.");
            }
        }

        if (await _context.Classrooms.AnyAsync(c => c.RoomNumber == dto.RoomNumber && c.BuildingId == dto.BuildingId))
        {
            return Result<ClassroomResponseDto>.Conflict("ROOM_EXISTS", "A classroom with this room number already exists.");
        }

        var classroom = new Classroom
        {
            BuildingId = dto.BuildingId,
            RoomNumber = dto.RoomNumber,
            BuildingName = dto.BuildingId.HasValue ? await GetBuildingNameAsync(dto.BuildingId.Value) : null,
            Capacity = dto.Capacity,
            Floor = dto.Floor,
            RoomType = dto.RoomType,
            IsActive = true
        };

        await _unitOfWork.Classrooms.AddAsync(classroom);
        await _unitOfWork.SaveChangesAsync();

        return Result<ClassroomResponseDto>.Success(ToResponse(classroom));
    }

    public async Task<Result<ClassroomResponseDto>> UpdateAsync(Guid id, CreateClassroomRequestDto dto)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null)
        {
            return Result<ClassroomResponseDto>.NotFound("CLASSROOM_NOT_FOUND", "Classroom not found.");
        }

        if (dto.BuildingId.HasValue)
        {
            var buildingExists = await _context.Buildings.AnyAsync(b => b.Id == dto.BuildingId.Value);
            if (!buildingExists)
            {
                return Result<ClassroomResponseDto>.Validation("BUILDING_NOT_FOUND", "Building not found.");
            }
        }

        classroom.BuildingId = dto.BuildingId;
        classroom.BuildingName = dto.BuildingId.HasValue ? await GetBuildingNameAsync(dto.BuildingId.Value) : null;
        classroom.RoomNumber = dto.RoomNumber;
        classroom.Capacity = dto.Capacity;
        classroom.Floor = dto.Floor;
        classroom.RoomType = dto.RoomType;
        classroom.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Classrooms.UpdateAsync(classroom);
        await _unitOfWork.SaveChangesAsync();

        return Result<ClassroomResponseDto>.Success(ToResponse(classroom));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);
        if (classroom == null)
        {
            return Result<bool>.NotFound("CLASSROOM_NOT_FOUND", "Classroom not found.");
        }

        _context.Classrooms.Remove(classroom);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private async Task<string?> GetBuildingNameAsync(Guid buildingId)
    {
        return await _context.Buildings
            .Where(b => b.Id == buildingId)
            .Select(b => (string?)b.Name)
            .FirstOrDefaultAsync();
    }

    private static ClassroomResponseDto ToResponse(Classroom c)
    {
        return new ClassroomResponseDto
        {
            Id = c.Id,
            BranchId = c.BranchId,
            BuildingId = c.BuildingId,
            RoomNumber = c.RoomNumber,
            BuildingName = c.BuildingName,
            Capacity = c.Capacity,
            Floor = c.Floor,
            RoomType = c.RoomType,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        };
    }
}