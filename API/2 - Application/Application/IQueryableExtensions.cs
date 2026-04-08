using System.Linq.Expressions;
using Application.Dto.RequestPatterns;

namespace Application;

public static class IQueryableExtensions
{
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, RequestAllDto request)
    {
        return query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);
    }

    public static IQueryable<T> ApplyOrdering<T>(this IQueryable<T> query, RequestAllDto request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderBy))
            return query;

        var property = typeof(T).GetProperty(request.OrderBy, 
            System.Reflection.BindingFlags.IgnoreCase | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance);

        if (property == null)
            return query;

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = request.OrderDesc ? "OrderByDescending" : "OrderBy";
        var resultExpression = Expression.Call(
            typeof(Queryable),
            methodName,
            new Type[] { typeof(T), property.PropertyType },
            query.Expression,
            Expression.Quote(lambda));

        return query.Provider.CreateQuery<T>(resultExpression);
    }

    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, RequestAllDto request, 
        Expression<Func<T, bool>>? searchExpression = null)
    {
        if (string.IsNullOrWhiteSpace(request.Search) || searchExpression == null)
            return query;

        return query.Where(searchExpression);
    }
}
