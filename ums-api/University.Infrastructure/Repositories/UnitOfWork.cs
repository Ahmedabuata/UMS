using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using University.Core.Entities;
using University.Core.Interfaces.Repositories;
using University.Infrastructure.Data;
using University.Shared.Interfaces.Repositories;

namespace University.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;

        Branches = new GenericRepository<Branch>(context);
        AdministrativeDepartments = new GenericRepository<AdministrativeDepartment>(context);
        Roles = new GenericRepository<Role>(context);
        Permissions = new GenericRepository<Permission>(context);
        RolePermissions = new GenericRepository<RolePermission>(context);
        Faculties = new GenericRepository<Faculty>(context);
        AcademicDepartments = new GenericRepository<AcademicDepartment>(context);
        Majors = new GenericRepository<Major>(context);
        Courses = new GenericRepository<Course>(context);
        CoursePrerequisites = new GenericRepository<CoursePrerequisite>(context);
        Semesters = new GenericRepository<Semester>(context);
        Classrooms = new GenericRepository<Classroom>(context);
        Users = new UserRepository(context);
        Instructors = new GenericRepository<Instructor>(context);
        Students = new StudentRepository(context);
        CourseSections = new GenericRepository<CourseSection>(context);
        StudyPlans = new GenericRepository<StudyPlan>(context);
        CourseEnrollments = new EnrollmentRepository(context);
        AttendanceRecords = new GenericRepository<AttendanceRecord>(context);
        Grades = new GradeRepository(context);
        AcademicRecords = new GenericRepository<AcademicRecord>(context);
        TuitionFees = new GenericRepository<TuitionFee>(context);
        FinancialRecords = new FinancialRepository(context);
        Payments = new GenericRepository<Payment>(context);
        Scholarships = new GenericRepository<Scholarship>(context);
        FinancialRefunds = new GenericRepository<FinancialRefund>(context);
        Guardians = new GenericRepository<Guardian>(context);
        AuditLogs = new AuditLogRepository(context);
        SystemNotifications = new GenericRepository<SystemNotification>(context);
        GraduationRequests = new GenericRepository<GraduationRequest>(context);

        UserRepository = new UserRepository(context);
        StudentRepository = new StudentRepository(context);
        CourseRepository = new CourseRepository(context);
        EnrollmentRepository = new EnrollmentRepository(context);
        GradeRepository = new GradeRepository(context);
        FinancialRepository = new FinancialRepository(context);
        SemesterRepository = new SemesterRepository(context);
    }

    public IGenericRepository<Branch> Branches { get; }
    public IGenericRepository<AdministrativeDepartment> AdministrativeDepartments { get; }
    public IGenericRepository<Role> Roles { get; }
    public IGenericRepository<Permission> Permissions { get; }
    public IGenericRepository<RolePermission> RolePermissions { get; }
    public IGenericRepository<Faculty> Faculties { get; }
    public IGenericRepository<AcademicDepartment> AcademicDepartments { get; }
    public IGenericRepository<Major> Majors { get; }
    public IGenericRepository<Course> Courses { get; }
    public IGenericRepository<CoursePrerequisite> CoursePrerequisites { get; }
    public IGenericRepository<Semester> Semesters { get; }
    public IGenericRepository<Classroom> Classrooms { get; }
    public IGenericRepository<User> Users { get; }
    public IGenericRepository<Instructor> Instructors { get; }
    public IGenericRepository<Student> Students { get; }
    public IGenericRepository<CourseSection> CourseSections { get; }
    public IGenericRepository<StudyPlan> StudyPlans { get; }
    public IGenericRepository<CourseEnrollment> CourseEnrollments { get; }
    public IGenericRepository<AttendanceRecord> AttendanceRecords { get; }
    public IGenericRepository<Grade> Grades { get; }
    public IGenericRepository<AcademicRecord> AcademicRecords { get; }
    public IGenericRepository<TuitionFee> TuitionFees { get; }
    public IGenericRepository<FinancialRecord> FinancialRecords { get; }
    public IGenericRepository<Payment> Payments { get; }
    public IGenericRepository<Scholarship> Scholarships { get; }
    public IGenericRepository<FinancialRefund> FinancialRefunds { get; }
    public IGenericRepository<Guardian> Guardians { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IGenericRepository<SystemNotification> SystemNotifications { get; }
    public IGenericRepository<GraduationRequest> GraduationRequests { get; }

    public IUserRepository UserRepository { get; }
    public IStudentRepository StudentRepository { get; }
    public ICourseRepository CourseRepository { get; }
    public IEnrollmentRepository EnrollmentRepository { get; }
    public IGradeRepository GradeRepository { get; }
    public IFinancialRepository FinancialRepository { get; }
    public ISemesterRepository SemesterRepository { get; }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.Serializable)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(CancellationToken.None);
        await _context.Database.ExecuteSqlRawAsync(
            $"SET TRANSACTION ISOLATION LEVEL {MapIsolationLevel(isolationLevel)}");
    }

    private static string MapIsolationLevel(IsolationLevel level) => level switch
    {
        IsolationLevel.ReadCommitted => "READ COMMITTED",
        IsolationLevel.ReadUncommitted => "READ UNCOMMITTED",
        IsolationLevel.RepeatableRead => "REPEATABLE READ",
        IsolationLevel.Serializable => "SERIALIZABLE",
        IsolationLevel.Snapshot => "SNAPSHOT",
        _ => "SERIALIZABLE"
    };

    public async Task CommitAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        if (_currentTransaction != null)
        {
            return await operation();
        }

        await BeginTransactionAsync();
        try
        {
            var result = await operation();
            await CommitAsync();
            return result;
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        if (_currentTransaction != null)
        {
            await operation();
            return;
        }

        await BeginTransactionAsync();
        try
        {
            await operation();
            await CommitAsync();
        }
        catch
        {
            await RollbackAsync();
            throw;
        }
    }
}
