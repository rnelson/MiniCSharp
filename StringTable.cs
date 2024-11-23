namespace MiniCSharp;

/// <summary>
/// String table for the mini C# compiler
/// </summary>
public class StringTable
{
    private const int MaximumStrings = 99999;
    private readonly StringT?[] vertArray;

    /// <summary>
    /// Keep track of the number of strings in the table
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Constructor for the string table class
    /// </summary>
    public StringTable()
    {
        vertArray = new StringT?[MaximumStrings];
        InitTable();
    }

    /// <summary>
    /// Insert a new string into the table
    /// </summary>
    /// <param name="str">the value of the new string</param>
    /// <returns>pointer to the new element</returns>
    public StringT Insert(string str)
    {
        var newString = str;

        // Error out if we have no more memory
        if (Count >= MaximumStrings)
        {
            Console.Error.WriteLine("error: no more memory available for additional strings, terminating");
            Environment.Exit(-3);
        }

        // Check for duplicates
        for (var i = 0; i < Count; i++)
            if (vertArray[i]?.String == str)
                return vertArray[i]!;

        // MASM 6.14 and 6.15 reject empty strings; change them to a space
        // TODO: are we still targeting an old MASM install? unlikely.
        if (str == "\"\"")
            newString = "\" \"";

        var el = new StringT
        {
            Name = $"_S{Count}",
            String = newString
        };
        
        vertArray[Count] = el;
        Count++;

        return el;
    }

    /// <summary>
    /// Search the string table for a specific string.
    /// </summary>
    /// <param name="name">string name to find</param>
    /// <returns>a pointer to the desired element, or <c>null</c></returns>
    public StringT? Lookup(string name)
    {
        for (var arrayLoc = 0; arrayLoc < Count; arrayLoc++)
        {
            var element = vertArray[arrayLoc];
            if (element?.Name == name)
                return element;
        }

        return null;
    }

    /// <summary>
    /// Print out the entire string table (for debugging purposes)
    /// </summary>
    public void PrintTable()
    {
        if (vertArray.Length != Count)
            throw new InternalCompilerException("error: table size mismatch",
                debugInformation: "M:StringTable.PrintTable");
        
        Console.WriteLine("Name  Value\n----  -----");
        for (var arrayLoc = 0; arrayLoc < Count; arrayLoc++)
            Console.WriteLine("{0}   {1}", vertArray[arrayLoc]!.Name, vertArray[arrayLoc]!.String);
    }

    /// <summary>
    /// Initialize the hash table
    /// </summary>
    private void InitTable()
    {
        for (var i = 0; i < MaximumStrings; i++)
            vertArray[i] = null;
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