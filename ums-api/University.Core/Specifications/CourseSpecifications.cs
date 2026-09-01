using System.Linq.Expressions;
using University.Core.Entities;

namespace University.Core.Specifications;

public class CourseSpecifications : BaseSpecification<Course>
{
    public static CourseSpecifications WithActiveCourses() =>
        New(c => c.IsActive);

    private static CourseSpecifications New(Expression<Func<Course, bool>> criteria)
    {
        var spec = new CourseSpecifications();
        spec.AddCriteria(criteria);
        spec.AddInclude(c => c.Prerequisites);
        return spec;
    }
}
