using System.Data;
using University.Shared.Interfaces.Repositories;

namespace University.Core.Interfaces.Repositories;

public interface IUnitOfWork
{
    IGenericRepository<University.Core.Entities.Branch> Branches { get; }
    IGenericRepository<University.Core.Entities.AdministrativeDepartment> AdministrativeDepartments { get; }
    IGenericRepository<University.Core.Entities.Role> Roles { get; }
    IGenericRepository<University.Core.Entities.Permission> Permissions { get; }
    IGenericRepository<University.Core.Entities.RolePermission> RolePermissions { get; }
    IGenericRepository<University.Core.Entities.Faculty> Faculties { get; }
    IGenericRepository<University.Core.Entities.AcademicDepartment> AcademicDepartments { get; }
    IGenericRepository<University.Core.Entities.Major> Majors { get; }
    IGenericRepository<University.Core.Entities.Course> Courses { get; }
    IGenericRepository<University.Core.Entities.CoursePrerequisite> CoursePrerequisites { get; }
    IGenericRepository<University.Core.Entities.Building> Buildings { get; }
    IGenericRepository<University.Core.Entities.Semester> Semesters { get; }
    IGenericRepository<University.Core.Entities.Classroom> Classrooms { get; }
    IGenericRepository<University.Core.Entities.User> Users { get; }
    IGenericRepository<University.Core.Entities.Instructor> Instructors { get; }
    IGenericRepository<University.Core.Entities.Student> Students { get; }
    IGenericRepository<University.Core.Entities.CourseSection> CourseSections { get; }
    IGenericRepository<University.Core.Entities.StudyPlan> StudyPlans { get; }
    IGenericRepository<University.Core.Entities.CourseEnrollment> CourseEnrollments { get; }
    IGenericRepository<University.Core.Entities.AttendanceRecord> AttendanceRecords { get; }
    IGenericRepository<University.Core.Entities.Grade> Grades { get; }
    IGenericRepository<University.Core.Entities.AcademicRecord> AcademicRecords { get; }
    IGenericRepository<University.Core.Entities.TuitionFee> TuitionFees { get; }
    IGenericRepository<University.Core.Entities.FinancialRecord> FinancialRecords { get; }
    IGenericRepository<University.Core.Entities.Payment> Payments { get; }
    IGenericRepository<University.Core.Entities.Scholarship> Scholarships { get; }
    IGenericRepository<University.Core.Entities.FinancialRefund> FinancialRefunds { get; }
    IGenericRepository<University.Core.Entities.Guardian> Guardians { get; }
    IAuditLogRepository AuditLogs { get; }
    IGenericRepository<University.Core.Entities.SystemNotification> SystemNotifications { get; }
    IGenericRepository<University.Core.Entities.GraduationRequest> GraduationRequests { get; }

    IUserRepository UserRepository { get; }
    IStudentRepository StudentRepository { get; }
    ICourseRepository CourseRepository { get; }
    IEnrollmentRepository EnrollmentRepository { get; }
    IGradeRepository GradeRepository { get; }
    IFinancialRepository FinancialRepository { get; }
    ISemesterRepository SemesterRepository { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Serializable);
    Task CommitAsync();
    Task RollbackAsync();
    Task ExecuteInTransactionAsync(Func<Task> operation);
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
