using System.Globalization;

namespace MiniCSharp;

internal class TypeHelper
{
    /* IsOperator()
     *
     * Return true/false based on whether the passed character
     * is [+-/*%<>=!]
     */
    public static bool IsOperator(char input)
    {
        try
        {
            switch (input)
            {
                case '+':
                case '-':
                case '/':
                case '*':
                case '%':
                case '<':
                case '>':
                case '=':
                case '!':
                case '&':
                case '|':
                    return true;
                default:
                    return false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception caught: {0}", e.Message);
            return false;
        }
    }

    /* IsNumeric()
     *
     * Return true/false based on whether the object is a number
     * Source: http://dotnet.org.za/deonvs/archive/2004/07/06/2579.aspx
     */
    public static bool IsNumeric(object expression) =>
        double.TryParse(Convert.ToString(expression),
            NumberStyles.Any,
            NumberFormatInfo.InvariantInfo,
            out _);
}