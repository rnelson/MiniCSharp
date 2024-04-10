/* $Id$
 * Ross Nelson
 * Compiler Construction (CSC 446)
 * MiniCSharp/Hash Table (Element data type)
 */

#region Using declarations
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
#endregion

namespace MiniCSharp
{
	///
	/// Parameter information for methods
	///
	/// \author Ross Nelson
	///
	public class Parameter
	{
		public Element.VarType type;		/* type of the variable */
		public Element.PassingMode mode;	/* passing mode */

		///
		/// Constructor for the Parameter class
		///
		public Parameter() { }
		
		///
		/// Return a string indicating the passing mode for the object
		/// \return The textual representation of the passing mode
		///
		public override string ToString()
		{
			if (mode == Element.PassingMode.passRef)
				return "ref";
			else if (mode == Element.PassingMode.passOut)
				return "out";
			else
				return "";
		}
	}
	
	///
	/// An element in the hash table
	///
	/// \author Ross Nelson
	///
	public class Element
	{
		public enum VarType
		{
			intType,		/* integer */
			charType,		/* character */
			floatType,		/* float */
			voidType,		/* void */
			emptyType		/* nada */
		}
		
		public enum EntryType
		{
			varType,		/* variable */
			constType,		/* constant */
			methodType,		/* method/function */
			classType,		/* class */
			emptyType		/* nada */
		}
		
		public enum PassingMode
		{
			passNorm,			/* pass by value */
			passRef,			/* pass by reference */
			passOut				/* out mode passing */
		}
		
		private EntryType type;				/* entry type */
		public VarType vtype;				/* variable type */
		public PassingMode mode;			/* passing mode */
		public int location;				/* the location it is in a method declaration */
		
		private Globals.Symbol token;		/* token type */
		private Globals.Symbol acc;			/* access modifier */
		
		public Element parent;				/* the parent element to a given element */
		public string childList;			/* list of children (if applicable) */
		public string pchildList;			/* list of children, parsable format (if applicable) */
		
		private int depth;					/* the depth we are at */
		private string lexeme;				/* the element's lexeme */
		public Element next;				/* the next element in the list */
		
		private int offset;					/* offset */
		private string offsetname;			/* variable in offset notation */
		private int sizeoflocals;			/* size of local variables */
		private int sizeofparams;			/* size of parameters */
		private int intval;					/* value - VarType:intType */
		private float floatval;				/* value - VarType:floatType */
		private char charval;				/* value - VarType:charType */
		
		private int numParams;				/* number of parameters - EntryType:methodType */
		
		///
		/// Set default values for the element
		///
		public void InitValues()
		{
			type = EntryType.emptyType;
			vtype = VarType.emptyType;
			mode = PassingMode.passNorm;
			location = 0;
			SetName("");
			SetToken(Globals.Symbol._unknown);
			SetOffset(0);
			SetSizeOfLocals(0);
			SetNumParams(0);
			childList = "";
			pchildList = "";
			parent = null;
		}

		///
		/// Set the access modifier for the element
		///
		public void SetAccess(Globals.Symbol accMod) { acc = accMod; }
		
		///
		/// Set the element to be of type int
		///
		public void SetInteger()
		{
			vtype = VarType.intType;
			SetToken(Globals.Symbol._int);
			if (type != EntryType.constType)
				SetVariable();
		}
		
		///
		/// Set the element to be of type float
		///
		public void SetFloat()
		{
			vtype = VarType.floatType;
			SetToken(Globals.Symbol._float);
			if (type != EntryType.constType)
				SetVariable();
		}
		
		///
		/// Set the element to be of type char
		///
		public void SetCharacter()
		{
			vtype = VarType.charType;
			SetToken(Globals.Symbol._char);
			if (type != EntryType.constType)
				SetVariable();
		}
		
		///
		/// Set the element to be of type const
		///
		public void SetConstant() { type = EntryType.constType; }
		
		///
		/// Set the element to be a method/function
		///
		public void SetMethod(Globals.Symbol access)
		{
			type = EntryType.methodType;
			
			/* set the method's return type */
			switch ((int)access)
			{
				case (int)Globals.Symbol._int:
					vtype = VarType.intType;
					break;
				case (int)Globals.Symbol._char:
					vtype = VarType.charType;
					break;
				case (int)Globals.Symbol._float:
					vtype = VarType.floatType;
					break;
				case (int)Globals.Symbol._void:
					vtype = VarType.voidType;
					break;
				default:
					vtype = VarType.emptyType;
					break;
			}
		}
		
		///
		/// Set the element to be a class
		///
		public void SetClass() { type = EntryType.classType; }
		
		///
		/// Set the element to be a variable
		///
		public void SetVariable() { type = EntryType.varType; }
		
		///
		/// Set the integer value of the element
		/// \param value The value for the object to hold
		///
		public void SetValue(int value) { intval = value; }
		
		///
		/// Set the floating point value of the element
		/// \param value The value for the object to hold
		///
		public void SetValue(float value) { floatval = value; }
		
		///
		/// Set the character value of the element
		/// \param value The value for the object to hold
		///
		public void SetValue(char value) { charval = value; }
		
		///
		/// Set the token type for the element
		/// \param tok token type
		///
		public void SetToken(Globals.Symbol tok) { token = tok; }
		
		///
		/// Get the integer value of the element
		/// \return The value stored in the object
		///
		public int GetIntegerValue() { return intval; }
		
		///
		/// Get the floating point value of the element
		/// \return The value stored in the object
		///
		public float GetFloatValue() { return floatval; }
		
		///
		/// Get the character value of the element
		/// \return The value stored in the object
		///
		public char GetCharacterValue() { return charval; }
		
		///
		/// Get the token type for the element
		/// \return token type
		///
		public Globals.Symbol GetToken() { return token; }
		
		///
		/// Constructor for the Element class
		///
		public Element()
		{
			/* initialize variables */
			InitValues();
		}
		
		///
		/// Set the variable name
		/// \param name The name of the variable to be stored
		///
		public void SetName(string name)
		{
			lexeme = name;
		}
		
		///
		/// Set the offset value
		/// \param value The offset
		///
		public void SetOffset(int value)
		{
			offset = value;
			offsetname = "_BP" + (offset < 0 ? offset.ToString() : "+" + offset.ToString());
			
			if (depth == 1)
				offsetname = lexeme;
		}
		
		///
		/// Set the size of local variables
		/// \param value size of local variables in a class/method
		///
		public void SetSizeOfLocals(int value)
		{
			sizeoflocals = value;
		}
		
		///
		/// Set the size of parameters
		/// \param value size of parameters in a method
		///
		public void SetSizeOfParams(int value)
		{
			sizeofparams = value;
		}
		
		///
		/// Set the number of parameters that a method has
		/// \param value The number of parameters
		///
		public void SetNumParams(int value)
		{
			numParams = value;
		}
		
		///
		/// Set the depth for a specific element
		/// \param value The depth
		///
		public void SetDepth(int value)
		{
			depth = value;
		}
		
		///
		/// Obtain the name of the variable
		/// \return The variable name
		///
		public string GetName()
		{
			return lexeme;
		}
		
		///
		/// Obtain the offset
		/// \return The offset
		///
		public int GetOffset()
		{
			return offset;
		}
		
		///
		/// Obtain the variable in offset notation
		/// \return The offset notation
		///
		public string GetOffsetName()
		{
			return offsetname;
		}
		
		///
		/// Obtain the size of the variable or the size of local variables in the class/method
		/// \return The size
		///
		public int GetSizeOfLocals()
		{
			return sizeoflocals;
		}
		
		///
		/// Obtain the size of the variable or the size of parameters in the method
		/// \return The size
		///
		public int GetSizeOfParams()
		{
			return sizeofparams;
		}
		
		///
		/// Obtain the depth of the variable
		/// \return The depth
		///
		public int GetDepth()
		{
			return depth;
		}
		
		///
		/// Obtain the number of parameters of the variable
		/// \return The number of parameters
		///
		public int GetNumParams()
		{
			return numParams;
		}
		
		///
		/// Get the type of the element
		/// \return EntryType of the element
		///
		public EntryType GetEType() { return type; }
		
		///
		/// Get the type of the element
		/// \return VarType of the element
		///
		public VarType GetVType() { return vtype; }
	}
}
