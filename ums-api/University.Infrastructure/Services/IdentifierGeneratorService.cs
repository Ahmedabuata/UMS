using Microsoft.EntityFrameworkCore;
using University.Core.Entities;
using University.Core.Interfaces.Services;
using University.Infrastructure.Data;

namespace University.Infrastructure.Services;

public class IdentifierGeneratorService : IIdentifierGeneratorService
{
    private readonly ApplicationDbContext _context;

    public IdentifierGeneratorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateEmployeeNumberAsync(Guid administrativeDepartmentId)
    {
        var dept = await _context.AdministrativeDepartments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == administrativeDepartmentId && d.IsActive);
        if (dept == null || string.IsNullOrWhiteSpace(dept.DepartmentCode))
        {
            throw new InvalidOperationException("A valid active administrative department with a department code is required to generate an employee number.");
        }

        var prefix = $"{dept.DepartmentCode.Trim()}-{DateTime.Now:yyyyMM}-";
        var last = await NextEmployeeSequenceAsync(prefix);
        return $"{prefix}{last.ToString().PadLeft(5, '0')}";
    }

    public async Task<string> GenerateStudentNumberAsync(Guid facultyId, Guid academicDepartmentId)
    {
        var faculty = await _context.Faculties
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == facultyId && f.IsActive);
        if (faculty == null || string.IsNullOrWhiteSpace(faculty.FacultyCode))
        {
            throw new InvalidOperationException("A valid active faculty with a faculty code is required to generate a student number.");
        }

        var dept = await _context.AcademicDepartments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == academicDepartmentId && d.FacultyId == facultyId && d.IsActive);
        if (dept == null || string.IsNullOrWhiteSpace(dept.DepartmentCode))
        {
            throw new InvalidOperationException("A valid active academic department with a department code is required to generate a student number.");
        }

        var prefix = $"{faculty.FacultyCode.Trim()}-{dept.DepartmentCode.Trim()}-{DateTime.Now:yyyyMM}-";
        var seq = await NextStudentSequenceAsync(prefix);
        return $"{prefix}{seq.ToString().PadLeft(5, '0')}";
    }

    private async Task<long> NextEmployeeSequenceAsync(string prefix)
    {
        var last = await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeNumber.StartsWith(prefix))
            .OrderByDescending(e => e.EmployeeNumber)
            .Select(e => e.EmployeeNumber)
            .FirstOrDefaultAsync();
        return ExtractAndIncrement(last);
    }

    private async Task<long> NextStudentSequenceAsync(string prefix)
    {
        var last = await _context.Students
            .AsNoTracking()
            .Where(s => s.StudentNumber.StartsWith(prefix))
            .OrderByDescending(s => s.StudentNumber)
            .Select(s => s.StudentNumber)
            .FirstOrDefaultAsync();
        return ExtractAndIncrement(last);
    }

    private static long ExtractAndIncrement(string? last)
    {
        if (string.IsNullOrWhiteSpace(last) || last.Length < 5)
        {
            return 1;
        }

        var tail = last.Substring(last.Length - 5);
        return long.TryParse(tail, out var seq) ? seq + 1 : 1;
    }
}