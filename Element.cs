namespace MiniCSharp;

/// <summary>
/// Parameter information for methods
/// </summary>
public class Parameter
{
    /// <summary>
    /// Passing mode
    /// </summary>
    public Element.PassingMode Mode { get; set; }
    
    /// <summary>
    /// Variable type
    /// </summary>
    public Element.VariableType Type { get; set; }

    /// <summary>
    /// Return a string indicating the passing mode for the object
    /// </summary>
    /// <returns>The textual representation of the passing mode</returns>
    public override string ToString() =>
        Mode switch
        {
            Element.PassingMode.Reference => "ref",
            Element.PassingMode.Output => "out",
            _ => string.Empty
        };
}

/// <summary>
/// An element in the hash table
/// </summary>
public class Element
{
    public enum EntryType
    {
        Variable,
        Constant,
        Method,
        Class,
        Empty
    }

    public enum PassingMode
    {
        Normal, // pass by value
        Reference,
        Output
    }

    public enum VariableType
    {
        Int32,
        Char,
        Float,
        Void,
        Empty
    }

    private Globals.Symbol _accessibilityModifier; // access modifier
    private int _depth; // the depth we are at
    private char _charValue; // value - VarType:charType
    private float _floatValue; // value - VarType:Float
    private int _integerValue; // value - VarType:Int32
    private string? _lexeme; // the element's lexeme
    private int _parameterCount; // number of parameters - EntryType:methodType
    private int _offset; // offset
    private string? _nameInOffsetNotation; // variable in offset notation
    private int _sizeOfLocals; // size of local variables
    private int _sizeOfParameters; // size of parameters
    private Globals.Symbol _token; // token type
    private EntryType _type; // entry type
    
    /// <summary>
    /// List of children (if applicable)
    /// </summary>
    public string? ChildList { get; set; }
    
    /// <summary>
    /// The location it is in a method declaration
    /// </summary>
    public int Location { get; set; }
    
    /// <summary>
    /// Passing mode for the element
    /// </summary>
    public PassingMode Mode { get; set; } = PassingMode.Normal;
    
    /// <summary>
    /// The next element in the list
    /// </summary>
    public Element? Next { get; set; }
    
    /// <summary>
    /// The parent element to a given element
    /// </summary>
    public Element? Parent { get; set; }
    
    /// <summary>
    /// Variable type
    /// </summary>
    public VariableType Type { get; set; } = VariableType.Empty;

    /// <summary>
    /// Constructor for the Element class
    /// </summary>
    public Element()
    {
        InitValues();
    }

    /// <summary>
    /// Set default values for the element
    /// </summary>
    public void InitValues()
    {
        _type = EntryType.Empty;
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

    /// <summary>
    /// Set the access modifier for the element
    /// </summary>
    /// <param name="accessModifier">The access modifier to use</param>
    public void SetAccess(Globals.Symbol accessModifier)
    {
        _accessibilityModifier = accessModifier;
    }

    /// <summary>
    /// Set the element to be of type int
    /// </summary>
    public void SetInteger()
    {
        Type = VariableType.Int32;
        SetToken(Globals.Symbol.Int);
        if (_type != EntryType.Constant)
            SetVariable();
    }

    /// <summary>
    /// Set the element to be of type float
    /// </summary>
    public void SetFloat()
    {
        Type = VariableType.Float;
        SetToken(Globals.Symbol.Float);
        if (_type != EntryType.Constant)
            SetVariable();
    }

    /// <summary>
    /// Set the element to be of type char
    /// </summary>
    public void SetCharacter()
    {
        Type = VariableType.Char;
        SetToken(Globals.Symbol.Char);
        if (_type != EntryType.Constant)
            SetVariable();
    }

    /// <summary>
    /// Set the element to be of type const
    /// </summary>
    public void SetConstant()
    {
        _type = EntryType.Constant;
    }

    /// <summary>
    /// Set the element to be a method/function
    /// </summary>
    /// <param name="returnType">The method's return type</param>
    public void SetMethod(Globals.Symbol returnType)
    {
        _type = EntryType.Method;

        // Set the method's return type
        Type = (int)returnType switch
        {
            (int)Globals.Symbol.Int => VariableType.Int32,
            (int)Globals.Symbol.Char => VariableType.Char,
            (int)Globals.Symbol.Float => VariableType.Float,
            (int)Globals.Symbol.Void => VariableType.Void,
            _ => VariableType.Empty
        };
    }

    /// <summary>
    /// Set the element to be a class
    /// </summary>
    public void SetClass()
    {
        _type = EntryType.Class;
    }

    /// <summary>
    /// Set the element to be a variable
    /// </summary>
    public void SetVariable()
    {
        _type = EntryType.Variable;
    }

    /// <summary>
    /// Set the integer value of the element
    /// </summary>
    /// <param name="value">The value for the object to hold</param>
    public void SetValue(int value)
    {
        _integerValue = value;
    }

    /// <summary>
    /// Set the floating point value of the element
    /// </summary>
    /// <param name="value">The value for the object to hold</param>
    public void SetValue(float value)
    {
        _floatValue = value;
    }

    /// <summary>
    /// Set the character value of the element
    /// </summary>
    /// <param name="value">The value for the object to hold</param>
    public void SetValue(char value)
    {
        _charValue = value;
    }

    /// <summary>
    /// Set the token type for the element
    /// </summary>
    /// <param name="tokenType">token type</param>
    public void SetToken(Globals.Symbol tokenType)
    {
        _token = tokenType;
    }

    /// <summary>
    /// Get the integer value of the element
    /// </summary>
    /// <returns>The value stored in the object</returns>
    public int GetIntegerValue() => _integerValue;

    /// <summary>
    /// Get the floating point value of the element
    /// </summary>
    /// <returns>The value stored in the object</returns>
    public float GetFloatValue() => _floatValue;

    /// <summary>
    /// Get the character value of the element
    /// </summary>
    /// <returns>The value stored in the object</returns>
    public char GetCharacterValue() => _charValue;

    /// <summary>
    /// Get the token type for the element
    /// </summary>
    /// <returns>token type</returns>
    public Globals.Symbol GetToken() => _token;

    /// <summary>
    /// Set the variable name
    /// </summary>
    /// <param name="name">The name of the variable to be stored</param>
    public void SetName(string? name)
    {
        _lexeme = name;
    }

    /// <summary>
    /// Set the offset value
    /// </summary>
    /// <param name="value">The offset</param>
    public void SetOffset(int value)
    {
        _offset = value;
        _nameInOffsetNotation = $"_BP{(_offset < 0 ? _offset.ToString() : $"+{_offset}")}";

        if (_depth == 1)
            _nameInOffsetNotation = _lexeme;
    }

    /// <summary>
    /// Set the size of local variables
    /// </summary>
    /// <param name="value">size of local variables in a class/method</param>
    public void SetSizeOfLocals(int value)
    {
        _sizeOfLocals = value;
    }

    /// <summary>
    /// Set the size of parameters
    /// </summary>
    /// <param name="value">size of parameters in a method</param>
    public void SetSizeOfParams(int value)
    {
        _sizeOfParameters = value;
    }

    /// <summary>
    /// Set the number of parameters that a method has
    /// </summary>
    /// <param name="value">The number of parameters</param>
    public void SetNumParams(int value)
    {
        _parameterCount = value;
    }

    /// <summary>
    /// Set the depth for a specific element
    /// </summary>
    /// <param name="value">The depth</param>
    public void SetDepth(int value)
    {
        _depth = value;
    }

    /// <summary>
    /// Obtain the name of the variable
    /// </summary>
    /// <returns>The variable name</returns>
    public string? GetName() => _lexeme;

    /// <summary>
    /// Obtain the offset
    /// </summary>
    /// <returns>The offset</returns>
    public int GetOffset() => _offset;

    /// <summary>
    /// Obtain the variable in offset notation
    /// </summary>
    /// <returns>The offset notation</returns>
    public string? GetOffsetName() => _nameInOffsetNotation;

    /// <summary>
    /// Obtain the size of the variable or the size of local variables in the class/method
    /// </summary>
    /// <returns>The size</returns>
    public int GetSizeOfLocals() => _sizeOfLocals;

    /// <summary>
    /// Obtain the size of the variable or the size of parameters in the method
    /// </summary>
    /// <returns>The size</returns>
    public int GetSizeOfParams() => _sizeOfParameters;

    /// <summary>
    /// Obtain the depth of the variable
    /// </summary>
    /// <returns>The depth</returns>
    public int GetDepth() => _depth;

    /// <summary>
    /// Obtain the number of parameters of the variable
    /// </summary>
    /// <returns>The number of parameters</returns>
    public int GetNumParams() => _parameterCount;

    /// <summary>
    /// Get the type of the element
    /// </summary>
    /// <returns>EntryType of the element</returns>
    public EntryType GetEntryType() => _type;

    /// <summary>
    /// Get the type of the element
    /// </summary>
    /// <returns>VarType of the element</returns>
    public VariableType GetVariableType() => Type;
}