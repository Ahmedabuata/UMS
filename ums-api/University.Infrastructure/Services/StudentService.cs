using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Shared.Common;
using University.Shared.DTOs.Students;

namespace University.Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;

    public StudentService(IUnitOfWork unitOfWork, ApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<StudentResponseDto>> GetStudentByIdAsync(Guid id)
    {
        var student = await _unitOfWork.StudentRepository.GetWithMajorAsync(id);
        if (student == null)
        {
            return Result<StudentResponseDto>.NotFound("STUDENT_NOT_FOUND", "Student not found.");
        }

        return Result<StudentResponseDto>.Success(StudentMapper.ToResponse(student));
    }

    public async Task<Result<StudentResponseDto>> CreateStudentAsync(CreateStudentRequestDto dto)
    {
        if (await _context.Students.AnyAsync(s => s.StudentNumber == dto.StudentNumber))
        {
            return Result<StudentResponseDto>.Conflict("STUDENT_NUMBER_EXISTS", "Student number already exists.");
        }

        var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
        if (!userExists)
        {
            return Result<StudentResponseDto>.Validation("USER_NOT_FOUND", "User not found.");
        }

        var student = new University.Core.Entities.Student
        {
            UserId = dto.UserId,
            MajorId = dto.MajorId,
            StudentNumber = dto.StudentNumber,
            EnrollmentDate = dto.EnrollmentDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Status = University.Shared.Enums.StudentStatus.ST_ACTIVE,
            Gpa = 0,
            IsActive = true
        };

        await _unitOfWork.Students.AddAsync(student);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.StudentRepository.GetWithMajorAsync(student.Id);
        return Result<StudentResponseDto>.Success(StudentMapper.ToResponse(saved!));
    }

    public async Task<Result<StudentResponseDto>> UpdateStudentAsync(Guid id, UpdateStudentRequestDto dto)
    {
        var student = await _unitOfWork.StudentRepository.GetWithMajorAsync(id);
        if (student == null)
        {
            return Result<StudentResponseDto>.NotFound("STUDENT_NOT_FOUND", "Student not found.");
        }

        if (dto.MajorId.HasValue) student.MajorId = dto.MajorId;
        if (dto.Status.HasValue) student.Status = dto.Status.Value;
        if (dto.Gpa.HasValue) student.Gpa = dto.Gpa.Value;
        if (dto.CompletedCredits.HasValue) student.CompletedCredits = dto.CompletedCredits.Value;
        student.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Students.UpdateAsync(student);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.StudentRepository.GetWithMajorAsync(id);
        return Result<StudentResponseDto>.Success(StudentMapper.ToResponse(saved!));
    }

    public async Task<Result<decimal>> GetStudentGPAAsync(Guid studentId)
    {
        var student = await _context.Students.FindAsync(studentId);
        if (student == null)
        {
            return Result<decimal>.NotFound("STUDENT_NOT_FOUND", "Student not found.");
        }

        return Result<decimal>.Success(student.Gpa);
    }

    public async Task<Result<bool>> UpdateGPAAsync(Guid studentId, decimal gpa)
    {
        var student = await _context.Students.FindAsync(studentId);
        if (student == null)
        {
            return Result<bool>.NotFound("STUDENT_NOT_FOUND", "Student not found.");
        }

        student.Gpa = gpa;
        student.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<StudentResponseDto>>> GetAllAsync()
    {
        var students = await _context.Students
            .Include(s => s.User)
            .Include(s => s.Major)
            .Where(s => s.IsActive)
            .ToListAsync();
        return Result<IEnumerable<StudentResponseDto>>.Success(students.Select(StudentMapper.ToResponse).ToList());
    }
}
