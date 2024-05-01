using System.Text;

namespace MiniCSharp;

internal class Parser
{
    /// the size of the local variables in a class
    private int classSize;

    /// the most recently inserted object (for setting parent)
    private Element? currentObject;

    /// the class that we're in
    private Element? currentClass;

    /// the method that we're in
    private Element? currentMethod;

    /// we found Main() -- error if not
    private bool foundMain;

    /// the size of local variables in a method
    private int functionLocalsSize;

    /// keep track of whether we're doing a method -- depth changes
    private bool incAtBrace;

    /// are we inside an ( Expr ) sequence?
    private bool isInExpression;

    /// are we in ParamList? (used with varLoc)
    private bool isInParameterList;

    /// keep track of whether an identifier is a method
    private bool isInFunction;

    /// a lexical analyzer object
    private readonly Lexical lex;

    /// the current offset (for a given block)
    private int localOffset;

    /// minimum offset in the current block
    private int minOffset;

    /// we are in Main()
    private bool nowMain;

    /// the offset multiplier (to get negative values)
    private int offsetmul;

    /// the current parent object (if applicable)
    private Element? parent;

    /// START instruction for TAC
    private string tacStartInstruction;

    /// TAC stream
    private readonly StreamWriter tacStreamWriter;

    /// the location of a parameter
    private int varLoc;

    /// The parser's contructor, which sets some default values
    /// and opens up the passed in filename
    /// \param filename the file to open
    public Parser(string filename)
    {
        lex = new Lexical(filename);
        lex.GetNextToken();

        /* give the class-scope variables some real values */
        incAtBrace = true;
        isInFunction = false;
        isInParameterList = false;
        foundMain = false;
        nowMain = false;
        functionLocalsSize = 0;
        classSize = 0;
        varLoc = 0;
        parent = null;
        currentObject = null;
        currentClass = null;
        localOffset = 2;
        offsetmul = -1;
        minOffset = 0;

        /* don't forget the three-address-code stuff */
        tacStartInstruction = string.Empty;
        isInExpression = false;

        /* open the TAC file */
        var tacFilestream = new FileStream($"{Globals.GetFilename(Globals.Filename, '.')}.tac", FileMode.Create);
        tacStreamWriter = new StreamWriter(tacFilestream, Encoding.UTF8);

        /* run the function for the first grammar rule */
        Prog();

        /* flush the StreamWriter buffer and close the files */
        try
        {
            tacStreamWriter.Write(tacStartInstruction);

            if (Globals.Visual)
                Console.WriteLine(tacStartInstruction);

            tacStreamWriter.Flush();
            tacStreamWriter.Close();
            tacFilestream.Close();
        }
        catch
        {
            /* oh well! */
        }
    }

    /// Dummy function that does nothing
    public void Dummy()
    {
        /* do nothing */
    }

    /// Spit out a error message and terminate execution
    /// \param line line number
    /// \param expected the expected token
    /// \param found the token that was found
    private static void Error(int line, string expected, string found)
    {
        Console.WriteLine("error: {0}:{1}: expecting {2} but found {3}", Globals.Filename, line, expected, found);
        Globals.Wait("\nPress enter...");
        Environment.Exit(42);
    }

    /// Spit out a error message and terminate execution
    /// \param message the error message
    private static void Error(string message)
    {
        Console.WriteLine("{0}", message);
        Globals.Wait("\nPress enter...");
        Environment.Exit(42);
    }

    /// Spit out a warning message
    /// \param line line number
    /// \param expected the expected token
    private static void Warning(int line, string expected)
    {
        Console.WriteLine("warning: {0}:{1}: expecting {0}", expected);
    }

    /// Spit out a warning message
    /// \param message the error message
    private static void Warning(string message)
    {
        Console.WriteLine("{0}", message);
    }

    /// Compare the current token with the desired one, error out if they don't match
    /// \param desired the desired token
    private void Match(Globals.Symbol desired)
    {
        var loc = (int)desired;
        var wanted = Globals.Tokens[loc];
        var found = Globals.Tokens[(int)Globals.Token];

        if (Globals.Token == Globals.Symbol.Eof)
            if (desired != Globals.Symbol.Eof)
                Error(Globals.CurLine, wanted, "end of file");

        if (desired == Globals.Token)
        {
            /* increase/decrease the depth as needed */
            if (desired == Globals.Symbol.Lbrace)
            {
                if (incAtBrace)
                {
                    Globals.Depth++;
                    parent = currentObject;
                    localOffset = 2;
                    offsetmul = -1;
                    functionLocalsSize = 0;
                }
                else
                {
                    incAtBrace = true;
                }
            }
            else if (desired == Globals.Symbol.Lparen)
            {
                incAtBrace = false;
                parent = currentObject;
                Globals.Depth++;
                localOffset = 4;
                offsetmul = 1;
                functionLocalsSize = 0;
            }
            else if (desired == Globals.Symbol.Rparen)
            {
                isInParameterList = false;
            }
            else if (desired == Globals.Symbol.Rbrace)
            {
                /* save the child lists */
                try
                {
                    parent.ChildList = Globals.SymbolTable.GetChildrenPrint(parent);
                    currentObject.ChildList = Globals.SymbolTable.GetChildrenPrint(currentObject);

                    /* restore the offset */
                    localOffset = parent.GetOffset();

                    /* save the method/class size */
                    functionLocalsSize = 0;

                    /* unset currentclass if needed */
                    if (currentClass == parent)
                        currentClass = null;

                    /* do the same with currentmethod */
                    currentMethod = null;

                    /* kill this depth */
                    Globals.SymbolTable.DeleteDepth(Globals.Depth);
                    Globals.Depth--;

                    /* go up a level for parent */
                    if (currentObject != null)
                        parent = parent.Parent;
                }
                catch
                {
                    /* temp variables break this */
                }
            }
            else if (desired == Globals.Symbol.Identifier)
            {
                /* ...in case we miss match("Main") */
                if (Globals.Lexeme == "Main")
                {
                    /* this bug should be fixed.  if we hit this, something went wrong. */
                    foundMain = true;

                    /* save the TAC START line */
                    tacStartInstruction = "START BROKEN_READ.Main";
                }
            }

            if (desired != Globals.Symbol.Eof)
                lex.GetNextToken();
        }
        else if (Globals.Token == Globals.Symbol.Comment)
        {
            if (desired != Globals.Symbol.Eof)
            {
                lex.GetNextToken();
                Match(desired);
            }
        }
        else
        {
            Error(Globals.CurLine, wanted, found);
        }
    }

    /// Compare the current token with the desired one, error out if they don't match
    /// \param desired the desired string
    private void Match(string desired)
    {
        if (Globals.Token == Globals.Symbol.Eof) Error(Globals.CurLine, desired, "end of file");

        if (desired == Globals.Lexeme)
        {
            lex.GetNextToken();
        }
        else if (Globals.Lexeme[..2] == "//")
        {
            lex.GetNextToken();
            Match(desired);
        }
        else
        {
            Error(Globals.CurLine, desired, Globals.Lexeme);
        }
    }

    /// Create a new Element object, used for temporary values
    /// \return pointer to a new object
    private Element? Newtemp()
    {
        /* find the next minimum offset */
        minOffset = Globals.SymbolTable.GetMinOffset();
        minOffset -= 2;

        /* get a logical name for the next temporary variable */
        var tName = "_BP";
        tName += minOffset < 0 ? minOffset.ToString() : $"-{minOffset}";

        /* create and return the new element */
        var e = AddSymbol(Globals.Symbol.Unknown, Globals.Symbol.Private, Globals.Symbol.Int, Globals.Symbol.Int, tName,
            Globals.Depth);

        /* make sure the temporary has the right offset */
        e.SetOffset(minOffset);

        return e;
    }

    /// Print a line of TAC code
    /// \param text the text to be printed
    private void Emit(string text)
    {
        /* add the line to the TAC file */
        tacStreamWriter.Write(text);

        /* print the line to the screen if requested */
        if (Globals.Visual)
        {
            if (Globals.Linecount <= 15)
            {
                Console.Write(text);
            }
            else
            {
                Globals.Linecount = 0;
                Globals.Wait("Hit enter to continue...");
                Console.Write(text);
            }

            Globals.Linecount++;
        }
    }

    /// Add an element to the symbol table
    /// \param accMod access modifier (ref, out)
    /// \param type type of symbol being added
    /// \param token I'm not quite sure why I added this
    /// \param lexeme lexeme (string representation of the symbol)
    /// \param depth the depth at which the lexeme should be added
    /// \return address to the new symbol
    private Element? AddSymbol(Globals.Symbol pMode, Globals.Symbol accMod, Globals.Symbol type, Globals.Symbol token,
        string lexeme, int depth)
    {
        /* add the identifier */
        var e = Globals.SymbolTable.Lookup(lexeme);

        /* keep track of the size for offset information */
        var mysize = 0;

        /* check for duplicates */
        if (e != null)
            if (e.GetDepth() == Globals.Depth)
                Error($"error: duplicate symbol \"{lexeme}\" found on line {Globals.CurLine}");

        e = Globals.SymbolTable.Insert(lexeme, type, depth);
        currentObject = e;

        /* set the access */
        e.SetAccess(accMod);

        /* set the type and size */
        if (!isInFunction)
        {
            switch ((int)type)
            {
                case (int)Globals.Symbol.Int:
                    e.SetInteger();
                    mysize = 2;
                    break;
                case (int)Globals.Symbol.Float:
                    e.SetFloat();
                    mysize = 4;
                    break;
                case (int)Globals.Symbol.Char:
                    e.SetCharacter();
                    mysize = 1;
                    break;
                case (int)Globals.Symbol.Class:
                    e.SetClass();
                    mysize = 0;
                    currentClass = e;
                    break;
            }

            if (token != Globals.Symbol.Const && !isInParameterList)
            {
                e.SetSizeOfLocals(mysize);
                functionLocalsSize += mysize;
            }
        }
        else /* we are dealing with a method */
        {
            switch ((int)type)
            {
                case (int)Globals.Symbol.Int:
                    e.SetInteger();
                    break;
                case (int)Globals.Symbol.Float:
                    e.SetFloat();
                    break;
                case (int)Globals.Symbol.Char:
                    e.SetCharacter();
                    break;
                case (int)Globals.Symbol.Class:
                    e.SetClass();
                    break;
            }

            isInFunction = false;
        }

        /* set the passing mode (if specified) */
        switch ((int)pMode)
        {
            case (int)Globals.Symbol.Ref:
                e.Mode = Element.PassingMode.Reference;
                break;
            case (int)Globals.Symbol.Out:
                e.Mode = Element.PassingMode.Output;
                break;
        }

        /* stay in touch with your folks! */
        e.Parent = parent;

        if (token != Globals.Symbol.Const)
        {
            /* update the local variable size for classes */
            if (parent != null)
                if (parent.GetEType() == Element.EntryType.ClassType)
                {
                    classSize += mysize;
                    if (currentObject.GetEType() == Element.EntryType.MethodType)
                        currentMethod = currentObject;
                }
                else if (parent.GetEType() == Element.EntryType.MethodType)
                {
                    if (!isInParameterList)
                        parent.SetSizeOfLocals(parent.GetSizeOfLocals() + mysize);
                }

            /* update the offset */
            e.SetOffset(localOffset);
            localOffset += mysize * offsetmul;

            /* update minOffset */
            if (e.GetOffset() < minOffset)
                minOffset = e.GetOffset();
        }
        else
        {
            e.SetOffset(0);
        }

        /* set the parameter location (or 0) */
        if (!isInParameterList)
        {
            varLoc = 0;
        }
        else
        {
            varLoc++;
            if (parent != null)
                parent.SetNumParams(varLoc);
        }

        /* set the parameter number (or 0) */
        e.Location = varLoc;

        /* make sure isFunc is false so we don't consider a variable inside a method to be a method */
        isInFunction = false;

        return e;
    }

    /// Implements the grammar rule: AccessModifier -> [public] | [static] | lambda
    private void AccessModifier(out Globals.Symbol access)
    {
        access = Globals.Symbol.Private;

        if (Globals.Token == Globals.Symbol.Public)
        {
            Match(Globals.Symbol.Public);
            access = Globals.Token;
        }
        else if (Globals.Token == Globals.Symbol.Static)
        {
            Match(Globals.Symbol.Static);
            access = Globals.Token;
        }
    }

    /// Implements the grammar rule: Addop -> [+] | [-] | [&&]
    private void Addop()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Addop:
                Match(Globals.Symbol.Addop);
                break;
            case (int)Globals.Symbol.Signop:
                Match(Globals.Symbol.Signop);
                break;
            case (int)Globals.Symbol.Orop:
                Match(Globals.Symbol.Orop);
                break;
            default:
                Error(Globals.CurLine, "+, -, or ||", Globals.Lexeme);
                break;
        }
    }

    /// Implements the grammar rule: AssignStat -> idt [=] Expr | idt [=] MethodCall | MethodCall
    private void AssignStat()
    {
        Element getMethod = null;
        var standAlone = true;
        var printAssignmentVar = string.Empty;
        var possibleReturnVariable = Globals.Lexeme;

        /* make sure the variable is defined */
        var e = Globals.SymbolTable.Lookup(Globals.Lexeme);
        if (e == null)
        {
            Warning($"warning: {Globals.Filename}:{Globals.CurLine}: undeclared variable \"{Globals.Lexeme}\"");
            e = AddSymbol(Globals.Symbol.Unknown, Globals.Symbol.Unknown, Globals.Symbol.Unknown,
                Globals.Symbol.Unknown, Globals.Lexeme, Globals.Depth);
        }

        Match(Globals.Symbol.Identifier);

        if (Globals.Token == Globals.Symbol.Assignop)
        {
            standAlone = false;
            Match(Globals.Symbol.Assignop);
        }

        if (Globals.Token == Globals.Symbol.Period)
        {
            AssignTail(ref getMethod, e, string.Empty, standAlone, out printAssignmentVar);
        }
        else if (Globals.Token == Globals.Symbol.Identifier)
        {
            AssignTail(ref getMethod, null, possibleReturnVariable, standAlone, out printAssignmentVar);
        }
        else
        {
            string rhside;
            Expr("", out rhside);

            if (rhside != string.Empty)
            {
                if (!TypeHelper.IsNumeric(rhside))
                {
                    var rh = Globals.SymbolTable.Lookup(rhside);

                    if (rh.GetEType() == Element.EntryType.ConstType)
                        Emit($"  {e.GetOffsetName()} = {rh.GetIntegerValue()}\n");
                    else
                        Emit($"  {e.GetOffsetName()} = {rh.GetOffsetName()}\n");
                }
                else
                {
                    Emit($"  {e.GetOffsetName()} = {rhside}\n");
                }
            }
        }

        if (printAssignmentVar != string.Empty)
        {
            if (printAssignmentVar.Length > 2)
            {
                if (printAssignmentVar[..3] != "_BP")
                {
                    var pav = Globals.SymbolTable.Lookup(printAssignmentVar);
                    Emit($"  {e.GetOffsetName()} = {pav.GetOffsetName()}\n");
                }
            }
            else
            {
                var pav = Globals.SymbolTable.Lookup(printAssignmentVar);
                Emit($"  {e.GetOffsetName()} = {pav.GetOffsetName()}\n");
            }
        }
    }

    /// Implements the grammar rule: AssignTail -> idt . MethodCall | idt ShortExpr
    private void AssignTail(ref Element getMethod, Element? parent, string retLoc, bool standAloneCall,
        out string rhSide)
    {
        if (standAloneCall)
            Match(Globals.Symbol.Period);

        /* grab the next variable and make sure it's valid */
        var e = Globals.SymbolTable.Lookup(Globals.Lexeme);
        if (e == null)
        {
            Warning($"warning: {Globals.Filename}:{Globals.CurLine}: undeclared variable \"{Globals.Lexeme}\"");
            e = AddSymbol(Globals.Symbol.Unknown, Globals.Symbol.Unknown, Globals.Symbol.Unknown,
                Globals.Symbol.Unknown, Globals.Lexeme, Globals.Depth);
        }

        var prnt = Globals.Lexeme;
        Match(Globals.Symbol.Identifier);

        if (Globals.Token == Globals.Symbol.Period || standAloneCall)
        {
            if (parent == null)
                parent = Globals.SymbolTable.Lookup(prnt);

            string nsaMethod;
            rhSide = string.Empty;
            getMethod = e;
            MethodCall(standAloneCall, out nsaMethod);

            if (parent == null)
            {
                if (nsaMethod == string.Empty)
                    Emit($"\n  CALL {e.GetName()}\n");
                else
                    Emit($"\n  CALL {nsaMethod}\n");
            }
            else
            {
                if (nsaMethod == string.Empty)
                    Emit($"\n  CALL {parent.GetName()}.{e.GetName()}\n");
                else
                    Emit($"\n  CALL {parent.GetName()}.{nsaMethod}\n");
            }

            if (retLoc.Length > 0)
            {
                var r = Globals.SymbolTable.Lookup(retLoc);
                Emit($"  {r.GetOffsetName()} = _AX\n");
            }
        }
        else
        {
            string rightLexeme;
            ShortExpr(e.GetName(), out rightLexeme);

            if (retLoc.Length > 0)
            {
                rhSide = string.Empty;
                var l = Globals.SymbolTable.Lookup(retLoc);
                var r = Globals.SymbolTable.Lookup(rightLexeme);

                if (r.GetEType() == Element.EntryType.ConstType)
                    Emit($"  {l.GetOffsetName()} = {r.GetIntegerValue()}\n");
                else
                    Emit($"  {l.GetOffsetName()} = {r.GetOffsetName()}\n");
            }
            else
            {
                rhSide = rightLexeme;
            }
        }
    }


    /// Implements the grammar rule: BaseClass -> [:] ClassOrNamespace | lambda
    private void BaseClass()
    {
        if (Globals.Token == Globals.Symbol.Colon)
        {
            Match(Globals.Symbol.Colon);
            ClassOrNamespace();
        }
    }

    /// Implements the grammar rule: Classes -> ClassesDecl Classes | lambda
    private void Classes()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Public:
            case (int)Globals.Symbol.Static:
            case (int)Globals.Symbol.Class:
                ClassesDecl();
                Classes();
                break;
        }
    }

    /// Implements the grammar rule: ClassesDecl -> AccessModifier [class] [idt] BaseClass [{] Composite [}]
    private void ClassesDecl()
    {
        Globals.Symbol accMod, type = Globals.Symbol.Class;
        var dep = Globals.Depth;
        classSize = 0;

        AccessModifier(out accMod);
        Match(Globals.Symbol.Class);

        /* add the class to the symbol table */
        var c = AddSymbol(Globals.Symbol.Unknown, accMod, type, Globals.Token, Globals.Lexeme, Globals.Depth);

        /* match the identifier and get the next token+lexeme */
        Match(Globals.Symbol.Identifier);

        BaseClass();
        Match(Globals.Symbol.Lbrace);

        /* find out which path we need to go on */
        Composite();

        Match(Globals.Symbol.Rbrace);

        /* set the class's size */
        c.SetSizeOfLocals(classSize);
    }

    /// Implements the grammar rule: ClassOrNamespace -> [idt] ClassOrNamespaceTail
    private void ClassOrNamespace()
    {
        Match(Globals.Symbol.Identifier);
        ClassOrNamespaceTail();
    }

    /// Implements the grammar rule: ClassOrNamespaceTail -> [.] ClassOrNamespace | lambda
    private void ClassOrNamespaceTail()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Period:
                Match(Globals.Symbol.Period);
                ClassOrNamespace();
                break;
        }
    }

    /// Implements the grammar rule: Composite -> AccessModifier Type MainIDT CompositeTail Composite | Type MainIDT CompositeTail Composite | lambda
    private void Composite()
    {
        Globals.Symbol accMod, type = Globals.Symbol.Unknown;
        string idt;

        localOffset = 4;
        offsetmul = 1;

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Public:
            case (int)Globals.Symbol.Static:
                AccessModifier(out accMod);

                /* Assignment 5 (initial semantic processing) was horribly

      broken because of this line--Type is non-nullable but isn't

      needed for the constructor.  Simply making it nullable would

      open up errors on other lines where a type *is* needed.


      Instead of doing that, check to see if the current lexeme

      (Globals.lexeme) is the same as the previous one that was

      added to the symbol table (current.lexeme).

     */
                    if (currentClass.GetName() == Globals.Lexeme)
                    {
                        type = Globals.Symbol.Constructor;
                        ConstructorDecl(accMod);
                    }
                    else
                    {
                        Type(out type);
                        MainIdt(accMod, type, out idt);
                        CompositeTail(idt);
                    }
                    Composite();
                    break;
                case (int)Globals.Symbol.Int:
                case (int)Globals.Symbol.Float:
                case (int)Globals.Symbol.Char:
                case (int)Globals.Symbol.Void:
                    Type(out type);
                    MainIdt(Globals.Symbol.Public, type, out idt);
                    CompositeTail(idt);
                    Composite();
                    break;
                case (int)Globals.Symbol.Const:
                    IdentifierDecl();
                    break;
                default:
                    break;
            }
        }

    ///
    /// Implements the grammar rule: CompositeTail -> [return] [;] [}] | [(] ParamList [)] [{] IdentifierList StatList ReturnLine [}] | [;]
    ///
    void CompositeTail(string idt)
    {
            switch ((int)Globals.Token)
            {
                case (int)Globals.Symbol.Return:
                    Match(Globals.Symbol.Return);
                    Match(Globals.Symbol.Semicolon);
                    Match(Globals.Symbol.Rbrace);
                    break;
                case (int)Globals.Symbol.Lparen:
                    var m = Globals.SymbolTable.Lookup(idt);
                    Emit("PROC " + m.Parent.GetName() + "." + m.GetName() + "\n");

                    Match(Globals.Symbol.Lparen);
                    localOffset = 4;
                    offsetmul = 1;
                    ParamList();
                    Match(Globals.Symbol.Rparen);
                    Match(Globals.Symbol.Lbrace);
                    localOffset = -2;
                    offsetmul = -1;
                    isInFunction = false;	/* off to bigger and better identifiers! */
                IdentifierList();
                StatList();
                ReturnLine(nowMain);
                Match(Globals.Symbol.Rbrace);

                Emit($"ENDP {m.Parent.GetName()}.{m.GetName()}\n");
                break;
            case (int)Globals.Symbol.Semicolon:
                Match(Globals.Symbol.Semicolon);
                break;
        }
    }

    /// Implements the grammar rule: ConstAssign -> idt [=] numt
    private Element? ConstAssign(Globals.Symbol accMod, Globals.Symbol type)
    {
        var e = AddSymbol(Globals.Symbol.Unknown, accMod, type, Globals.Symbol.Const, Globals.Lexeme, Globals.Depth);
        e.SetConstant();

        switch ((int)type)
        {
            case (int)Globals.Symbol.Char:
                e.Type = Element.VariableType.Char;
                break;
            case (int)Globals.Symbol.Int:
                e.Type = Element.VariableType.Int32;
                break;
            case (int)Globals.Symbol.Float:
                e.Type = Element.VariableType.Float;
                break;
            default:
                e.Type = Element.VariableType.Empty;
                break;
        }

        Match(Globals.Symbol.Identifier);
        Match(Globals.Symbol.Assignop);

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Number:
                e.SetValue(Globals.Value);
                Match(Globals.Symbol.Number);
                break;
            case (int)Globals.Symbol.Numfloat:
                e.SetValue((float)Globals.ValueF);
                Match(Globals.Symbol.Numfloat);
                break;
            default:
                Error(Globals.CurLine, "a numeric value", Globals.Lexeme);
                break;
        }

        /* mark the element as constant */
        e.SetConstant();
        return e;
    }

    /// Implements the grammar rule: ConstructorDecl -> AccessModifier [idt] [(] ParamList [)] [{] IdentifierList StatList [}]
    private void ConstructorDecl(Globals.Symbol accMod)
    {
        var type = Globals.Symbol.Constructor;

        /* add the constructor */
        var c = AddSymbol(Globals.Symbol.Unknown, accMod, type, Globals.Token, Globals.Lexeme, Globals.Depth);

        Emit($"PROC {c.Parent.GetName()}.{c.GetName()}\n");
        Match(Globals.Symbol.Identifier);
        Match(Globals.Symbol.Lparen);
        localOffset = 4;
        offsetmul = 1;
        ParamList();
        Match(Globals.Symbol.Rparen);
        Match(Globals.Symbol.Lbrace);
        localOffset = -2;
        offsetmul = -1;
        IdentifierList();
        StatList();

        /* set the constructor's size */
        c.SetMethod(accMod);
        c.SetSizeOfLocals(Globals.SymbolTable.GetDepthSize(Globals.Depth));
        classSize += c.GetSizeOfLocals();

        Match(Globals.Symbol.Rbrace);
        Emit($"ENDP {c.Parent.GetName()}.{c.GetName()}\n");
    }

    /// Implements the grammar rule: Expr -> Relation | lambda
    private void Expr(string left, out string right)
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Identifier:
            case (int)Globals.Symbol.Numfloat:
            case (int)Globals.Symbol.Number:
            case (int)Globals.Symbol.Lparen:
            case (int)Globals.Symbol.Unarynot:
            case (int)Globals.Symbol.Signop:
                SimpleExpr(out right);
                break;
            default:
                right = string.Empty;
                break;
        }
    }

    /// Implements the grammar rule: Factor -> idt | numt | [(] Expr [)] | [!] Factor | signop Factor
    private void Factor(out string mylex)
    {
        string fname;
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Identifier:
                /* make sure the variable is defined */
                var e = Globals.SymbolTable.Lookup(Globals.Lexeme);
                if (e == null)
                    Error($"error: {Globals.Filename}:{Globals.CurLine}: undeclared variable \"{Globals.Lexeme}\"");

                mylex = Globals.Lexeme;
                Match(Globals.Symbol.Identifier);
                break;
            case (int)Globals.Symbol.Numfloat:
            case (int)Globals.Symbol.Number:
                mylex = Globals.Lexeme;
                Match(Globals.Token);
                break;
            case (int)Globals.Symbol.Lparen:
                /* store the offsets, matching lparen resets them */
                int lo = localOffset, om = offsetmul;
                Match(Globals.Symbol.Lparen);

                /* resetore the offsets */
                localOffset = lo;
                offsetmul = om;

                isInExpression = true;
                Expr("", out mylex);
                isInExpression = false;
                Match(Globals.Symbol.Rparen);
                break;
            case (int)Globals.Symbol.Unarynot:
                Match(Globals.Symbol.Unarynot);
                Factor(out fname);
                mylex = $"!{fname}";
                break;
            case (int)Globals.Symbol.Signop:
                Signop();
                Factor(out fname);

                if (TypeHelper.IsNumeric(fname))
                {
                    mylex = $"-{fname}";
                }
                else
                {
                    var f = Globals.SymbolTable.Lookup(fname);
                    var n = Newtemp();

                    if (f.GetEType() == Element.EntryType.ConstType)
                        Emit($"  {n.GetOffsetName()} = {f.GetIntegerValue()}\n");
                    else
                        Emit($"  {n.GetOffsetName()} = -{f.GetOffsetName()}\n");

                    mylex = n.GetName();
                }

                break;
            default:
                Error(Globals.CurLine, "identifier, number, (, !, or -", Globals.Tokens[(int)Globals.Token]);
                mylex = string.Empty;
                break;
        }
    }

    /// Implements the grammar rule: IdentifierDecl -> AccessModifier Type IDT [;] | Type IDT [;] | lambda
    private void IdentifierDecl()
    {
        /* variables to hold variable information */
        var accMod = Globals.Symbol.Unknown;
        var type = Globals.Symbol.Unknown;

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Public:
            case (int)Globals.Symbol.Static:
            case (int)Globals.Symbol.Private:
                AccessModifier(out accMod);
                break;
            case (int)Globals.Symbol.Int:
            case (int)Globals.Symbol.Float:
            case (int)Globals.Symbol.Char:
            case (int)Globals.Symbol.Void:
            case (int)Globals.Symbol.Const:
                break;
        }

        /* ...and keep running the grammar rules */
        if (Globals.Token == Globals.Symbol.Const)
        {
            Match(Globals.Symbol.Const);
            Type(out type);
            var c = IdtConst(accMod, type);
            Match(Globals.Symbol.Semicolon);
            c.SetConstant();
            IdentifierList();
        }
        else
        {
            if (currentClass.GetName() == Globals.Lexeme)
            {
                type = Globals.Symbol.Constructor;
                ConstructorDecl(accMod);
            }
            else
            {
                Type(out type);
                Idt(accMod, type);
                var e = currentObject;

                if (Globals.Token == Globals.Symbol.Lparen)
                {
                    Emit($"PROC {e.Parent.GetName()}.{e.GetName()}\n");
                    Match(Globals.Symbol.Lparen);
                    localOffset = 4;
                    offsetmul = 1;
                    ParamList();
                    Match(Globals.Symbol.Rparen);
                    Match(Globals.Symbol.Lbrace);
                    localOffset = 2;
                    offsetmul = -1;
                    IdentifierList();
                    StatList();
                    ReturnLine(nowMain);
                    Match(Globals.Symbol.Rbrace);
                    Emit($"ENDP {e.Parent.GetName()}.{e.GetName()}\n");
                }
                else
                {
                    Match(Globals.Symbol.Semicolon);
                }
            }
        }
    }

    /// Implements the grammar rule: IdentifierList -> IdentifierDecl IdentifierList | lambda
    private void IdentifierList()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Int:
            case (int)Globals.Symbol.Float:
            case (int)Globals.Symbol.Char:
            case (int)Globals.Symbol.Void:
            case (int)Globals.Symbol.Public:
            case (int)Globals.Symbol.Static:
            case (int)Globals.Symbol.Const:
                IdentifierDecl();
                IdentifierList();
                break;
        }
    }

    /// Implements the grammar rule: Id_List -> [idt] Id_List_Tail
    private void Id_List()
    {
        /* make sure that the variable exists */
        var r = Globals.SymbolTable.Lookup(Globals.Lexeme);
        if (r == null)
            Error($"error: {Globals.Filename}:{Globals.CurLine}: attempt to read undeclared variable {Globals.Lexeme}");

        Emit($"  RDI {r.GetOffsetName()}\n");
        Match(Globals.Symbol.Identifier);
        Id_List_Tail();
    }

    /// Implements the grammar rule: Id_List_Tail -> [,] [idt] Id_List_Tail | lambda
    private void Id_List_Tail()
    {
        if (Globals.Token == Globals.Symbol.Comma)
        {
            Match(Globals.Symbol.Comma);

            /* make sure that the variable exists */
            var r = Globals.SymbolTable.Lookup(Globals.Lexeme);
            if (r == null)
                Error(
                    $"error: {Globals.Filename}:{Globals.CurLine}: attempt to read undeclared variable {Globals.Lexeme}");

            Emit($"  RDI {r.GetOffsetName()}\n");
            Match(Globals.Symbol.Identifier);
            Id_List_Tail();
        }
    }

    /// Implements the grammar rule: IDT -> [idt] | [,] [idt] IDT | lambda
    private void Idt(Globals.Symbol access, Globals.Symbol type)
    {
        Element? e = null;

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Identifier:
                e = AddSymbol(Globals.Symbol.Unknown, access, type, Globals.Token, Globals.Lexeme, Globals.Depth);

                /* if it's not a class but has a size of 0, reset the size -- hit a bug somewhere */
                if (e.GetSizeOfLocals() == 0 &&
                    !(e.GetEType() == Element.EntryType.ClassType || e.GetEType() == Element.EntryType.MethodType))
                {
                    switch ((int)e.GetVType())
                    {
                        case (int)Element.VariableType.Char:
                            e.SetSizeOfLocals(e.GetSizeOfLocals() + 1);
                            break;
                        case (int)Element.VariableType.Int32:
                            e.SetSizeOfLocals(e.GetSizeOfLocals() + 2);
                            break;
                        case (int)Element.VariableType.Float:
                            e.SetSizeOfLocals(e.GetSizeOfLocals() + 4);
                            break;
                    }

                    functionLocalsSize += e.GetSizeOfLocals();
                }

                Match(Globals.Symbol.Identifier);

                /* cheat and see if we have a method coming */
                if (Globals.Token == Globals.Symbol.Lparen)
                {
                    isInFunction = true;
                    e.SetMethod(type);
                    e.SetSizeOfLocals(0);
                    currentMethod = e;
                }
                else
                {
                    Idt(access, type);
                }

                break;
            case (int)Globals.Symbol.Comma:
                Match(Globals.Symbol.Comma);
                Idt(access, type);
                break;
        }
    }

    /// Implements the grammar rule: IDTConst -> ConstAssign | IDTConst [,] ConstAssign
    private Element? IdtConst(Globals.Symbol accMod, Globals.Symbol type)
    {
        Element? c = null;

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Identifier:
                c = ConstAssign(accMod, type);
                IdtConst(accMod, type);
                break;
            case (int)Globals.Symbol.Comma:
                Match(Globals.Symbol.Comma);
                IdtConst(accMod, type);
                break;
        }

        if (c != null)
            c.SetConstant();
        return c;
    }

    /// Implements the grammar rule: In_Stat -> [read] [(] Id_List [)]
    private void In_Stat()
    {
        Match(Globals.Symbol.Read);
        Match(Globals.Symbol.Lparen);
        Id_List();
        Match(Globals.Symbol.Rparen);
    }

    /// Implements the grammar rule: IOStat -> In_Stat | Out_Stat
    private void IoStat()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Read:
                In_Stat();
                break;
            case (int)Globals.Symbol.Write:
            case (int)Globals.Symbol.Writeln:
                Out_Stat();
                break;
            default:
                Error(Globals.CurLine, "'read', 'write', or 'writeln'", Globals.Lexeme);
                break;
        }
    }

    /// Implements the grammar rule: MainIDT -> [idt] MainIDT | [main] [(] [)] [{] IdentifierList StatList | [,] [idt] MainIDT | lambda
    private void MainIdt(Globals.Symbol accMod, Globals.Symbol type, out string idt)
    {
        idt = string.Empty;
        /* take care of the constructor */
        if (type == Globals.Symbol.Constructor)
        {
            Idt(accMod, type);
            MainIdt(accMod, Globals.Symbol.Unknown, out idt);
        }

        if (Globals.Token == Globals.Symbol.Identifier && Globals.Lexeme != "Main")
        {
            string recurIdt;
            idt = Globals.Lexeme;
            Idt(accMod, type);
            MainIdt(accMod, type, out recurIdt);
        }
        else if (Globals.Lexeme == "Main")
        {
            Emit($"PROC {currentClass.GetName()}.Main\n");

            var main = AddSymbol(Globals.Symbol.Unknown, Globals.Symbol.Unknown, Globals.Symbol.Void,
                Globals.Symbol.Void, "Main", Globals.Depth);
            main.Parent = currentClass;

            Match("Main");
            foundMain = true; /* we found it! */
            nowMain = true; /* hello! */

            /* save the TAC START line */
            tacStartInstruction = $"START {currentClass.GetName()}.Main";

            Match(Globals.Symbol.Lparen);
            Match(Globals.Symbol.Rparen);
            Match(Globals.Symbol.Lbrace);
            localOffset = -2;
            offsetmul = -1;
            IdentifierList();
            StatList();
            nowMain = false; /* we're done with Main() */
            Emit($"ENDP {currentClass.GetName()}.Main\n");
        }
        else if (Globals.Token == Globals.Symbol.Comma)
        {
            Match(Globals.Symbol.Comma);
            Match(Globals.Symbol.Identifier);
            MainIdt(accMod, type, out idt);
        }
    }

    /// Implements the grammar rule: MethodCall -> idt ( Params )
    private void MethodCall(bool standAloneCall, out string methodName)
    {
        var pushStats = string.Empty;

        if (standAloneCall)
        {
            methodName = string.Empty;
            Match(Globals.Symbol.Lparen);
            Params(ref pushStats);
        }
        else
        {
            Match(Globals.Symbol.Period);
            methodName = Globals.Lexeme;
            Match(Globals.Symbol.Identifier);
            Match(Globals.Symbol.Lparen);
            Params(ref pushStats);
        }

        /* now print out the 'push' statements */
        Emit(pushStats);

        Match(Globals.Symbol.Rparen);
    }

    /// Implements the grammar rule: Mode -> [ref] | [out] | lambda
    private void Mode(ref Globals.Symbol mode)
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Ref:
                Match(Globals.Symbol.Ref);
                mode = Globals.Symbol.Ref;
                break;
            case (int)Globals.Symbol.Out:
                Match(Globals.Symbol.Out);
                mode = Globals.Symbol.Out;
                break;
        }
    }

    /// Implements the grammar rule: MoreFactor -> Mulop Factor MoreFactor | lambda
    private void MoreFactor(string leftLexeme, out string rightLexeme, string oldmulchar, out string mulchar,
        bool firstRound)
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Lparen:
                mulchar = string.Empty;
                Match(Globals.Symbol.Lparen);
                Expr("", out rightLexeme);
                Match(Globals.Symbol.Rparen);
                break;
            case (int)Globals.Symbol.Mulop:
            case (int)Globals.Symbol.Andop:
                Element? rLex;
                mulchar = Globals.Lexeme;
                oldmulchar = mulchar;
                string factLexeme, lvar, rvar, recurLexeme, newmulchar;

                Mulop();
                Factor(out factLexeme);
                MoreFactor(factLexeme, out recurLexeme, oldmulchar, out newmulchar, false);

                /* look up the left and right values */
                var lLex = Globals.SymbolTable.Lookup(leftLexeme);
                if (recurLexeme == string.Empty)
                    rLex = Globals.SymbolTable.Lookup(factLexeme);
                else
                    rLex = Globals.SymbolTable.Lookup(recurLexeme);

                /* ...and get string values for them */
                if (lLex != null)
                {
                    if (lLex.GetEType() == Element.EntryType.ConstType)
                        lvar = lLex.GetIntegerValue().ToString();
                    else
                        lvar = lLex.GetOffsetName();
                }
                else if (TypeHelper.IsNumeric(leftLexeme))
                {
                    lvar = leftLexeme;
                }
                else
                {
                    lvar = string.Empty;
                }

                if (rLex != null)
                {
                    if (rLex.GetEType() == Element.EntryType.ConstType)
                        rvar = rLex.GetIntegerValue().ToString();
                    else
                        rvar = rLex.GetOffsetName();
                }
                else if (TypeHelper.IsNumeric(factLexeme))
                {
                    rvar = factLexeme;
                }
                else
                {
                    rvar = string.Empty;
                }

                var t = Newtemp();

                Emit($"  {t.GetOffsetName()} = {lvar} {mulchar} {rvar}\n");
                rightLexeme = t.GetName();

                break;

            default:
                mulchar = oldmulchar;
                rightLexeme = leftLexeme;
                break;
        }
    }

    /// Implements the grammar rule: MoreTerm -> Addop Term MoreTerm | lambda
    private void MoreTerm(string leftLexeme, out string rightLexeme, string oldaddchar, out string addchar,
        bool firstRound)
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Addop:
            case (int)Globals.Symbol.Signop:
            case (int)Globals.Symbol.Orop:
                Element? rLex;
                addchar = Globals.Lexeme;
                oldaddchar = addchar;
                string termLexeme, lvar, rvar, recurLexeme, newaddchar;

                Addop();
                Term(out termLexeme);
                MoreTerm(termLexeme, out recurLexeme, oldaddchar, out newaddchar, false);

                /* look up the left and right values */
                var lLex = Globals.SymbolTable.Lookup(leftLexeme);
                if (recurLexeme == string.Empty)
                    rLex = Globals.SymbolTable.Lookup(termLexeme);
                else
                    rLex = Globals.SymbolTable.Lookup(recurLexeme);

                /* ...and get string values for them */
                if (lLex != null)
                {
                    if (lLex.GetEType() == Element.EntryType.ConstType)
                        lvar = lLex.GetIntegerValue().ToString();
                    else
                        lvar = lLex.GetOffsetName();
                }
                else if (TypeHelper.IsNumeric(leftLexeme))
                {
                    lvar = leftLexeme;
                }
                else
                {
                    lvar = string.Empty;
                }

                if (rLex != null)
                {
                    if (rLex.GetEType() == Element.EntryType.ConstType)
                        rvar = rLex.GetIntegerValue().ToString();
                    else
                        rvar = rLex.GetOffsetName();
                }
                else if (TypeHelper.IsNumeric(termLexeme))
                {
                    rvar = termLexeme;
                }
                else
                {
                    rvar = string.Empty;
                }

                var t = Newtemp();
                Emit($"  {t.GetOffsetName()} = {lvar} {addchar} {rvar}\n");
                rightLexeme = t.GetName();

                break;
            case (int)Globals.Symbol.Rparen:
            case (int)Globals.Symbol.Semicolon:
                rightLexeme = leftLexeme;
                addchar = string.Empty;
                break;
            default:
                rightLexeme = string.Empty;
                if (isInExpression)
                    rightLexeme = leftLexeme;
                addchar = string.Empty;
                break;
        }
    }

    /// Implements the grammar rule: Mulop -> [*] | [/] | [||]
    private void Mulop()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Mulop:
                Match(Globals.Symbol.Mulop);
                break;
            case (int)Globals.Symbol.Andop:
                Match(Globals.Symbol.Andop);
                break;
            default:
                Error(Globals.CurLine, "*, /, or &&", Globals.Lexeme);
                break;
        }
    }

    /// Implements the grammar rule: NamespaceBlock -> NamespaceBlockDecl | Classes
    private void NamespaceBlock()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Namespace:
                NamespaceBlockDecl();
                break;
            default:
                Classes();
                break;
        }
    }

    /// Implements the grammar rule: NamespaceBlockDecl -> [namespace] ClassOrNamespace [{] Classes [}]
    private void NamespaceBlockDecl()
    {
        Match(Globals.Symbol.Namespace);
        ClassOrNamespace();

        /* don't forget to add the namespace to the symbol table to give classes a parent*/
        AddSymbol(Globals.Symbol.Unknown, Globals.Symbol.Public, Globals.Symbol.Namespace, Globals.Symbol.Namespace,
            Globals.Lexeme, Globals.Depth);

        Match(Globals.Symbol.Lbrace);
        Classes();
        Match(Globals.Symbol.Rbrace);
    }

    /// Implements the grammar rule: Out_Stat -> [write] [(] Write_List [)] | [writeln] [(] Write_list [)]
    private void Out_Stat()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Write:
                Match(Globals.Symbol.Write);
                Match(Globals.Symbol.Lparen);
                WriteList();
                Match(Globals.Symbol.Rparen);
                break;
            case (int)Globals.Symbol.Writeln:
                Match(Globals.Symbol.Writeln);
                Match(Globals.Symbol.Lparen);
                WriteList();
                Match(Globals.Symbol.Rparen);
                Emit("  WRLN\n");
                break;
            default:
                Error(Globals.CurLine, "'write' or 'writeln'", Globals.Lexeme);
                break;
        }
    }

    /// Implements the grammar rule: ParamList -> Mode Type [idt] ParamTail | [return] Expr [;] | lambda
    private void ParamList()
    {
        Globals.Symbol mode = Globals.Symbol.Unknown, type;
        Element? e = null;

        /* start updating varLoc */
        isInParameterList = true;

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Ref:
            case (int)Globals.Symbol.Out:
                Mode(ref mode);
                Type(out type);
                e = AddSymbol(mode, Globals.Symbol.Private, type, Globals.Token, Globals.Lexeme, Globals.Depth);

                /* play with our new symbol */
                if (e != null)
                {
                    var mysize = 0;
                    var cont = false;

                    switch ((int)e.GetVType())
                    {
                        case (int)Element.VariableType.Int32:
                            e.SetInteger();
                            mysize = 2;
                            cont = true;
                            break;
                        case (int)Element.VariableType.Float:
                            e.SetFloat();
                            mysize = 4;
                            cont = true;
                            break;
                        case (int)Element.VariableType.Char:
                            e.SetCharacter();
                            mysize = 1;
                            cont = true;
                            break;
                    }

                    /* if we hit a real variable, set properties */
                    if (cont)
                    {
                        e.SetSizeOfLocals(mysize);
                        e.SetOffset(localOffset * offsetmul - mysize * offsetmul);
                        e.Parent = parent;

                        functionLocalsSize += mysize;
                        localOffset += mysize * offsetmul;
                    }
                }

                Match(Globals.Symbol.Identifier);
                ParamTail();
                break;
            case (int)Globals.Symbol.Int:
            case (int)Globals.Symbol.Float:
            case (int)Globals.Symbol.Char:
            case (int)Globals.Symbol.Void:
                Type(out type);
                e = AddSymbol(mode, Globals.Symbol.Private, type, Globals.Token, Globals.Lexeme, Globals.Depth);

                /* play with our new symbol */
                if (e != null)
                {
                    var mysize = 0;
                    var cont = false;

                    switch ((int)e.GetVType())
                    {
                        case (int)Element.VariableType.Int32:
                            e.SetInteger();
                            mysize = 2;
                            cont = true;
                            break;
                        case (int)Element.VariableType.Float:
                            e.SetFloat();
                            mysize = 4;
                            cont = true;
                            break;
                        case (int)Element.VariableType.Char:
                            e.SetCharacter();
                            mysize = 1;
                            cont = true;
                            break;
                    }

                    /* if we hit a real variable, set properties */
                    if (cont)
                    {
                        // we don't want to count parameters towards locals
                        //e.SetSizeOfLocals(mysize);

                        e.Parent = parent;

                        if (e.Parent.GetName() == currentClass.GetName())
                            e.SetOffset(localOffset * offsetmul - mysize * offsetmul);
                        else
                            e.SetOffset(localOffset * offsetmul);

                        functionLocalsSize += mysize;
                        if (e.Parent.GetName() != currentClass.GetName())
                            localOffset += mysize * offsetmul;

                        if (e.Parent.GetEType() == Element.EntryType.MethodType)
                            e.Parent.SetSizeOfParams(e.Parent.GetSizeOfParams() + mysize);
                    }
                }

                Match(Globals.Symbol.Identifier);
                ParamTail();
                break;
            case (int)Globals.Symbol.Return:
                Match(Globals.Symbol.Return);
                string a = string.Empty, b;
                Expr(a, out b);
                Match(Globals.Symbol.Semicolon);
                break;
        }

        /* done! this should already by unset */
        isInParameterList = false;
    }

    /// Implements the grammar rule: ParamTail -> [,] Mode Type [idt] ParamTail | lambda
    private void ParamTail()
    {
        Globals.Symbol mode = Globals.Symbol.Private, type;

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Comma:
                Match(Globals.Symbol.Comma);
                Mode(ref mode);
                Type(out type);
                var e = AddSymbol(mode, Globals.Symbol.Private, type, Globals.Token, Globals.Lexeme, Globals.Depth);

                /* play with our new symbol */
                if (e != null)
                {
                    var mysize = 0;
                    var cont = false;

                    switch ((int)e.GetVType())
                    {
                        case (int)Element.VariableType.Int32:
                            e.SetInteger();
                            mysize = 2;
                            cont = true;
                            break;
                        case (int)Element.VariableType.Float:
                            e.SetFloat();
                            mysize = 4;
                            cont = true;
                            break;
                        case (int)Element.VariableType.Char:
                            e.SetCharacter();
                            mysize = 1;
                            cont = true;
                            break;
                    }

                    /* if we hit a real variable, set properties */
                    if (cont)
                    {
                        if (!isInParameterList)
                            e.SetSizeOfLocals(mysize);
                        e.Parent = parent;

                        if (e.Parent.GetName() == currentClass.GetName())
                            e.SetOffset(localOffset * offsetmul - mysize * offsetmul);
                        else
                            e.SetOffset(localOffset * offsetmul);

                        functionLocalsSize += mysize;
                        if (e.Parent.GetName() != currentClass.GetName())
                            localOffset += mysize * offsetmul;

                        if (e.Parent.GetEType() == Element.EntryType.MethodType)
                            e.Parent.SetSizeOfParams(e.Parent.GetSizeOfParams() + mysize);
                    }
                }

                Match(Globals.Symbol.Identifier);
                ParamTail();
                break;
        }
    }

    /// Implements the grammar rule: Params -> [idt] ParamsTail | [num] ParamsTails | lambda
    private void Params(ref string pushStats)
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Identifier:
                /* make sure it's a valid symbol */
                var e = Globals.SymbolTable.Lookup(Globals.Lexeme);
                if (e == null)
                    Error($"error: {Globals.Filename}:{Globals.CurLine}: undeclared variable \"{Globals.Lexeme}\"");

                /* prepend the new symbol to our list */
                if (pushStats.Length > 0)
                    pushStats = $"  PUSH {e.GetOffsetName()}\n{pushStats}";
                else
                    pushStats = $"  PUSH {e.GetOffsetName()}";

                Match(Globals.Symbol.Identifier);
                ParamsTail(ref pushStats);
                break;
            case (int)Globals.Symbol.Number:
            case (int)Globals.Symbol.Numfloat:
                /* prepend the new symbol to our list */
                if (pushStats.Length > 0)
                    pushStats = $"  PUSH {Globals.Lexeme}\n{pushStats}";
                else
                    pushStats = $"  PUSH {Globals.Lexeme}";

                Match(Globals.Token);
                ParamsTail(ref pushStats);
                break;
        }
    }

    /// Implements the grammar rule: ParamsTail -> [,] [idt] ParamsTail | [,] [num] ParamsTail | lambda
    private void ParamsTail(ref string pushStats)
    {
        if (Globals.Token == Globals.Symbol.Comma)
        {
            Match(Globals.Symbol.Comma);
            switch ((int)Globals.Token)
            {
                case (int)Globals.Symbol.Identifier:
                    /* make sure it's a valid symbol */
                    var e = Globals.SymbolTable.Lookup(Globals.Lexeme);
                    if (e == null)
                        Error($"error: {Globals.Filename}:{Globals.CurLine}: undeclared variable \"{Globals.Lexeme}\"");

                    /* prepend the new symbol to our list */
                    if (pushStats.Length > 0)
                        pushStats = $"  PUSH {e.GetOffsetName()}\n{pushStats}";
                    else
                        pushStats = $"  PUSH {e.GetOffsetName()}";

                    Match(Globals.Symbol.Identifier);
                    ParamsTail(ref pushStats);
                    break;
                case (int)Globals.Symbol.Number:
                case (int)Globals.Symbol.Numfloat:
                    /* prepend the new symbol to our list */
                    if (pushStats.Length > 0)
                        pushStats = $"  PUSH {Globals.Lexeme}\n{pushStats}";
                    else
                        pushStats = $"  PUSH {Globals.Lexeme}";

                    Match(Globals.Token);
                    ParamsTail(ref pushStats);
                    break;
            }
        }
    }

    /// Implements the grammar rule: Prog -> UsingDirective NamespaceBlock [eof]
    private void Prog()
    {
        UsingDirective();
        NamespaceBlock();
        Match(Globals.Symbol.Eof);

        // if we hit EOF and didn't find Main(), error -- we don't support multiple source files
        if (!foundMain)
            Error("error: Main() not found in " + Globals.Filename);
    }


    ///
    /// Implements the grammar rule: Relation -> SimpleExpr
    ///
    void Relation(out string lex)
    {
        SimpleExpr(out lex);
    }

    ///
    /// Implements the grammar rule: ReturnLine -> [return] Expr [;] | lambda
    ///
    void ReturnLine(bool inMain)
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Return:
                Match(Globals.Symbol.Return);

                /* Main() doesn't return a value */
                if (!inMain)
                {
                    string a = string.Empty, retval;
                    Expr(a, out retval);
                    var r = Globals.SymbolTable.Lookup(retval);
                    if (r != null)
                        Emit($"    _AX = {r.GetOffsetName()}\n");
                    else
                        Emit($"    _AX = {retval}\n");
                }

                Match(Globals.Symbol.Semicolon);
                break;
            default:
                break;
        }
    }

    ///
    /// Implements the grammar rule: ShortExpr -> MoreFactor MoreTerm
    ///
    void ShortExpr(string leftLexeme, out string rightLexeme)
    {
        string tmpchar, factRight;
        MoreFactor(leftLexeme, out factRight, string.Empty, out tmpchar, true);
        MoreTerm(factRight, out rightLexeme, string.Empty, out tmpchar, true);
    }

    ///
    /// Implements the grammar rule: Signop -> -
    ///
    void Signop()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Signop:
                Match(Globals.Symbol.Signop);
                break;
            default:
                Error(Globals.CurLine, "-", Globals.Lexeme);
                break;
        }
    }

    ///
    /// Implements the grammar rule: SimpleExpr -> Term MoreTerm
    ///
    void SimpleExpr(out string rightLexeme)
    {
        string termLex, tmpchar;
        Term(out termLex);
        MoreTerm(termLex, out rightLexeme, string.Empty, out tmpchar, true);
    }

    ///
    /// Implements the grammar rule: Statement -> AssignStat | IOStat
    ///
    void Statement()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Identifier:
                AssignStat();
                break;
            case (int)Globals.Symbol.Read:
            case (int)Globals.Symbol.Write:
            case (int)Globals.Symbol.Writeln:
                IoStat();
                break;
            default:
                Error(Globals.CurLine, "an identifier, 'read', 'write', or 'writeln'", Globals.Lexeme);
                break;
        }
    }

    ///
    /// Implements the grammar rule: StatList -> Statement [;] StatList | lambda
    ///
    void StatList()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Identifier:
            case (int)Globals.Symbol.Read:
            case (int)Globals.Symbol.Write:
            case (int)Globals.Symbol.Writeln:
                Statement();
                Match(Globals.Symbol.Semicolon);
                StatList();
                break;
        }
    }

    ///
    /// Implements the grammar rule: Term -> Factor MoreFactor
    ///
    void Term(out string rightLexeme)
    {
        string leftLexeme, mulchar;
        Factor(out leftLexeme);
        MoreFactor(leftLexeme, out rightLexeme, string.Empty, out mulchar, true);
    }


    ///
    /// Implements the grammar rule: Type -> [int] | [float] | [char] | [void]
    ///
    void Type(out Globals.Symbol type)
    {
        type = Globals.Token;

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Int:
                Match(Globals.Symbol.Int);
                break;
            case (int)Globals.Symbol.Float:
                Match(Globals.Symbol.Float);
                break;
            case (int)Globals.Symbol.Char:
                Match(Globals.Symbol.Char);
                break;
            case (int)Globals.Symbol.Void:
                Match(Globals.Symbol.Void);
                break;
            default:
                type = Globals.Symbol.Unknown;
                Error(Globals.CurLine, "int, float, char, or void", Globals.Tokens[(int)Globals.Token]);
                break;
        }
    }

    ///
    /// Implements the grammar rule: UsingDirective -> [using] ClassOrNamespace [;] UsingDirective | lambda
    ///
    void UsingDirective()
    {
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Using:
                Match(Globals.Symbol.Using);
                ClassOrNamespace();
                Match(Globals.Symbol.Semicolon);
                UsingDirective();
                break;
        }
    }

    ///
    /// Implements the grammar rule: Write_List -> Write_Token Write_List_Tail
    ///
    void WriteList()
    {
        WriteToken();
        WriteListTail();
    }

    ///
    /// Implements the grammar rule: Write_List_Tail -> [,] Write_Token Write_List_Tail | lambda
    ///
    void WriteListTail()
    {
        if (Globals.Token == Globals.Symbol.Comma)
        {
            Match(Globals.Symbol.Comma);
            WriteToken();
            WriteListTail();
        }
    }

    ///
    /// Implements the grammar rule: Write_Token -> [idt] | [numt] | [literal]
    ///
    void WriteToken()
    {
        Element? w;

        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Identifier:
                if (TypeHelper.IsNumeric(Globals.Lexeme))
                {
                    Emit($"  WRI {Globals.Lexeme}\n");
                    Match(Globals.Token);
                    break;
                }

                /* make sure that the variable exists */
                w = Globals.SymbolTable.Lookup(Globals.Lexeme);
                if (w == null)
                    Error(
                        $"error: {Globals.Filename}:{Globals.CurLine}: attempt to write undeclared variable {Globals.Lexeme}");

                Emit($"  WRI {w.GetOffsetName()}\n");
                Match(Globals.Symbol.Identifier);
                break;
            case (int)Globals.Symbol.Number:
            case (int)Globals.Symbol.Numfloat:
                if (TypeHelper.IsNumeric(Globals.Lexeme))
                {
                    Emit($"  WRI {Globals.Lexeme}\n");
                    Match(Globals.Token);
                    break;
                }

                /* make sure that the variable exists */
                w = Globals.SymbolTable.Lookup(Globals.Lexeme);
                if (w == null)
                    Error(
                        $"error: {Globals.Filename}:{Globals.CurLine}: attempt to write undeclared variable {Globals.Lexeme}");

                Emit($"  WRI {w.GetOffsetName()}\n");
                Match(Globals.Token);
                break;
            case (int)Globals.Symbol.Literal:
                /* add the string to the string table */
                var s = Globals.StringTable.Insert(Globals.Lexeme);

                Emit($"  WRS {s.Name}\n");
                Match(Globals.Symbol.Literal);
                break;
        }
    }
}