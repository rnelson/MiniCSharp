namespace MiniCSharp;

/// <summary>
/// Hash table for the mini C# compiler
/// </summary>
public class HashTable
{
    /// Prime number to be used for the table size
    private const uint PrimeNumber = 50333;

    /// The array holding pointers to Element objects
	private readonly Element?[] vertArray;

	/// Constructor for the hash table class
	public HashTable()
    {
        /* allocate space for the hash values */
        vertArray = new Element?[PrimeNumber];
        InitTable();
    }

	/// Initialize the hash table
	public void InitTable()
    {
        for (var i = 0; i < PrimeNumber; i++)
            vertArray[i] = null;
    }

	/// Completely remove a depth from the hash table
	/// \param depth depth to remove
	public void DeleteDepth(int depth)
    {
        if (Globals.Visual)
            WriteTable(Globals.Depth);

        /* we don't want to kill depths 1, 2 */
        if (depth < 2)
            return;

        for (var loc = 0; loc < PrimeNumber; loc++)
        {
            var e = vertArray[loc];

            /* if there's something there, check the depth */
            if (e != null)
            {
                /* find the start of the correct depth */
                while (e.GetDepth() > depth && e.Next != null)
                    e = e.Next;

                if (e == null)
                    break;

                /* if the depth is the same, remove the node */
                while (e.GetDepth() == depth)
                {
                    vertArray[loc] = e.Next; /* remove the head node */
                    e = vertArray[loc]; /* reset e to the front of that linked list */

                    if (e == null || e.Next == null)
                        break;
                }
            }
        }
    }

	/// Insert a new element into the hash table
	/// \param lexeme the name of the variable
	/// \param token token type
	/// \param depth the depth at which to store the Element
	/// \return pointer to the new element
	public Element? Insert(string lexeme, Globals.Symbol token, int depth)
    {
        /* reserve memory for the new node */
        var el = new Element();

        /* figure out where the node goes */
        var arrayLoc = Hash(lexeme);

        /* set values for the node */
        el.InitValues();
        el.SetName(lexeme);
        el.SetToken(token);
        el.SetDepth(depth); // for some reason, mono crashed without casting this for anything set to a method
        el.Next = null;

        /* keep track of if we have a constant or not */
        var haveConst = el.GetEType() == Element.EntryType.ConstType;

        /* set the type */
        switch ((int)token)
        {
            case (int)Globals.Symbol.Int:
                el.SetInteger();
                break;
            case (int)Globals.Symbol.Float:
                el.SetFloat();
                break;
            case (int)Globals.Symbol.Char:
                el.SetCharacter();
                break;
            case (int)Globals.Symbol.Const:
                el.SetConstant();
                break;
            case (int)Globals.Symbol.Class:
                el.SetClass();
                break;
            default:
                el.SetMethod(Globals.Symbol.Void);
                break;
        }

        /* add the node */
        try
        {
            var oldHead = vertArray[arrayLoc];
            el.Next = oldHead;
            vertArray[arrayLoc] = el;

            if (haveConst)
                el.SetConstant();
        }
        catch
        {
            vertArray[arrayLoc] = el;

            if (haveConst)
                el.SetConstant();
        }

        return el;
    }

    /// Search the hash table for a specific element.
	/// \param lexeme variable name to find
	/// \return a pointer to the desired element, or null
	public Element? Lookup(string lexeme)
    {
        /* create a pointer to an Element */
        Element? el;

        /* figure out where the node goes */
        var arrayLoc = Hash(lexeme);

        /* find the node */
        if (vertArray[arrayLoc] == null)
        {
            el = null;
        }
        else
        {
            el = vertArray[arrayLoc];

            /* if needed (collisions), traverse the list to find the element */
            while (el.GetName() != lexeme)
            {
                el = el.Next;

                /* we don't want to try to access inaccessible memory */
                if (el == null)
                    return null;
            }
        }

        /* return whatever we found */
        return el;
    }

	/// Print all entries at a given depth
	/// \param depth the depth to print out
	public void WriteTable(int depth)
    {
        var header = $"\nSymbol Table - Depth {depth}\n----------------------";
        Console.WriteLine(header);

        /* create a pointer to an Element */
        Element? el;

        /* print out the values */
        foreach (var e in vertArray)
        {
            el = e;

            if (el != null)
            {
                /* advance to the correct depth if needed */
                while (el.GetDepth() > depth)
                    if (el.Next != null)
                        el = el.Next;
                    else
                        break;

                while (el != null && el.GetDepth() == depth)
                {
                    /* increment the line count */
                    Globals.Linecount += 3;

                    /* if we've printed a lot, clear the screen */
                    if (Globals.Linecount > 15)
                    {
                        /* reset the line count */
                        Globals.Linecount = 0;
                        /* wait for the user to press enter */
                        Globals.Wait("\nPress enter to continue...");

                        /* print out the header */
                        Console.WriteLine(header);
                    }

                    var etype = el.GetEType();
                    var vtype = el.GetVType();
                    var lexeme = el.GetName();
                    var token = Globals.Tokens[(int)el.GetToken()];
                    var offset = el.GetOffset().ToString();
                    var size = el.GetSizeOfLocals().ToString();
                    string vartype = string.Empty, value = string.Empty, output = string.Empty;

                    switch ((int)vtype)
                    {
                        case (int)Element.VarType.IntType:
                            vartype = "int";
                            value = el.GetIntegerValue().ToString();
                            break;
                        case (int)Element.VarType.CharType:
                            vartype = "char";
                            value = el.GetCharacterValue().ToString();
                            break;
                        case (int)Element.VarType.FloatType:
                            vartype = "float";
                            value = el.GetFloatValue().ToString();
                            break;
                        case (int)Element.VarType.VoidType:
                            vartype = "void";
                            value = string.Empty;
                            break;
                        default:
                            vartype = string.Empty;
                            value = string.Empty;
                            break;
                    }

                    /* to keep the relatively readable, just shove all of the */
                    /* information into one big string and spit that out to the screen */
                    switch ((int)etype)
                    {
                        case (int)Element.EntryType.VarType:
                            switch ((int)el.Mode)
                            {
                                case (int)Element.PassingMode.PassOut:
                                    output += "out ";
                                    break;
                                case (int)Element.PassingMode.PassRef:
                                    output += "ref ";
                                    break;
                                case (int)Element.PassingMode.PassNorm:
                                default:
                                    break;
                            }

                            output += vartype;
                            if (vartype != string.Empty) output += " ";
                            output += lexeme; // + " = ";
                            // output += value;
                            output += $"\t//size:{size}";
                            output += $" offset:{offset}";
                            break;
                        case (int)Element.EntryType.ConstType:
                            output += "const ";
                            output += $"{vartype} ";
                            output += lexeme;
                            output += $"\t//size:{size}";
                            output += $" offset:{offset}";

                            switch ((int)el.GetVType())
                            {
                                case (int)Element.VarType.IntType:
                                    output += $" value:{el.GetIntegerValue()}";
                                    break;
                                case (int)Element.VarType.FloatType:
                                    output += $" value:{el.GetFloatValue()}";
                                    break;
                            }

                            break;
                        case (int)Element.EntryType.MethodType:
                            output += vartype;
                            if (vartype != string.Empty) output += " ";
                            output += $"{lexeme}() ";
                            output += $"\t//size:{size}";
                            output += $" params:{el.GetNumParams()}";
                            output += $" offset:{offset}";
                            if (el.ChildList.Length > 0)
                                output += el.ChildList;
                            break;
                        case (int)Element.EntryType.ClassType:
                            output += "class ";
                            output += $"{lexeme} ";
                            output += $"\t//size:{size}";
                            //output += " offset:" + offset;
                            if (el.ChildList.Length > 0)
                                output += el.ChildList;
                            break;
                        default:
                            output += $"{lexeme}, type:{token} offset:{offset}";
                            break;
                    }

                    if (el.Location > 0)
                        output += $" location:{el.Location}";

                    Console.WriteLine(output);

                    /* break out of the loop or move to the next item, whichever is needed */
                    if (el.Next == null)
                    {
                        break;
                    }

                    el = el.Next;
                    if (el == null)
                        break;
                }
            }
        }
    }

	/// Get the total size for a given depth
	/// \param dep the depth
	/// \return the size of the depth
	public int GetDepthSize(int dep)
    {
        var retval = 0;
        Element? search = null;

        foreach (var el in vertArray)
        {
            search = el;

            while (search != null)
            {
                if (search.GetDepth() == dep)
                    retval += search.GetSizeOfLocals();

                search = search.Next;
            }
        }

        return retval;
    }

	/// Get the smallest used offset (for newtemp()
	/// \return the smallest used offset
	public int GetMinOffset()
    {
        var retval = 0;
        Element? search = null;

        foreach (var el in vertArray)
        {
            search = el;

            while (search != null)
            {
                if (search.GetOffset() < retval)
                    retval = search.GetOffset();

                search = search.Next;
            }
        }

        return retval;
    }

	/// Get a string with variables for .data
	/// \param dep the depth to print
	/// \return string containing declaractions for variables
	public string GenerateAssemblyData(int dep)
    {
        var retval = string.Empty;
        Element? search = null;

        foreach (var el in vertArray)
        {
            search = el;

            while (search != null)
            {
                if (search.GetDepth() == dep)
                    if (search.GetEType() == Element.EntryType.VarType)
                        if (retval.Length == 0)
                            retval = $"{search.GetName()} dw ?";
                        else
                            retval += $"\n{search.GetName()} dw ?";

                search = search.Next;
            }
        }

        return retval;
    }

	/// Get child information for an element
	/// \param ele the element
	/// \return string containing child information, parsable format
	public string GetChildren(Element? ele)
    {
        var retstr = string.Empty;
        Element? search = null;

        foreach (var elem in vertArray)
        {
            search = elem;

            while (search != null)
            {
                if (search.Parent == ele)
                {
                    switch ((int)search.Mode)
                    {
                        case (int)Element.PassingMode.PassOut:
                            retstr += "out ";
                            break;
                        case (int)Element.PassingMode.PassRef:
                            retstr += "ref ";
                            break;
                        case (int)Element.PassingMode.PassNorm:
                        default:
                            break;
                    }

                    switch ((int)search.GetVType())
                    {
                        case (int)Globals.Symbol.Int:
                            retstr += "int ";
                            break;
                        case (int)Globals.Symbol.Char:
                            retstr += "char ";
                            break;
                        case (int)Globals.Symbol.Float:
                            retstr += "float ";
                            break;
                    }

                    retstr += search.GetName();
                    retstr += " | ";
                }

                search = search.Next;
            }
        }

        return retstr;
    }

	/// Get child information for an element
	/// \param ele the element
	/// \return string containing child information, printable format
	public string GetChildrenPrint(Element? ele)
    {
        var retstr = "\n";
        Element? search = null;

        foreach (var elem in vertArray)
        {
            search = elem;

            while (search != null)
            {
                if (search.Parent == ele)
                {
                    retstr += " => ";
                    switch ((int)search.Mode)
                    {
                        case (int)Element.PassingMode.PassOut:
                            retstr += "out ";
                            break;
                        case (int)Element.PassingMode.PassRef:
                            retstr += "ref ";
                            break;
                        case (int)Element.PassingMode.PassNorm:
                        default:
                            break;
                    }

                    switch ((int)search.GetVType())
                    {
                        case (int)Globals.Symbol.Int:
                            retstr += "int ";
                            break;
                        case (int)Globals.Symbol.Char:
                            retstr += "char ";
                            break;
                        case (int)Globals.Symbol.Float:
                            retstr += "float ";
                            break;
                    }

                    retstr += search.GetName();
                    retstr += "\n";
                }

                search = search.Next;
            }
        }

        return retstr;
    }

	/// Implementation of hashpjw, page 436 of Compilers (dragon)
	/// \param name string to hash
	private static int Hash(string name)
    {
        long h = 0, g;

        foreach (var ch in name)
        {
            h = (h << 4) + ch;

            if ((g = h & 0xf0000000) != 0)
            {
                h = h ^ (g >> 24);
                h = h ^ g;
            }
        }

        return Math.Abs((int)(h % PrimeNumber));
    }
}