namespace MiniCSharp;

public static class Globals
{
    /* Symbol - token type */
    public enum Symbol : ushort
    {
        Class, /* 'class' token*/
        If, /* 'if' token */
        New, /* 'new' token */
        Foreach, /* 'foreach' token */
        Const, /* 'const' token */
        Float, /* 'float' token */
        Null, /* 'null' token */
        In, /* 'in' token */
        Public, /* 'public' token */
        Private, /* 'private' token */
        Return, /* 'return' token */
        This, /* 'this' token */
        Using, /* 'using' token */
        Char, /* 'char' token */
        Else, /* 'else' token */
        Int, /* 'int' token */
        Static, /* 'static' token */
        Namespace, /* 'namespace' token */
        Void, /* 'void' token */
        Ref, /* 'ref' token */
        Out, /* 'out' token */
        Read, /* 'read' token */
        Write, /* 'write' token */
        Writeln, /* 'writeln' token */
        Relop, /* relational operator token */ // < > <= >=
        Condop, /* conditional operator token */ // == !=
        Unarynot, /* unary not token */ // !
        Addop, /* addition operator token */ // +
        Signop, /* negation/subtraction operator token */ // -
        Mulop, /* multiplication/division operator token */ // * /
        Assignop, /* assignment operator token */ // =
        Orop, /* or operator token */ // ||
        Borop, /* binary or operator token */ // |
        Andop, /* and operator token */ // &&
        Bandop, /* binary and operator token */ // &
        Lparen, /* left parenthesis token */ // (
        Rparen, /* right parenthesis token */ // )
        Lbrace, /* left french brace token */ // {
        Rbrace, /* right french brace token */ // }
        Lbracket, /* left bracket */ // [
        Rbracket, /* right bracket */ // ]
        Comma, /* comma token */ // ,
        Colon, /* colon token */ // :
        Semicolon, /* semicolon token */ // ;
        Period, /* period token */ // .
        Quote, /* single quote token */ // '
        Dquote, /* double quote token */ // "
        Number, /* numerical value token */
        Numfloat, /* floating point value token */
        Literal, /* literal characters/strings */
        Eof, /* end of file token */
        Identifier, /* identifier token */
        Constructor, /* constructors */
        Comment, /* a comment - ignore it */
        Unknown /* unknown token */
    }

    public const char EndOfFile = '\uffff'; /* EOF in C#: ^E --- ???*/
    /* "global" variables for Program.cs */
    // none

    // "Global" variables for Lexical.cs
    public static string Lexeme; /* the lexeme GetNextToken() finds */
    public static int Value; /* integer value */
    public static double ValueF; /* floating point value */
    public static string Literal; /* a string literal */
    public static string[] ReservedWords; /* the reserved words list */
    public static string[] Tokens; /* textual representation of tokens */
    public static int Linecount; /* the number of lines printed to the screen */
    public static int CurLine; /* the current line number */
    public static string Filename; /* the filename */

    public static int Depth; /* the depth we're at */
    public static HashTable SymbolTable = null!; /* symbol table */
    public static StringTable StringTable = null!; /* string table */

    public static bool Visual; /* print TAC/asm to the screen? */

    /** for Lexical **/
    public static Symbol Token; /* token type */

    /* Wait()
     *
     * Method to pause and clear the screen
     */
    public static void Wait(string message)
    {
        Console.WriteLine(message);
        Console.ReadLine();
        Console.Clear();
    }

    /* Initialize()
     *
     * This method *MUST* be called before Globals is used, it sets
     * anything up for the Globals class that has to be done at runtime.
     */
    public static void Initialize()
    {
        var index = 0;
        Token = Symbol.Unknown;

        /* set the initial line count */
        Linecount = 0;

        /* we've not yet read a line... */
        CurLine = 0;

        /* set our outer depth */
        Depth = 0;

        /* create the symbol table */
        SymbolTable = new HashTable();

        /* create the string table */
        StringTable = new StringTable();

        /* let's be quiet */
        Visual = false;

        /* create memory for the reserved words */
        ReservedWords = new string[24];

        /* set all of the reserved words */
        ReservedWords[index++] = "class";
        ReservedWords[index++] = "if";
        ReservedWords[index++] = "new";
        ReservedWords[index++] = "foreach";
        ReservedWords[index++] = "const";
        ReservedWords[index++] = "float";
        ReservedWords[index++] = "null";
        ReservedWords[index++] = "in";
        ReservedWords[index++] = "public";
        ReservedWords[index++] = "private";
        ReservedWords[index++] = "return";
        ReservedWords[index++] = "this";
        ReservedWords[index++] = "using";
        ReservedWords[index++] = "char";
        ReservedWords[index++] = "else";
        ReservedWords[index++] = "int";
        ReservedWords[index++] = "static";
        ReservedWords[index++] = "namespace";
        ReservedWords[index++] = "void";
        ReservedWords[index++] = "ref";
        ReservedWords[index++] = "out";
        ReservedWords[index++] = "read";
        ReservedWords[index++] = "write";
        ReservedWords[index++] = "writeln";

        /* do the same for Tokens */
        index = 0;
        Tokens = new string[55];
        Tokens[index++] = "class";
        Tokens[index++] = "if";
        Tokens[index++] = "new";
        Tokens[index++] = "foreach";
        Tokens[index++] = "const";
        Tokens[index++] = "float";
        Tokens[index++] = "null";
        Tokens[index++] = "in";
        Tokens[index++] = "public";
        Tokens[index++] = "private";
        Tokens[index++] = "return";
        Tokens[index++] = "this";
        Tokens[index++] = "using";
        Tokens[index++] = "char";
        Tokens[index++] = "else";
        Tokens[index++] = "int";
        Tokens[index++] = "static";
        Tokens[index++] = "namespace";
        Tokens[index++] = "void";
        Tokens[index++] = "ref";
        Tokens[index++] = "out";
        Tokens[index++] = "read";
        Tokens[index++] = "write";
        Tokens[index++] = "writeln";
        Tokens[index++] = "relop";
        Tokens[index++] = "condop";
        Tokens[index++] = "unarynot";
        Tokens[index++] = "addop";
        Tokens[index++] = "signop";
        Tokens[index++] = "mulop";
        Tokens[index++] = "assignop";
        Tokens[index++] = "orop";
        Tokens[index++] = "borop";
        Tokens[index++] = "andop";
        Tokens[index++] = "bandorop";
        Tokens[index++] = "lparen";
        Tokens[index++] = "rparen";
        Tokens[index++] = "lbrace";
        Tokens[index++] = "rbrace";
        Tokens[index++] = "lbracket";
        Tokens[index++] = "rbracket";
        Tokens[index++] = "comma";
        Tokens[index++] = "colon";
        Tokens[index++] = "semicolon";
        Tokens[index++] = "period";
        Tokens[index++] = "quote";
        Tokens[index++] = "dquote";
        Tokens[index++] = "number";
        Tokens[index++] = "numfloat";
        Tokens[index++] = "literal";
        Tokens[index++] = "eof";
        Tokens[index++] = "identifier";
        Tokens[index++] = "constructor";
        Tokens[index++] = "comment";
        Tokens[index++] = "unknown";
    }

    public static string GetFilename(string fullName, char parseChar) => fullName[..fullName.LastIndexOf(parseChar)];
}