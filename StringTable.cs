namespace MiniCSharp;

/// <summary>
/// String table for the mini C# compiler
/// </summary>
public class StringTable
{
    private readonly Dictionary<string, StringT> _stringTable = new();

    /// <summary>
    /// Keep track of the number of strings in the table
    /// </summary>
    public int Count => _stringTable.Count;

    /// <summary>
    /// Insert a new string into the table
    /// </summary>
    /// <param name="str">the value of the new string</param>
    /// <returns>pointer to the new element</returns>
    public StringT Insert(string str)
    {
        var newString = str;

        // Check for duplicates
        if (_stringTable.TryGetValue(str, out var existingString))
            return existingString;

        // MASM 6.14 and 6.15 reject empty strings; change them to a space
        // TODO: are we still targeting an old MASM install? unlikely.
        if (str == "\"\"")
            newString = "\" \"";

        var el = new StringT
        {
            Name = $"_S{Count}",
            String = newString
        };
        
        _stringTable.Add(str, el);
        return el;
    }

    /// <summary>
    /// Search the string table for a specific string.
    /// </summary>
    /// <param name="name">string name to find</param>
    /// <returns>a pointer to the desired element, or <c>null</c></returns>
    public StringT? Lookup(string name) => _stringTable.GetValueOrDefault(name);

    /// <summary>
    /// Print out the entire string table (for debugging purposes)
    /// </summary>
    public void PrintTable()
    {
        Console.WriteLine("Name  Value\n----  -----");
        foreach (var entry in _stringTable)
            Console.WriteLine("{0}   {1}", entry.Value.Name, entry.Value.String);
    }
}

/// <summary>
/// String table object for the mini C# compiler
/// </summary>
public class StringT
{
    public string? Name;
    public string? String;
}