using System;
using System.Collections.Generic;

public enum ComparisonOperator
{
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Equal,
    NotEqual,
    In,
    Like
}

public enum LogicalOperator
{
    And,
    Or
}

public class GenericFilter<T>
{
    // A method that accepts LHS, RHS, comparison type and logical operators using enums and returns a string representation
    public static string BuildExpression(
        string lhs, object rhs, ComparisonOperator comparisonOperator)
    {
        // Generating a string expression based on comparison operator enum
        string comparison = comparisonOperator switch
        {
            ComparisonOperator.GreaterThan => ">",
            ComparisonOperator.LessThan => "<",
            ComparisonOperator.GreaterThanOrEqual => ">=",
            ComparisonOperator.LessThanOrEqual => "<=",
            ComparisonOperator.Equal => "==",
            ComparisonOperator.NotEqual => "!=",
            ComparisonOperator.In => "IN",
            ComparisonOperator.Like => "LIKE",
            _ => throw new ArgumentException($"Unsupported comparison operator: {comparisonOperator}")
        };

        // Returning string representation of the expression
        return $"{lhs} {comparison} {FormatRhs(rhs)}";
    }

    // Method to format the RHS for proper representation (e.g., list for "IN" operator)
    private static string FormatRhs(object rhs)
    {
        return rhs switch
        {
            IEnumerable<object> enumerable => $"({string.Join(", ", enumerable)})",
            string str => $"\"{str}\"",
            _ => rhs.ToString()
        };
    }

    // Combine multiple expressions with logical operators (AND/OR) using enums and return a string
    public static string CombineExpressions(
        string expr1, string expr2, LogicalOperator logicalOperator)
    {
        string logical = logicalOperator switch
        {
            LogicalOperator.And => "AND",
            LogicalOperator.Or => "OR",
            _ => throw new ArgumentException($"Unsupported logical operator: {logicalOperator}")
        };

        // Combine expressions as a string
        return $"({expr1} {logical} {expr2})";
    }
}