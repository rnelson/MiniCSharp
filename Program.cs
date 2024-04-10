using MiniCSharp;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: dotnet minicsharp.dll <filename>");
    return;
}

Globals.Initialize();
Globals.Visual = true;

// Parse the source and generate an intermediate file
_ = new Parser(args[0]);

// Generate the assembly
new AssemblyGenerator(Globals.Filename, Globals.SymbolTable, Globals.StringTable).Generate();

if (Globals.Visual)
{
    Console.WriteLine();
    Globals.SymbolTable.WriteTable(0);
    Globals.Wait("\nPress enter to view the string table...");

    // Print out the string table
    Console.Clear();
    Globals.StringTable.PrintTable();
    Globals.Wait("");
}

Globals.Wait("Compilation successful!\nPress enter to quit...");