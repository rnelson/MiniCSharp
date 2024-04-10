namespace MiniCSharp;

internal class Lexical
{
    public char Ch; /* current or lookahead char */
    private readonly FileInfo fs; /* input file stream part 1 */
    private readonly StreamReader stream; /* input file stream part 2 */
    private string reader; /* read string from the file stream */
    private bool haveToken; /* we have a token, stop reading */
    private bool eof; /* true=eof, false=!eof */

    /* Lexical()
     *
     * Dummy constructor to spit out an error and kill the calling Main()
     */
    public Lexical()
    {
        Console.WriteLine("error: default Lexical constructor called, filename needed");
        Environment.Exit(1);
    }

    /* Lexical()
     *
     * The lexical analyzer's contructor, which sets some default values
     * and opens up the passed in filename
     */
    public Lexical(string filename)
    {
        /* set default values */
        CleanUp();
        eof = false;

        /* save the filename */
        Globals.Filename = filename;

        /* open the source file */
        try
        {
            fs = new FileInfo(filename);
            stream = fs.OpenText();
        }
        catch
        {
            Console.WriteLine("There has been an error opening {0}.  The program is terminating.", filename);
            Environment.Exit(-1);
        }
    }

    /* ~Lexical()
     *
     * The lexical analyzer's destructor, closes the file
     */
    ~Lexical()
    {
        /* close the source file, set the token to End of File */
        try
        {
            stream.Close();
        }
        catch
        {
            Console.WriteLine("error closing {0}", Globals.Filename);
        }

        Globals.Token = Globals.Symbol.Eof;
    }

    /* DisplayOutput()
     *
     * Print out information about the token
     */
    public void DisplayOutput()
    {
        /* increment the line count */
        Globals.Linecount++;

        /* if we've printed a lot, clear the screen */
        if (Globals.Linecount > 15)
        {
            /* reset the line count */
            Globals.Linecount = 0;
            /* wait for the user to press enter */
            Globals.Wait("\nPress enter to continue...");

            /* print out the header */
            Console.WriteLine("Token\t\t        Lexeme\t\t        Attribute");
            Console.WriteLine("-----\t\t        ------\t\t        ---------");
        }

        /* print out the data */
        switch ((int)Globals.Token)
        {
            case (int)Globals.Symbol.Literal:
                Console.WriteLine("{0,-10}\t\t{1,-10}\t\t{2}", Globals.Tokens[(int)Globals.Token], Globals.Lexeme,
                    Globals.Literal);
                break;
            case (int)Globals.Symbol.Number:
                Console.WriteLine("{0,-10}\t\t{1,-10}\t\t{2}", Globals.Tokens[(int)Globals.Token], Globals.Lexeme,
                    Globals.Value);
                break;
            case (int)Globals.Symbol.Numfloat:
                Console.WriteLine("{0,-10}\t\t{1,-10}\t\t{2}", Globals.Tokens[(int)Globals.Token], Globals.Lexeme,
                    Globals.ValueF);
                break;
            default:
                Console.WriteLine("{0,-10}\t\t{1,-10}", Globals.Tokens[(int)Globals.Token], Globals.Lexeme);
                break;
        }
        /* we don't want this stuff now */
    }

    /* GetNextToken()
     *
     * Read the next token from a string of text
     * and place it into `lexeme`; if a literal
     * string or integer/floating point number is
     * found, place it into the appropriate public
     * variable
     */
    public void GetNextToken()
    {
        /* skip comments */
        if (reader != null)
            while (Ch == '/' && reader[0] == '/')
            {
                GetNextLine();
                if (reader == null)
                    break;
            }

        /* clean up the mess */
        CleanUp();
        eof = false;

        /* avoid trying to reference something that doesn't exist */
        if (reader == null)
            GetNextLine();

        /* remove leading and trailing whitespace, if possible */
        try
        {
            reader.Trim();
        }
        catch
        {
            /* nothing */
        }

        /* ignore whitespace */
        while (Ch <= ' ' && !eof) GetNextChar();

        /* as long as we read text in, process the token */
        if (reader != null) ProcessToken();
    }

    /* GetNextChar()
     *
     * Place the next char into `ch`
     */
    private void GetNextChar()
    {
        /* clear out the current value */
        Ch = (char)0;

        /* stop if eof */
        if (eof)
        {
            Globals.Token = Globals.Symbol.Eof;
            return;
        }

        /* don't try to access the Length property if we have no string */
        var newLine = false;
        if (reader == null || reader == string.Empty)
            try
            {
                GetNextLine();
                newLine = true;
            }
            catch
            {
                /* nothing to do */
            }

        if (!newLine && Globals.Token != Globals.Symbol.Eof)
            if (reader != null)
            {
                if (reader.Length > 0)
                {
                    /* place the next character into `ch` */
                    Ch = reader[0];

                    /* remove `ch` from reader */
                    reader = reader.Substring(1, reader.Length - 1);
                }
                else
                {
                    GetNextLine();
                }
            }
    }

    /* GetNextLine()
     *
     * Read the next line from the source file
     */
    private void GetNextLine()
    {
        try
        {
            /* read a line from the file */
            reader = stream.ReadLine();
            Globals.CurLine++;

            /* set eof if needed */
            if (reader == null)
            {
                eof = true;
                Globals.Token = Globals.Symbol.Eof;
                return;
            }

            /* make sure we don't have a blank line */
            reader.Trim();
            while (reader.Length == 0)
            {
                reader = stream.ReadLine();

                if (reader == null)
                {
                    eof = true;
                    Globals.Token = Globals.Symbol.Eof;
                    return;
                }

                reader.Trim();
            }

            /* save the first character */
            Ch = reader[0];

            /* clear whitespace */
            while (char.IsWhiteSpace(Ch))
            {
                reader = reader.Substring(1, reader.Length - 1);
                Ch = reader[0];
            }

            /* remove `ch` from reader */
            reader = reader.Substring(1, reader.Length - 1);
        }
        catch
        {
            /* r795: changed to call GetNextLine() recursively   */
            /*       now, a line of just whitespace doesn't kill */
            /*       the parser when it gets _unknown instead of */
            /*       a real value                                */
            //CleanUp();
            //eof = true;
            //Globals.token = Globals.Symbol._unknown;
            //reader = string.Empty;
            GetNextLine();
        }
    }

    /* ProcessToken()
     *
     * Decide which function to use to process
     * a given token
     */
    private void ProcessToken()
    {
        if (haveToken)
            return;

        if (!char.IsWhiteSpace(Ch))
            Globals.Lexeme = Ch.ToString();
        else
            Globals.Lexeme = string.Empty;

        /* grab the next character so we know if it's a comment/2-char operator */
        GetNextChar();

        try
        {
            if (char.IsLetter(Globals.Lexeme[0]) || Globals.Lexeme[0] == '_') /* word */
            {
                ProcessWordToken();
            }
            else if (char.IsDigit(Globals.Lexeme[0])) /* number */
            {
                ProcessNumericToken();
            }
            else if (Globals.Lexeme[0] == '\"' || Globals.Lexeme[0] == '\'') /* literal */
            {
                ProcessLiteral();
            }
            else
            {
                /* for complex operations (+=, -=, /=, *=, %=, ==, !=, etc), lex[0] holds the first
                 character and ch holds the second */
                if (TypeHelper.IsOperator(Globals.Lexeme[0]) && TypeHelper.IsOperator(Ch)) /* 2-character operator */
                {
                    /* deal with comments */
                    if (Globals.Lexeme[0] == '/' && Ch == '/')
                    {
                        ProcessComment();
                    }
                    else if (reader[0] == '/' && Ch == '/')
                    {
                        ProcessComment();
                    }
                    else
                    {
                        ProcessDoubleToken();
                        GetNextChar();
                    }
                }
                else
                {
                    ProcessSingleToken();
                }
            }
        }
        catch
        {
            /* let's just ignore it -- that's good programming, right? */
        }
    }

    /* ProcessWordToken()
     *
     * Process an alphanumeric (+ underscore) token
     */
    private void ProcessWordToken()
    {
        /* fill lexeme */
        while ((char.IsLetter(Ch) || char.IsDigit(Ch) || Ch == '_') && !eof)
        {
            Globals.Lexeme += Ch.ToString();
            GetNextChar();
        }

        /* check to see if we have a reserved word */
        for (var count = 0; count < Globals.ReservedWords.Length; count++)
            if (Globals.Lexeme == Globals.ReservedWords[count])
            {
                Globals.Token = (Globals.Symbol)count;
                haveToken = true;
                break;
            }

        /* if we get here, we've got an identifier */
        if (Globals.Token == Globals.Symbol.Unknown)
        {
            Globals.Token = Globals.Symbol.Identifier;
            haveToken = true;
        }
    }

    /* ProcessNumericToken()
     *
     * Read in until no more digits or more than one
     * period is found, leave the string in lexeme and
     * the value in value or valueF, depending on int/float
     */
    private void ProcessNumericToken()
    {
        var havePeriod = false; /* only allow one decimal point */

        /* fill lexeme */
        while (char.IsDigit(Ch) || (Ch == '.' && !eof))
        {
            if (char.IsDigit(Ch) || Ch == '.')
            {
                /* kill the '12..05' bug */
                if (Ch == '.' && havePeriod)
                {
                    Globals.Lexeme += Ch;
                    while ((char.IsDigit(Ch) || Ch == '.') && !eof)
                    {
                        GetNextChar();
                        if (char.IsDigit(Ch) || Ch == '.')
                            Globals.Lexeme += Ch;
                    }

                    Console.WriteLine("error: {0}:{1}: \"{2}\" is an invalid number", Globals.Filename, Globals.CurLine,
                        Globals.Lexeme);
                    Environment.Exit(-2);
                }

                Globals.Lexeme += Ch;
            }

            if (Ch == '.')
                havePeriod = true;

            GetNextChar();
        }

        /* convert the string to a number and return the token type */
        if (havePeriod)
        {
            Globals.Value = 0;
            Globals.ValueF = Convert.ToDouble(Globals.Lexeme);
            Globals.Token = Globals.Symbol.Numfloat;
            haveToken = true;
        }
        else
        {
            Globals.Value = Convert.ToInt32(Globals.Lexeme);
            Globals.ValueF = 0.0;
            Globals.Token = Globals.Symbol.Number;
            haveToken = true;
        }
    }

    /* ProcessCommentToken()
     *
     * Take care of (read: ignore) comments
     */
    private void ProcessComment()
    {
        /* By the time we get to this chunk of code, ch holds '/' and

      Globals.lexeme[0] holds '/', we know we've got a comment.

      Since double slash comments extend to the end of the line,

      nothing more has to be done with the line of text we have.

      Wipe it out and let a parent function read in the next line. */
        reader = string.Empty;
        Globals.Token = Globals.Symbol.Comment;
        GetNextLine();
        GetNextToken();
    }

    /* ProcessSingleToken()
     *
     * Deal with single operators...assignop, mulop, addop, and so forth
     */
    private void ProcessSingleToken()
    {
        /* ch holds the operator */
        switch (Globals.Lexeme[0])
        {
            case '<':
            case '>':
                Globals.Token = Globals.Symbol.Relop;
                haveToken = true;
                break;
            case '!':
                Globals.Token = Globals.Symbol.Unarynot;
                haveToken = true;
                break;
            case '+':
                Globals.Token = Globals.Symbol.Addop;
                haveToken = true;
                break;
            case '-':
                Globals.Token = Globals.Symbol.Signop;
                haveToken = true;
                break;
            case '*':
            case '/':
                Globals.Token = Globals.Symbol.Mulop;
                haveToken = true;
                break;
            case '=':
                Globals.Token = Globals.Symbol.Assignop;
                haveToken = true;
                break;
            case '(':
                Globals.Token = Globals.Symbol.Lparen;
                haveToken = true;
                break;
            case ')':
                Globals.Token = Globals.Symbol.Rparen;
                haveToken = true;
                break;
            case '{':
                Globals.Token = Globals.Symbol.Lbrace;
                haveToken = true;
                break;
            case '}':
                Globals.Token = Globals.Symbol.Rbrace;
                haveToken = true;
                break;
            case '[':
                Globals.Token = Globals.Symbol.Lbracket;
                haveToken = true;
                break;
            case ']':
                Globals.Token = Globals.Symbol.Rbracket;
                haveToken = true;
                break;
            case ',':
                Globals.Token = Globals.Symbol.Comma;
                haveToken = true;
                break;
            case ':':
                Globals.Token = Globals.Symbol.Colon;
                haveToken = true;
                break;
            case ';':
                Globals.Token = Globals.Symbol.Semicolon;
                haveToken = true;
                break;
            case '.':
                Globals.Token = Globals.Symbol.Period;
                haveToken = true;
                break;
            case '\'':
                Globals.Token = Globals.Symbol.Quote;
                haveToken = true;
                break;
            case '"':
                Globals.Token = Globals.Symbol.Dquote;
                haveToken = true;
                break;
            case '&':
                Globals.Token = Globals.Symbol.Bandop;
                haveToken = true;
                break;
            case '|':
                Globals.Token = Globals.Symbol.Borop;
                haveToken = true;
                break;
            default:
                Globals.Token = Globals.Symbol.Unknown;
                haveToken = true;
                break;
        }
    }

    /* ProcessDoubleToken()
     *
     * Take care of two-character operators, such as += -= *= /= %=
     * and so on
     */
    private void ProcessDoubleToken()
    {
            switch (Globals.Lexeme[0])
            {
                case '+': case '-': case '/': case '*': case '%':
                    Globals.Token = Globals.Symbol.Assignop;
                    Globals.Lexeme = Globals.Lexeme[0] + Ch.ToString();
                    haveToken = true;
                    break;
                case '<': case '>':
                    Globals.Token = Globals.Symbol.Relop;
                    Globals.Lexeme = Globals.Lexeme[0] + Ch.ToString();
                    haveToken = true;
                    break;
                case '=': case '!':
                    Globals.Token = Globals.Symbol.Condop;
                    Globals.Lexeme = Globals.Lexeme[0] + Ch.ToString();
                    haveToken = true;
                    break;
                case '&':
                    if (Ch == '&')
                    {
                        Globals.Token = Globals.Symbol.Andop;
                        Globals.Lexeme = Globals.Lexeme[0] + Ch.ToString();
                        haveToken = true;
                    }
                    break;
                case '|':
                    if (Ch == '|')
                    {
                        Globals.Token = Globals.Symbol.Orop;
                        Globals.Lexeme = Globals.Lexeme[0] + Ch.ToString();
                        haveToken = true;
                    }
                    break;
                default:
                    if (!TypeHelper.IsOperator(Globals.Lexeme[0]))
                    {
                        ProcessSingleToken();
                        break;
                    }
                    Globals.Token = Globals.Symbol.Unknown;
                    haveToken = true;
                    break;
            }
        }

    /* ProcessLiteral()
     *
     * Take care of literals
     */
        private void ProcessLiteral()
        {
            bool dblQuote;

            /* keep track of the character to look for */
            var findMe = Globals.Lexeme[0];
            Globals.Literal = string.Empty;

            /* what kind of quote do we have? */
            if (findMe == '\"')
                dblQuote = true;
            else
                dblQuote = false;

            /* print out the information about the current quote mark */
            Globals.Lexeme = findMe.ToString();
            if (dblQuote)
                Globals.Token = Globals.Symbol.Dquote;
            else
                Globals.Token = Globals.Symbol.Quote;

            /* get the remainder of literal */
            while (Ch != findMe)
            {
                if (reader.Length == 0)
                {
                    Console.WriteLine("warning: {0}:{1}: unterminated literal, expecting {2}", Globals.Filename,
                        Globals.CurLine, findMe);
                    break;
                }

                Globals.Lexeme += Ch.ToString();
                Globals.Literal += Ch.ToString();

                if (Ch != findMe)
                    GetNextChar();
            }

            /* add the quote on the end */
            Globals.Lexeme += Ch.ToString();

            /* set the token type */
            Globals.Token = Globals.Symbol.Literal;

            /* read the next character */
            GetNextChar();
        }

        /* CleanUp()
         *
         * Reset variables
         */
        private void CleanUp()
        {
            /* reset things */
            Globals.Lexeme = string.Empty;
            Globals.Token = Globals.Symbol.Unknown;
            Globals.Value = 0;
            Globals.ValueF = 0.0;
            Globals.Literal = string.Empty;
            haveToken = false;

            /* fix blank line issue */
            if (reader == null)
            {
                Globals.Token = Globals.Symbol.Comment;
                return;
            }

            if (reader == string.Empty)
            {
                Globals.Token = Globals.Symbol.Comment;
                return;
            }

            if (reader[0] == '/' && reader[1] == '/')
            {
                reader = string.Empty;
                Globals.Token = Globals.Symbol.Comment;
            }
        }
    }