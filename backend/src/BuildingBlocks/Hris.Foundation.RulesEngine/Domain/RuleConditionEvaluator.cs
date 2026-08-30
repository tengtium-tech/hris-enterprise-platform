using System.Globalization;

namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// The comparison mechanics behind <see cref="ComparisonOperator"/>, kept as a
/// standalone static helper rather than inlined in <see cref="RuleVersion"/> since it
/// is pure value comparison with no aggregate state or invariant of its own.
///
/// Ordering operators (<see cref="ComparisonOperator.GreaterThan"/> and siblings) try
/// a numeric comparison first, since most of this document's own condition examples
/// are numeric ("Years of Service," "Salary Grade"), then a date comparison, falling
/// back to ordinal string comparison only if neither parses -- this keeps
/// "5" &lt; "12" evaluating correctly rather than as a string comparison that would
/// place "12" before "5".
/// </summary>
internal static class RuleConditionEvaluator
{
    public static bool Matches(RuleCondition condition, string factValue)
    {
        return condition.Operator switch
        {
            ComparisonOperator.Equals => string.Equals(factValue, condition.ComparisonValue, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.NotEquals => !string.Equals(factValue, condition.ComparisonValue, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.Contains => factValue.Contains(condition.ComparisonValue, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.In => condition.ComparisonValue
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(candidate => string.Equals(candidate, factValue, StringComparison.OrdinalIgnoreCase)),
            ComparisonOperator.GreaterThan => Compare(factValue, condition.ComparisonValue) > 0,
            ComparisonOperator.GreaterThanOrEqual => Compare(factValue, condition.ComparisonValue) >= 0,
            ComparisonOperator.LessThan => Compare(factValue, condition.ComparisonValue) < 0,
            ComparisonOperator.LessThanOrEqual => Compare(factValue, condition.ComparisonValue) <= 0,
            _ => false,
        };
    }

    private static int Compare(string left, string right)
    {
        if (decimal.TryParse(left, NumberStyles.Number, CultureInfo.InvariantCulture, out var leftNumber)
            && decimal.TryParse(right, NumberStyles.Number, CultureInfo.InvariantCulture, out var rightNumber))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        if (DateOnly.TryParse(left, CultureInfo.InvariantCulture, DateTimeStyles.None, out var leftDate)
            && DateOnly.TryParse(right, CultureInfo.InvariantCulture, DateTimeStyles.None, out var rightDate))
        {
            return leftDate.CompareTo(rightDate);
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }
}
