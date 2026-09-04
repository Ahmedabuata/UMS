using System.Linq.Expressions;
using University.Core.Entities;

namespace University.Core.Specifications;

public class StudentSpecifications : BaseSpecification<Student>
{
    public static StudentSpecifications WithActiveStudents() =>
        New(s => s.IsActive && s.Status == University.Shared.Enums.StudentStatus.ST_ACTIVE);

    public static StudentSpecifications ByMajor(Guid majorId) =>
        New(s => s.MajorId == majorId);

    public static StudentSpecifications WithGpaAbove(decimal gpa) =>
        New(s => s.Gpa >= gpa);

    private static StudentSpecifications New(Expression<Func<Student, bool>> criteria)
    {
        var spec = new StudentSpecifications();
        spec.AddCriteria(criteria);
        spec.AddInclude(s => s.Major!);
        return spec;
    }
}
