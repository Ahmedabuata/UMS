using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Buildings;
using University.Shared.DTOs.Classrooms;

namespace University.Infrastructure.Services;

public class BuildingService : IBuildingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public BuildingService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<BuildingResponseDto>> GetByIdAsync(Guid id)
    {
        var building = await _context.Buildings
            .Include(b => b.Branch)
            .Include(b => b.Classrooms)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (building == null)
        {
            return Result<BuildingResponseDto>.NotFound("BUILDING_NOT_FOUND", "Building not found.");
        }

        return Result<BuildingResponseDto>.Success(ToResponse(building));
    }

    public async Task<Result<IEnumerable<BuildingResponseDto>>> GetAllAsync()
    {
        var buildings = await _context.Buildings
            .Include(b => b.Branch)
            .Include(b => b.Classrooms)
            .OrderBy(b => b.Name)
            .ToListAsync();
        return Result<IEnumerable<BuildingResponseDto>>.Success(
            buildings.Select(ToResponse).ToList());
    }

    public async Task<Result<IEnumerable<ClassroomResponseDto>>> GetClassroomsAsync(Guid buildingId)
    {
        var exists = await _context.Buildings.AnyAsync(b => b.Id == buildingId);
        if (!exists)
        {
            return Result<IEnumerable<ClassroomResponseDto>>.NotFound("BUILDING_NOT_FOUND", "Building not found.");
        }

        var classrooms = await _context.Classrooms
            .Where(c => c.BuildingId == buildingId && c.IsActive)
            .OrderBy(c => c.Floor)
            .ThenBy(c => c.RoomNumber)
            .ToListAsync();
        return Result<IEnumerable<ClassroomResponseDto>>.Success(
            classrooms.Select(ToClassroomResponse).ToList());
    }

    public async Task<Result<BuildingResponseDto>> CreateAsync(CreateBuildingRequestDto dto)
    {
        var code = dto.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _context.Buildings.AnyAsync(b => b.Code == code))
        {
            return Result<BuildingResponseDto>.Conflict("BUILDING_CODE_EXISTS", "Building code already exists.");
        }

        if (dto.BranchId is null || !await _context.Branches.AnyAsync(br => br.Id == dto.BranchId.Value))
        {
            return Result<BuildingResponseDto>.Validation("INVALID_BRANCH", "Branch must be valid.");
        }

        var building = new Building
        {
            Name = dto.Name,
            Code = code,
            BranchId = dto.BranchId.Value,
            Address = dto.Address,
            Floors = dto.Floors,
            IsActive = true
        };

        await _unitOfWork.Buildings.AddAsync(building);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(building.Id);
    }

    public async Task<Result<BuildingResponseDto>> UpdateAsync(Guid id, CreateBuildingRequestDto dto)
    {
        var building = await _context.Buildings.FindAsync(id);
        if (building == null)
        {
            return Result<BuildingResponseDto>.NotFound("BUILDING_NOT_FOUND", "Building not found.");
        }

        var code = dto.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (await _context.Buildings.AnyAsync(b => b.Code == code && b.Id != id))
        {
            return Result<BuildingResponseDto>.Conflict("BUILDING_CODE_EXISTS", "Building code already exists.");
        }

        if (dto.BranchId is null || !await _context.Branches.AnyAsync(br => br.Id == dto.BranchId.Value))
        {
            return Result<BuildingResponseDto>.Validation("INVALID_BRANCH", "Branch must be valid.");
        }

        building.Name = dto.Name;
        building.Code = code;
        building.BranchId = dto.BranchId.Value;
        building.Address = dto.Address;
        building.Floors = dto.Floors;
        building.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Buildings.UpdateAsync(building);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        var building = await _context.Buildings.FindAsync(id);
        if (building == null)
        {
            return Result<bool>.NotFound("BUILDING_NOT_FOUND", "Building not found.");
        }

        _context.Buildings.Remove(building);
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static BuildingResponseDto ToResponse(Building building)
    {
        return new BuildingResponseDto
        {
            Id = building.Id,
            Name = building.Name,
            Code = building.Code,
            BranchId = building.BranchId,
            BranchName = building.Branch?.BranchName,
            BranchCode = building.Branch?.BranchCode,
            Address = building.Address,
            Floors = building.Floors,
            ClassroomCount = building.Classrooms?.Count(c => c.IsActive) ?? 0,
            IsActive = building.IsActive,
            CreatedAt = building.CreatedAt,
            Classrooms = building.Classrooms?
                .Where(c => c.IsActive)
                .Select(ToClassroomResponse)
                .ToList() ?? new()
        };
    }

    private static ClassroomResponseDto ToClassroomResponse(Classroom c)
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
