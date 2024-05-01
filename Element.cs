namespace MiniCSharp;

/// Parameter information for methods
/// 
/// \author Ross Nelson
public class Parameter
{
    public Element.PassingMode Mode; /* passing mode */
    public Element.VariableType Type; /* type of the variable */

    /// Constructor for the Parameter class
    public Parameter()
    {
    }

    /// Return a string indicating the passing mode for the object
    /// \return The textual representation of the passing mode
    public override string ToString() =>
        Mode switch
        {
            Element.PassingMode.Reference => "ref",
            Element.PassingMode.Output => "out",
            _ => string.Empty
        };
}

/// An element in the hash table
/// 
/// \author Ross Nelson
public class Element
{
    public enum EntryType
    {
        VarType, /* variable */
        ConstType, /* constant */
        MethodType, /* method/function */
        ClassType, /* class */
        EmptyType /* nada */
    }

    public enum PassingMode
    {
        Normal, /* pass by value */
        Reference, /* pass by reference */
        Output /* out mode passing */
    }

    public enum VariableType
    {
        Int32, /* integer */
        Char, /* character */
        Float, /* float */
        Void, /* void */
        Empty /* nada */
    }

    private Globals.Symbol accessibilityModifier; /* access modifier */
    private char charval; /* value - VarType:charType */
    public string ChildList; /* list of children (if applicable) */

    private int depth; /* the depth we are at */
    private float floatValue; /* value - VarType:Float */
    private int integerValue; /* value - VarType:Int32 */
    private string lexeme; /* the element's lexeme */
    public int Location; /* the location it is in a method declaration */
    public PassingMode Mode { get; set; } = PassingMode.Normal; /* passing mode */
    public Element? Next; /* the next element in the list */

    private int parameterCount; /* number of parameters - EntryType:methodType */

    private int offset; /* offset */
    private string nameInOffsetNotation; /* variable in offset notation */

    public Element? Parent; /* the parent element to a given element */
    private int sizeOfLocals; /* size of local variables */
    private int sizeOfParameters; /* size of parameters */

    private Globals.Symbol token; /* token type */

    private EntryType type; /* entry type */
    public VariableType Type { get; set; } = VariableType.Empty; /* variable type */

    /// Constructor for the Element class
    public Element()
    {
        /* initialize variables */
        InitValues();
    }

    /// Set default values for the element
    public void InitValues()
    {
        type = EntryType.EmptyType;
        Type = VariableType.Empty;
        Mode = PassingMode.Normal;
        Location = 0;
        
        SetName("");
        SetToken(Globals.Symbol.Unknown);
        SetOffset(0);
        SetSizeOfLocals(0);
        SetNumParams(0);
        
        ChildList = string.Empty;
        Parent = null;
    }

    /// Set the access modifier for the element
    public void SetAccess(Globals.Symbol accMod)
    {
        accessibilityModifier = accMod;
    }

    /// Set the element to be of type int
    public void SetInteger()
    {
        Type = VariableType.Int32;
        SetToken(Globals.Symbol.Int);
        if (type != EntryType.ConstType)
            SetVariable();
    }

    /// Set the element to be of type float
    public void SetFloat()
    {
        Type = VariableType.Float;
        SetToken(Globals.Symbol.Float);
        if (type != EntryType.ConstType)
            SetVariable();
    }

    /// Set the element to be of type char
    public void SetCharacter()
    {
        Type = VariableType.Char;
        SetToken(Globals.Symbol.Char);
        if (type != EntryType.ConstType)
            SetVariable();
    }

    /// Set the element to be of type const
    public void SetConstant()
    {
        type = EntryType.ConstType;
    }

    /// Set the element to be a method/function
    public void SetMethod(Globals.Symbol access)
    {
        type = EntryType.MethodType;

        /* set the method's return type */
        Type = (int)access switch
        {
            (int)Globals.Symbol.Int => VariableType.Int32,
            (int)Globals.Symbol.Char => VariableType.Char,
            (int)Globals.Symbol.Float => VariableType.Float,
            (int)Globals.Symbol.Void => VariableType.Void,
            _ => VariableType.Empty
        };
    }

    /// Set the element to be a class
    public void SetClass()
    {
        type = EntryType.ClassType;
    }

    /// Set the element to be a variable
    public void SetVariable()
    {
        type = EntryType.VarType;
    }

    /// Set the integer value of the element
    /// \param value The value for the object to hold
    public void SetValue(int value)
    {
        integerValue = value;
    }

    /// Set the floating point value of the element
    /// \param value The value for the object to hold
    public void SetValue(float value)
    {
        floatValue = value;
    }

    /// Set the character value of the element
    /// \param value The value for the object to hold
    public void SetValue(char value)
    {
        charval = value;
    }

    /// Set the token type for the element
    /// \param tok token type
    public void SetToken(Globals.Symbol tok)
    {
        token = tok;
    }

    /// Get the integer value of the element
    /// \return The value stored in the object
    public int GetIntegerValue()
    {
        return integerValue;
    }

    /// Get the floating point value of the element
    /// \return The value stored in the object
    public float GetFloatValue()
    {
        return floatValue;
    }

    /// Get the character value of the element
    /// \return The value stored in the object
    public char GetCharacterValue()
    {
        return charval;
    }

    /// Get the token type for the element
    /// \return token type
    public Globals.Symbol GetToken()
    {
        return token;
    }

    /// Set the variable name
    /// \param name The name of the variable to be stored
    public void SetName(string name)
    {
        lexeme = name;
    }

    /// Set the offset value
    /// \param value The offset
    public void SetOffset(int value)
    {
        offset = value;
        nameInOffsetNotation = $"_BP{(offset < 0 ? offset.ToString() : $"+{offset}")}";

        if (depth == 1)
            nameInOffsetNotation = lexeme;
    }

    /// Set the size of local variables
    /// \param value size of local variables in a class/method
    public void SetSizeOfLocals(int value)
    {
        sizeOfLocals = value;
    }

    /// Set the size of parameters
    /// \param value size of parameters in a method
    public void SetSizeOfParams(int value)
    {
        sizeOfParameters = value;
    }

    /// Set the number of parameters that a method has
    /// \param value The number of parameters
    public void SetNumParams(int value)
    {
        parameterCount = value;
    }

    /// Set the depth for a specific element
    /// \param value The depth
    public void SetDepth(int value)
    {
        depth = value;
    }

    /// Obtain the name of the variable
    /// \return The variable name
    public string GetName()
    {
        return lexeme;
    }

    /// Obtain the offset
    /// \return The offset
    public int GetOffset()
    {
        return offset;
    }

    /// Obtain the variable in offset notation
    /// \return The offset notation
    public string GetOffsetName()
    {
        return nameInOffsetNotation;
    }

    /// Obtain the size of the variable or the size of local variables in the class/method
    /// \return The size
    public int GetSizeOfLocals()
    {
        return sizeOfLocals;
    }

    /// Obtain the size of the variable or the size of parameters in the method
    /// \return The size
    public int GetSizeOfParams()
    {
        return sizeOfParameters;
    }

    /// Obtain the depth of the variable
    /// \return The depth
    public int GetDepth()
    {
        return depth;
    }

    /// Obtain the number of parameters of the variable
    /// \return The number of parameters
    public int GetNumParams()
    {
        return parameterCount;
    }

    /// Get the type of the element
    /// \return EntryType of the element
    public EntryType GetEType()
    {
        return type;
    }

    /// Get the type of the element
    /// \return VarType of the element
    public VariableType GetVType()
    {
        return Type;
    }
}