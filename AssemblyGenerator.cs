using System.Text;
using MiniCSharp.Extensions;

namespace MiniCSharp;

/// <summary>
/// Translates TAX to x86 assembly (Intel style)
/// </summary>
public class AssemblyGenerator
{
    private readonly FileStream outputFileStream;
    private readonly StreamWriter outputFileWriter;
    private readonly StreamReader inputFileReader;
    private readonly HashTable symbols;
    private readonly StringTable strings;
    private List<string?> tokens;
    private int tacTokenCount;
    private string? reader;

    public AssemblyGenerator(string filename, HashTable symbolTable, StringTable stringTable)
    {
        symbols = symbolTable;
        strings = stringTable;
        tokens = new List<string?>();

        // Open the input file
        var outputFileInfo = new FileInfo($"{Globals.GetFilename(Globals.Filename, '.')}.tac");
        inputFileReader = outputFileInfo.OpenText();

        // Open the output file
        var assemblyFile = $"{Globals.GetFilename(filename, '.')}.s";
        outputFileStream = new FileStream(assemblyFile, FileMode.Create);
        outputFileWriter = new StreamWriter(outputFileStream, Encoding.ASCII);

        // Get an initial set of tokens
        GetTokens();
    }

    /// <summary>
    /// Writes <paramref name="text"/> to the output file
    /// </summary>
    /// <param name="text">The text to write</param>
    private void Emit(string text)
    {
        if (text.Length > 8)
        {
            outputFileWriter.Write(text[..9] == "      end" ? text : $"{text}\n");
        }
        else
        {
            outputFileWriter.Write($"{text}\n");
        }
    }

    /// <summary>
    /// Get tokens from the next line of text from the three address code file
    /// </summary>
    /// <returns><c>true</c> if tokens were found, else <c>false</c></returns>
    private bool GetTokens()
    {
        reader = null;
        tokens = [];

        // Skip over blank lines
        while (reader?.Length == 0)
        {
            reader = inputFileReader.ReadLine()?.Trim() ?? null;

            if (reader == null)
                return false;
        }

        tokens = reader!.Split().ToList();
        tacTokenCount = tokens.Count;
        return true;
    }

    /// <summary>
    /// Translate _BP+X -> [bp+x] (except class-scope variables), return numerics as-is
    /// </summary>
    /// <param name="tacAddress">The address in TAC format</param>
    /// <returns>The address in x86 assembly format</returns>
    private static string GetAddress(string tacAddress)
    {
        // If we have a number, return it as-is
        if (TypeHelper.IsNumeric(tacAddress))
            return tacAddress;

        // If it's shorter than 3 characters and non-numeric, assume it's a class-scope
        if (tacAddress.Length < 3)
            return tacAddress;

        // If we have a 'global' (class-scope) variable, return that. Otherwise, return an assembly-friendly pointer
        return tacAddress[..3] != "_BP"
            ? tacAddress
            : $"[bp{tacAddress[3]}{tacAddress.Substring(4, tacAddress.Length - 4)}]";
    }

    /// <summary>
    /// Finds a given method in the symbol table
    /// </summary>
    /// <param name="tacName">The name of the method</param>
    /// <returns>The <see cref="Element"/> for the method</returns>
    private Element? FindMethod(string tacName)
    {
        if (string.IsNullOrWhiteSpace(tacName))
            return null;

        // Split the name into method and class
        tacName.Split('.', 2)
            .Unpack(out var sClass)
            .Unpack(out var sMethod);

        // Find the method and return it or null
        var element = symbols.Lookup(sMethod);
        if (element is null)
            return null;

        if (sMethod == "Main")
            return null;

        return element.Parent?.GetName() == sClass ? element : null;
    }

    /// <summary>
    /// Generate the assembly
    /// </summary>
    public void Generate()
    {
        // Print out information about the program
        Header();
        DataSegment();
        CodeSegment();

        // Print the program itself
        Procedures();
        Start();

        // Close open streams
        outputFileWriter.Flush();
        outputFileWriter.Close();
        outputFileStream.Close();
        inputFileReader.Close();
    }

    /// <summary>
    /// Write out the header
    /// </summary>
    private void Header()
    {
        Emit($"; {Globals.Filename}");
        Emit("    .model small");
        Emit("    .586");
        Emit("    .stack 100h");
        Emit("    ");
    }

    /// <summary>
    /// Write out the DS
    /// </summary>
    private void DataSegment()
    {
        Emit("; data segment");
        Emit("    .data");

        // Add all strings
        for (var sc = 0; sc < strings.NumberOfStrings; sc++)
        {
            var st = strings.Lookup($"_S{sc}");

            // Sanitize the string we don't deal with escaped characters anywhere in the lexical analyzer so this isn't an issue, but it's good to have in there
            var str = st.String?
                .Substring(1, st.String.Length - 2)
                .Replace("\"", "\", 34, \"");
            
            Emit($"_S{sc} DB \"{str}\", '$'");
        }

        // Print out class-scope variables
        Emit(symbols.GenerateAssemblyData(1));
        Emit("    ");
    }

    /// <summary>
    /// Write out the CS
    /// </summary>
    private void CodeSegment()
    {
        Emit("; code segment");
        Emit("    .code");
        Emit("    include io.asm");
    }

    /// <summary>
    /// Write out each procedure
    /// </summary>
    private void Procedures()
    {
        var t1 = tokens[0] ?? null;
        var t2 = tokens[1] ?? null;
        var t3 = tokens[2] ?? null;
        var t4 = tokens[3] ?? null;
        var t5 = tokens[4] ?? null;
        
        while ("START" != t1)
        {
            var sizeOLocals = 0;
            var sizeOParams = 0;
            var locals = 0;

            Element? method;
            string procedureName;
            switch (t1)
            {
                case "PROC":
                    method = FindMethod(t2);
                    if (method != null)
                    {
                        sizeOLocals = method.GetSizeOfLocals();
                        sizeOParams = method.GetSizeOfParams();
                    }
                    else
                    {
                        sizeOLocals = 30; // No magic to this number
                        sizeOParams = 0;
                    }

                    locals = sizeOLocals;

                    // Rename the procedure
                    procedureName = t2.Replace(".", "_");

                    Emit($"{procedureName} proc");
                    Emit("    push bp");
                    Emit("    mov bp, sp");
                    if (locals != 0)
                        Emit($"    sub sp, {locals}");
                    break;
                case "ENDP":
                    method = FindMethod(t2);
                    if (method != null)
                    {
                        sizeOLocals = method.GetSizeOfLocals();
                        sizeOParams = method.GetSizeOfParams();
                    }
                    else
                    {
                        sizeOLocals = 30; // No magic to this number
                        sizeOParams = 0;
                    }

                    locals = sizeOLocals;

                    // Rename the procedure
                    procedureName = t2.Replace(".", "_");

                    if (locals != 0)
                        Emit($"    add sp, {locals}");
                    Emit("    pop bp");
                    Emit(sizeOParams == 0 ? "    ret" : $"    ret {sizeOParams}");
                    Emit($"{procedureName} endp");
                    break;
                case "PUSH":
                    Emit(!TypeHelper.IsNumeric(t2) ? $"    push {GetAddress(t2)}" : $"    push {t2}");
                    break;
                case "CALL":
                    Emit($"    call {t2.Replace(".", "_")}");
                    break;
                case "WRI":
                    WriteInt(t2);
                    break;
                case "WRS":
                    WriteStr(t2);
                    break;
                case "WRLN":
                    WriteLn();
                    break;
                case "RDI":
                    ReadInt(t2);
                    break;
                default:
                    // Figure out what kind of statement we have
                    if (tacTokenCount == 5)
                    {
                        switch (t4)
                        {
                            case "+":
                                Add(t1, t3, t5);
                                break;
                            case "-":
                                Sub(t1, t3, t5);
                                break;
                            case "/":
                                Div(t1, t3, t5);
                                break;
                            case "*":
                                Mul(t1, t3, t5);
                                break;
                            default:
                                Console.WriteLine("Oops, unexpected situation: {0}", reader);
                                break;
                        }
                    }
                    else if (tacTokenCount == 4)
                    {
                        switch (t3)
                        {
                            case "-":
                                Neg(t1, t4);
                                break;
                            default:
                                Console.WriteLine("Oops, unexpected situation: {0}", reader);
                                break;
                        }
                    }
                    else if (tacTokenCount == 3)
                    {
                        Ass(t1, t3);
                    }
                    else
                    {
                        if (reader?.Length > 0)
                            Console.WriteLine("Oops, unexpected situation: {0}", reader);
                    }

                    break;
            }

            // Get the next set of tokens
            if (GetTokens() == false)
                return;
        }
    }

    /// <summary>
    /// Write out the 'start' procedures
    /// </summary>
    private void Start()
    {
        var firstProcedure = t2.Replace(".", "_");

        Emit("start proc");
        Emit("    mov ax, @data");
        Emit("    mov ds, ax");
        Emit($"    call {firstProcedure}");
        Emit("    mov al, 0");
        Emit("    mov ah, 4ch");
        Emit("    int 21h");
        Emit("start endp");
        Emit("      end start");
    }

    /// <summary>
    /// Handle an add instruction
    /// </summary>
    /// <param name="dest">Destination location</param>
    /// <param name="left">LHS</param>
    /// <param name="right">RHS</param>
    private void Add(string dest, string left, string right)
    {
        // If we're adding 1, just increment. Optimization!
        if (left == "1" && !TypeHelper.IsNumeric(right))
        {
            Emit($"    inc {GetAddress(right)}");
            return;
        }

        // If we're adding 1, just increment. Optimization!
        if (right == "1" && !TypeHelper.IsNumeric(left))
        {
            Emit($"    inc {GetAddress(left)}");
            return;
        }

        // If we're adding 0, skip this line. Optimization!
        if (left == "0" || right == "0")
            return;

        if (TypeHelper.IsNumeric(left))
        {
            if (TypeHelper.IsNumeric(right))
            {
                Emit($"    mov ax, {left}");
                Emit($"    add ax, {right}");
                Emit($"    mov {GetAddress(dest)}, ax");
            }
            else
            {
                Emit($"    mov ax, {left}");
                Emit($"    add ax, {GetAddress(right)}");
                Emit($"    mov {GetAddress(dest)}, ax");
            }
        }
        else if (TypeHelper.IsNumeric(right))
        {
            Emit($"    mov ax, {GetAddress(left)}");
            Emit($"    add ax, {right}");
            Emit($"    mov {GetAddress(dest)}, ax");
        }
        else
        {
            Emit($"    mov ax, {GetAddress(left)}");
            Emit($"    add ax, {GetAddress(right)}");
            Emit($"    mov {GetAddress(dest)}, ax");
        }
    }

    /// <summary>
    /// Handle a subtraction instruction
    /// </summary>
    /// <param name="dest">Destination location</param>
    /// <param name="left">LHS</param>
    /// <param name="right">RHS</param>
    private void Sub(string dest, string left, string right)
    {
        // If we're subtracting 1, just decrement. Optimization!
        if (left == "1" && !TypeHelper.IsNumeric(right))
        {
            Emit($"    dec {GetAddress(right)}");
            return;
        }

        // If we're subtracting 1, just decrement. Optimization!
        if (right == "1" && !TypeHelper.IsNumeric(left))
        {
            Emit($"    dec {GetAddress(left)}");
            return;
        }

        // If we're subtracting 0, skip this line. Optimization!
        if (left == "0" || right == "0")
            return;

        if (TypeHelper.IsNumeric(left))
        {
            if (TypeHelper.IsNumeric(right))
            {
                Emit($"    mov ax, {left}");
                Emit($"    sub ax, {right}");
                Emit($"    mov {GetAddress(dest)}, ax");
            }
            else
            {
                Emit($"    mov ax, {left}");
                Emit($"    sub ax, {GetAddress(right)}");
                Emit($"    mov {GetAddress(dest)}, ax");
            }
        }
        else if (TypeHelper.IsNumeric(right))
        {
            Emit($"    mov ax, {GetAddress(left)}");
            Emit($"    sub ax, {right}");
            Emit($"    mov {GetAddress(dest)}, ax");
        }
        else
        {
            Emit($"    mov ax, {GetAddress(left)}");
            Emit($"    sub ax, {GetAddress(right)}");
            Emit($"    mov {GetAddress(dest)}, ax");
        }
    }

    /// mul (imul) instruction
    private void Mul(string dest, string left, string right)
    {
        // If we're multiplying by 0, just set the value to 0. Optimization!
        if (left == "0")
        {
            Emit($"    mov {GetAddress(right)}, 0");
            return;
        }

        // If we're multiplying by 0, just set the value to 0. Optimization!
        if (right == "0")
        {
            Emit($"    mov {GetAddress(left)}, 0");
            return;
        }

        // If we're multiplying by 1, skip this line. Optimization!
        if (left == "1" || right == "1")
            return;

        Emit("    push bx");
        Emit($"    mov ax, {GetAddress(left)}");
        Emit($"    mov bx, {GetAddress(right)}");
        Emit("    imul bx");
        Emit($"    mov {GetAddress(dest)}, ax");
        Emit("    pop bx");
    }

    /// div (idiv) instruction
    private void Div(string dest, string left, string right)
    {
        // If we're dividing by 0, just set the value to 0. Optimization? This was in the original code. I don't
        // remember if this was part of the changes in MiniCSharp over standard C#, suddenly allowing division by
        // 0, but it didn't fail any test cases in class so let's leave it.
        if (left == "0")
        {
            Emit($"    mov {GetAddress(right)}, 0");
            return;
        }

        // Also here, f we're dividing by 0, just set the value to 0. Optimization, or something.
        if (right == "0")
        {
            Emit($"    mov {GetAddress(left)}, 0");
            return;
        }

        // If we're dividing by 1, skip this line. Optimization!
        if (left == "1" || right == "1")
            return;

        Emit("    push bx");
        Emit($"    mov ax, {GetAddress(left)}");
        Emit($"    mov bx, {GetAddress(right)}");
        Emit("    idiv bx");
        Emit($"    mov {GetAddress(dest)}, ax");
        Emit("    pop bx");
    }

    /// neg instruction
    private void Neg(string dest, string right)
    {
        if (TypeHelper.IsNumeric(right))
        {
            Emit($"    mov {GetAddress(dest)}, -{right}");
            return;
        }

        Emit($"    mov ax, {GetAddress(right)}");
        Emit("    neg ax");
        Emit($"    mov {GetAddress(dest)}, ax");
    }

    /// assignment instruction
    private void Ass(string dest, string src)
    {
        if (dest == src)
            /* we don't need to do 'a = a' */
            return;

        if (dest == "_AX")
        {
            Emit($"    mov ax, {GetAddress(src)}");
        }
        else if (src == "_AX")
        {
            Emit($"    mov {GetAddress(dest)}, ax");
        }
        else
        {
            if (TypeHelper.IsNumeric(src))
            {
                Emit($"    mov ax, {src}");
            }
            else
            {
                if (src[0] == '-')
                {
                    Neg(dest, src.Substring(1, src.Length - 1));
                    return;
                }

                Emit($"    mov ax, {GetAddress(src)}");
            }

            Emit($"    mov {GetAddress(dest)}, ax");
        }
    }

    /// writestr instruction
    private void WriteStr(string strname)
    {
        Emit("    push dx");
        Emit($"    mov dx, OFFSET _S{strname[2]}");
        Emit("    call writestr");
        Emit("    pop dx");
    }

    /// writeint instruction
    private void WriteInt(string intname)
    {
        if (TypeHelper.IsNumeric(intname))
            Emit($"    mov ax, {intname}");
        else
            Emit($"    mov ax, {GetAddress(intname)}");
        Emit("    call writeint");
    }

    /// writeln instruction
    private void WriteLn()
    {
        Emit("    call writeln");
    }

    /// readint instruction
    private void ReadInt(string addr)
    {
        Emit("    push bx");
        Emit("    call readint");
        Emit($"    mov {GetAddress(addr)}, bx");
        Emit("    pop bx");
    }
}