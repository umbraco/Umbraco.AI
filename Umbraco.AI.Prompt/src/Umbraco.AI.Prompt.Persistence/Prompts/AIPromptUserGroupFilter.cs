using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore;

namespace Umbraco.AI.Prompt.Persistence.Prompts;

/// <summary>
/// Limits a prompt query to the prompts a set of user groups may use, in the same SQL query.
/// </summary>
internal static class AIPromptUserGroupFilter
{
    private static readonly MethodInfo LikeMethod = typeof(DbFunctionsExtensions).GetMethod(
        nameof(DbFunctionsExtensions.Like),
        [typeof(DbFunctions), typeof(string), typeof(string)])!;

    /// <summary>
    /// Keeps prompts with no group limit (null, empty or `[]` column), or limited to at least one of the given groups.
    /// Null <paramref name="userGroupKeys"/> applies no limit.
    /// </summary>
    /// <remarks>
    /// The column holds a JSON array of lowercase, dashed keys (see <see cref="AIPromptEntityFactory"/>), so each
    /// group becomes one <c>LIKE '%"key"%'</c>, ORed. The quotes keep a key from matching part of another value.
    /// A user in no groups only matches the unlimited prompts.
    /// </remarks>
    public static IQueryable<AIPromptEntity> WhereUsableBy(this IQueryable<AIPromptEntity> query, IReadOnlyCollection<Guid>? userGroupKeys)
    {
        if (userGroupKeys is null)
        {
            return query;
        }

        var prompt = Expression.Parameter(typeof(AIPromptEntity), "e");
        var column = Expression.Property(prompt, nameof(AIPromptEntity.AllowedUserGroupIds));

        Expression condition = Expression.OrElse(
            Expression.Equal(column, Expression.Constant(null, typeof(string))),
            Expression.OrElse(
                Expression.Equal(column, Expression.Constant(string.Empty)),
                Expression.Equal(column, Expression.Constant("[]"))));

        foreach (var key in userGroupKeys.Distinct())
        {
            var like = Expression.Call(
                LikeMethod,
                Expression.Constant(EF.Functions),
                column,
                PatternParameter($"%\"{key:D}\"%"));
            condition = Expression.OrElse(condition, like);
        }

        return query.Where(Expression.Lambda<Func<AIPromptEntity, bool>>(condition, prompt));
    }

    // Reading the pattern from a captured holder makes EF send it as a SQL parameter, so the
    // compiled query depends on the number of groups, not on which groups.
    private static MemberExpression PatternParameter(string pattern)
        => Expression.Property(Expression.Constant(new Tuple<string>(pattern)), nameof(Tuple<string>.Item1));
}
