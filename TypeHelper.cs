using System.Globalization;

namespace MiniCSharp;

internal static class TypeHelper
{
    /// <summary>
    /// Determines if <paramref name="input"/> is an operator/the start of an operator
    /// </summary>
    /// <param name="input">The character to check</param>
    /// <returns><c>true</c> if <c>[+-/*%&lt;&gt;=!]</c>, else <c>false</c></returns>
    public static bool IsOperator(char input) =>
        input switch
        {
            '+' or '-' or '/' or '*' or '%' or '<' or '>' or '=' or '!' or '&' or '|' => true,
            _ => false
        };

    /// <summary>
    /// Determines if <paramref name="expression"/> is numeric
    /// </summary>
    /// <param name="expression">The object to check</param>
    /// <returns><c>true</c> if <paramref name="expression"/> is numeric, else <c>false</c></returns>
    /// <seealso cref="http://dotnet.org.za/deonvs/archive/2004/07/06/2579.aspx"/>
    public static bool IsNumeric(object expression) =>
        double.TryParse(Convert.ToString(expression),
            NumberStyles.Any,
            NumberFormatInfo.InvariantInfo,
            out _);
}