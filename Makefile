# $Id$

MCS=mcs
MINICS_SRC=AssemblyInfo.cs \
	   CCType.cs \
	   Globals.cs \
	   Lexical.cs \
	   RDP.cs \
	   GenAsm.cs \
	   HashTable.cs \
	   StringTable.cs \
	   Element.cs \
	   Program.cs \
	   monoClearConsole.cs
BINNAME=minicsharp.exe

all:
	$(MCS) -optimize -warn:0 -r:System.dll -target:exe -out:$(BINNAME) $(MINICS_SRC)

clean:	
	-rm $(BINNAME)
