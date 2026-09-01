using System.Linq.Expressions;

namespace University.Core.Specifications;

public abstract class BaseSpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public bool OrderByDescending { get; private set; }

    protected void AddCriteria(Expression<Func<T, bool>> criteria) => Criteria = criteria;

    protected void AddInclude(Expression<Func<T, object>> include) => Includes.Add(include);

    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy, bool descending = false)
    {
        OrderBy = orderBy;
        OrderByDescending = descending;
    }
}
