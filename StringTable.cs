namespace MiniCSharp;

/// String table for the mini C# compiler
/// 
/// \author Ross Nelson
public class StringTable
{
    /// the maximum number of strings
    private const int MaximumStrings = 99999;

    /// keep track of the number of strings in the table
    public int NumberOfStrings;

    /// our array
    private readonly StringT?[] vertArray;

    /// Constructor for the string table class
    public StringTable()
    {
        /* allocate space for the hash values */
        vertArray = new StringT?[MaximumStrings];
        InitTable();
    }

    /// Initialize the hash table
    public void InitTable()
    {
        for (var i = 0; i < MaximumStrings; i++)
            vertArray[i] = null;
    }

    /// Insert a new string into the table
    /// \param str the value of the new string
    /// \return pointer to the new element
    public StringT Insert(string str)
    {
        var insstr = str;

        /* error out if we have no more memory */
        if (NumberOfStrings >= MaximumStrings)
        {
            Console.WriteLine("error: no more memory available for additional strings, terminating");
            Environment.Exit(-3);
        }

        /* check for duplicates */
        for (var i = 0; i < NumberOfStrings; i++)
            if (vertArray[i].String == str)
                return vertArray[i];

        /* MASM 6.14 and 6.15 reject empty strings; change them to a space */
        if (str == "\"\"")
            insstr = "\" \"";

        /* set the new string and add it */
        var el = new StringT
        {
            Name = $"_S{NumberOfStrings}",
            String = insstr
        };
        vertArray[NumberOfStrings] = el;
        NumberOfStrings++;

        return el;
    }

    /// Search the string table for a specific string.
    /// \param name string name to find
    /// \return a pointer to the desired element, or null
    public StringT? Lookup(string name)
    {
        /* create a pointer to a StringT */
        StringT? el = null;

        /* find the string */
        for (var arrayLoc = 0; arrayLoc < NumberOfStrings; arrayLoc++)
        {
            el = vertArray[arrayLoc];
            if (el.Name == name)
                return el;
        }

        /* return whatever we found */
        return null;
    }

    /// Print out the entire string table (for debugging purposes)
    public void PrintTable()
    {
        Console.WriteLine("Name  Value\n----  -----");
        for (var arrayLoc = 0; arrayLoc < NumberOfStrings; arrayLoc++)
            Console.WriteLine("{0}   {1}", vertArray[arrayLoc].Name, vertArray[arrayLoc].String);
    }
}

/// String table object for the mini C# compiler
/// 
/// \author Ross Nelson
public class StringT
{
    public string? Name;
    public string? String;
}