using Microsoft.EntityFrameworkCore;
using University.Application.Mapping;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;
using University.Infrastructure.Security;
using University.Shared.Common;
using University.Shared.DTOs.Students;
using University.Shared.Enums;

namespace University.Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordPolicyService _passwordPolicy;
    private readonly INotificationService _notification;
    private readonly IIdentifierGeneratorService _identifierGenerator;

    public StudentService(
        IUnitOfWork unitOfWork,
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IPasswordPolicyService passwordPolicy,
        INotificationService notification,
        IIdentifierGeneratorService identifierGenerator)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _notification = notification;
        _identifierGenerator = identifierGenerator;
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
        // Generate the identifier server-side (FACULTY-DEPT-YYYYMM-XXXXX) when not provided.
        string studentNumber;
        if (string.IsNullOrWhiteSpace(dto.StudentNumber))
        {
            if (!dto.FacultyId.HasValue || !dto.AcademicDepartmentId.HasValue)
            {
                return Result<StudentResponseDto>.Validation("FACULTY_DEPT_REQUIRED", "Faculty and academic department are required to generate a student number.");
            }
            try
            {
                studentNumber = await _identifierGenerator.GenerateStudentNumberAsync(dto.FacultyId.Value, dto.AcademicDepartmentId.Value);
            }
            catch (InvalidOperationException ex)
            {
                return Result<StudentResponseDto>.Validation("CODE_MISSING", ex.Message);
            }
        }
        else
        {
            studentNumber = dto.StudentNumber.Trim();
        }

        if (await _context.Students.AnyAsync(s => s.StudentNumber == studentNumber))
        {
            return Result<StudentResponseDto>.Conflict("STUDENT_NUMBER_EXISTS", "Student number already exists.");
        }

        // Student admission/academic record is created with its OWN UUID (NOT shared with the user).
        // Optionally link to an EXISTING (employee-backed) user via Student.UserId; otherwise no user.
        // No user account is auto-created here (all users must be employee-backed -> users.id == employees.id).
        Guid studentId = Guid.NewGuid();
        Guid? linkedUserId = null;
        if (dto.UserId.HasValue && dto.UserId.Value != Guid.Empty &&
            await _context.Users.AnyAsync(u => u.Id == dto.UserId.Value))
        {
            linkedUserId = dto.UserId.Value;
        }

        var student = new Student
        {
            Id = studentId,
            UserId = linkedUserId,
            MajorId = dto.MajorId.HasValue && dto.MajorId.Value != Guid.Empty ? dto.MajorId : null,
            StudentNumber = studentNumber,
            EnrollmentDate = dto.EnrollmentDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Status = StudentStatus.ST_ACTIVE,
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
            .Include(s => s.Major)
            .Where(s => s.IsActive)
            .ToListAsync();
        return Result<IEnumerable<StudentResponseDto>>.Success(students.Select(StudentMapper.ToResponse).ToList());
    }

    public async Task<Result<string>> PreviewStudentNumberAsync(Guid facultyId, Guid academicDepartmentId)
    {
        try
        {
            var number = await _identifierGenerator.GenerateStudentNumberAsync(facultyId, academicDepartmentId);
            return Result<string>.Success(number);
        }
        catch (InvalidOperationException ex)
        {
            return Result<string>.Validation("CODE_MISSING", ex.Message);
        }
    }
}
