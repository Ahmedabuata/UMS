namespace University.Core.Interfaces.Services;

public interface IIdentifierGeneratorService
{
    Task<string> GenerateEmployeeNumberAsync(Guid administrativeDepartmentId);
    Task<string> GenerateStudentNumberAsync(Guid facultyId, Guid academicDepartmentId);
}