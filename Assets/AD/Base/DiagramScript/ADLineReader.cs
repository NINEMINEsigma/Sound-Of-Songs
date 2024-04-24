using System.IO;
using System.Text;
using System;
using AD.BASE;
using System.Globalization;
using AD.Types;
using AD.Reflection;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

namespace AD.BASE.IO
{
	/*
	 * 	Specific ADReader for reading Line data.
	 * 
	 * 	Note: 	Leading & trailing whitespace is ignored whenever 
	 * 			reading characters which are part of the Line syntax,
	 * 			i.e. { } [ ] , " " :
	 */
	public class ADLineReader : ADReader
	{
		private const char endOfStreamChar = (char)65535;
        private const string _ErrorFormatCannotParse
			= "Cannot load from file because the data in it is not Line data, or the data is encrypted." +
              "\nIf the save data is encrypted, please ensure that encryption is enabled when you load, and that you are using the same password used to encrypt the data.";
        public StreamReader baseReader;

        internal ADLineReader(Stream stream, ADSettings settings, bool readHeaderAndFooter = true) : base(settings, readHeaderAndFooter)
		{
			this.baseReader = new StreamReader(stream);

			// Read opening brace from file if we're loading straight from file.
			if (readHeaderAndFooter)
			{
				try
				{
					SkipOpeningBraceOfFile();
				}
				catch
				{
					this.Dispose();
					throw new FormatException(
						_ErrorFormatCannotParse);
				}
			}
			IsSupportCycle = true;
		}

        internal class Entry
        {
			public ADType type;
			public object value;
			public int id;
		}
		//internal class Waiter
		//{
		//	public ReflectionExtension.ADReflectedMember Member;
		//	public object value;
		//}
		internal class Waiter : ADInvokableCall<object>
        {
			private static void SetValue(ReflectionExtension.ADReflectedMember Member, object origin, object value)
			{
				Member.SetValue(origin, value);
            }
			public Waiter(ReflectionExtension.ADReflectedMember member, object Obj) : base(value => SetValue(member, Obj, value)) { }
			public Waiter(IList member, int index) : base(value => member[index] = value) { }
			public Waiter(Array member,int index) : base(value => member.SetValue(value, index)) { }
            public Waiter(IDictionary member, object key) : base(value => member[key] = value) { }
			public Waiter(object member, string methodName) : base(value => member.RunMethodByName(methodName, ReflectionExtension.PublicFlags, value)) { }
            public Waiter(object member, string methodName, params object[] args) : base(value => member.RunMethodByName(methodName, ReflectionExtension.PublicFlags, value, args)) { }

            public Waiter(UnityAction<object> action) : base(action) { }
        }

		internal enum ReadMode
        {
            Ref, Def
        }
        internal ReadMode readMode = ReadMode.Ref;
        internal Dictionary<int, Entry> DefSource = new();
		internal Dictionary<int, Queue<Waiter>> CallBackWaiter = new();
        internal Queue<Entry> NextTree = new();
        //internal int IsNeedUpdateSum = 0;

        private int GetReadID(string name)
        {
			if (name == null)
				throw new ArgumentNullException("arg : name is null");
			else if (name == "Root")
				return 0;
			else if (name.StartsWith("Def[") && name.EndsWith("]"))
				return int.Parse(name[4..^1]);
			else if (name.StartsWith("Ref[") && name.EndsWith("]"))
				return int.Parse(name[4..^1]);
			else return -1;//throw new FormatException("It does not a \"Def[id]\" or \"Ref[id]\" name , but \"" + name + "\"");
        }

		/// <summary>Reads a value from the reader with the given key.</summary>
		/// <param name="key">The key which uniquely identifies our value.</param>
		public override T Input<T>(string key)
		{
			if (!Goto(key))
				throw new KeyNotFoundException("Key \"" + key + "\" was not found in file \"" + settings.FullPath + "\"." +
					" Use Load<T>(key, defaultValue) if you want to return a default value if the key does not exist.");


			DefSource = new();
			NextTree = new();
			CallBackWaiter = new();
            //IsNeedUpdateSum = 0;

            StartReadObject();
            T obj = default;
			bool isFirst = true;
			do
			{
				Type type = isFirst ? ReadTypeFromHeader<T>() : ReadTypeFromHeader<object>();
				isFirst = false;
                readMode = ReadMode.Def;
				ADType adtype = ADType.GetOrCreateADType(type);
				object value = Read<object>(adtype);
				if (currentID == 0) obj = (T)value;
				if (CallBackWaiter.TryGetValue(currentID, out var waiters))
				{
					while (waiters.Count > 0)
					{
						var waiter = waiters.Dequeue();
						waiter.Invoke(value);
					}
					CallBackWaiter.Remove(currentID);
					//IsNeedUpdateSum--;
				}
				DefSource.Add(currentID, new()
				{
					id = currentID,
					type = adtype,
					value = value
				});
                EndReadObject();
				ReadCharIgnoreWhitespace();
				if((char)baseReader.Peek() == ',')
					baseReader.Read();
            } while (CallBackWaiter.Count > 0);
			EndReadObject();

			//ReadKeySuffix(); //No need to read key suffix as we're returning. Doing so would throw an error at this point for BinaryReaders.
			return obj;

		}

		internal int currentID = 0;
		internal int currentRefID = 0;
		internal override int CurrentStateID => currentRefID;

        public override bool SetMember(ReflectionExtension.ADReflectedMember member, object obj)
        {
			if (!CallBackWaiter.ContainsKey(currentRefID))
				CallBackWaiter.Add(currentRefID, new());
			CallBackWaiter[currentRefID].Enqueue(new(member, obj));
			return true;
        }

        public override bool SetMember(IList list, int index)
        {
			return SetMember(list, index, currentRefID);
        }

        public override bool SetMember(IList list, int index, int id)
        {
            if (id == -1)
            {
				return base.SetMember(list, index, id);
            }
            if (!CallBackWaiter.ContainsKey(id))
                CallBackWaiter.Add(id, new());
            CallBackWaiter[id].Enqueue(new(list, index));
            return base.SetMember(list, index, id);
        }

        public override bool SetMember(Array array, int index)
        {
            return SetMember(array, index,currentRefID);
        }

        public override bool SetMember(Array array, int index, int id)
        {
            if (id == -1)
            {
                return base.SetMember(array, index, id);
            }
            if (!CallBackWaiter.ContainsKey(id))
                CallBackWaiter.Add(id, new());
            CallBackWaiter[id].Enqueue(new(array, index));
            return base.SetMember(array, index, id);
        }

        public override bool SetMember(IDictionary dic, object key)
        {
            if (currentRefID == -1)
            {
                return base.SetMember(dic, key);
            }
            if (!CallBackWaiter.ContainsKey(currentRefID))
                CallBackWaiter.Add(currentRefID, new());
            CallBackWaiter[currentRefID].Enqueue(new(dic, key));
            return base.SetMember(dic, key);
        }

        public override bool SetMember(object source, string methodName, int id)
        {
            if (id == -1)
            {
                return base.SetMember(source, methodName, id);
            }
            if (!CallBackWaiter.ContainsKey(id))
                CallBackWaiter.Add(id, new());
            CallBackWaiter[id].Enqueue(new(source, methodName));
			return false;
        }

        public override bool SetMember(object source, string methodName, int id, params object[] args)
        {
            if (id == -1)
            {
                return base.SetMember(source, methodName, id, args);
            }
            if (!CallBackWaiter.ContainsKey(id))
                CallBackWaiter.Add(id, new());
            CallBackWaiter[id].Enqueue(new(source, methodName, args));
            return false;
        }

		protected override T ReadObject<T>(ADType type)
		{
			if (StartReadObject())
				return default;

			object obj = null;
			if (readMode == ReadMode.Def)
			{
				readMode = ReadMode.Ref;
				obj = type.Read<T>(this);
				if (obj == null) currentRefID = -1;
			}
			else if (readMode == ReadMode.Ref)
			{
				currentRefID = GetReadID(Read_string());
				if (DefSource.TryGetValue(currentRefID, out var entry))
				{
					obj = entry.value;
				}
				else
				{
					obj = null;
				}
			}

			EndReadObject();
			try
			{
				return (T)obj;
			}
			catch
			{
				Debug.LogError($"The type of expect is {typeof(T).Name} but the instance is {obj.GetType().Name}");
				return default;
			}
		}

        #region Property/Key Methods

        /*
		 * 	Reads the name of a property, and must be positioned (with or without whitespace) either:
		 * 		- Before the '"' of a property name.
		 * 		- Before the ',' separating properties.
		 * 		- Before the '}' or ']' terminating this list of properties.
		 * 	Can be used in conjunction with Read(ADType) to read a property.
		 */
        public override string ReadPropertyName()
		{
			char c = PeekCharIgnoreWhitespace();

			// Check whether there are any properties left to read.
			if (IsTerminator(c))
				return null;
			else if (c == ',')
				ReadCharIgnoreWhitespace();
			else if (!IsQuotationMark(c))
			{
				char bad = c;
				string str = "";
				for (int i = 0;c!=endOfStreamChar&& i < 36; i++)
				{
					c = (char)baseReader.Read();
					str += c;
				}
				throw new FormatException("Expected ',' separating properties or '\"' before property name, found '" + bad + "' before \n" + str);
			}

			var propertyName = Read_string() ?? throw new FormatException("Stream isn't positioned before a property.");

            // Skip the ':' seperating property and value.
            ReadCharIgnoreWhitespace(':');

			return propertyName;
		}

		/*
		 * 	Reads the type data prefixed to this key.
		 * 	If ignore is true, it will return null to save the computation of converting
		 * 	the string to a Type.
		 */
		protected override Type ReadKeyPrefix(bool ignoreType = false)
		{
			//1
			//StartReadObject();
			string propertyName = ReadPropertyName();
			currentID = GetReadID(propertyName);
			if (currentID == -1)
				throw new FormatException("This data is not AD Key Value data. Expected property name \"Root\" or \"Def[id]\" or \"Ref[id]\", found \"" + propertyName + "\".");

			//2
			StartReadObject();
			propertyName = ReadPropertyName();

			Type dataType;
			if (propertyName == ADType.typeFieldName)
			{
				string typeString = Read_string();
				dataType = ignoreType ? null : ReflectionExtension.GetType(typeString);
			}
			else
			{
				throw new FormatException("This data is not AD Key Value data. Expected property name \"__type\" , found \"" + propertyName + "\".");
			}
			propertyName = ReadPropertyName();
			if (propertyName == ADLineWriter.valueFieldName)
			{

			}
			else
			{
				throw new FormatException("This data is not AD Key Value data. Expected property name \"__value\" , found \"" + propertyName + "\".");
			}

			return dataType;
		}

		protected override void ReadKeySuffix()
		{
            EndReadObject();
        }


		internal override bool StartReadObject()
		{
            base.StartReadObject();
			return ReadNullOrCharIgnoreWhitespace('{');
		}

		internal override void EndReadObject()
		{
			ReadCharIgnoreWhitespace('}');
            base.EndReadObject();
        }


		internal override bool StartReadDictionary()
		{
			return StartReadObject();
		}

		internal override void EndReadDictionary(){}

		internal override bool StartReadDictionaryKey()
		{
			// If this is an empty Dictionary, return false.
			if(PeekCharIgnoreWhitespace() == '}')
			{
				ReadCharIgnoreWhitespace();
				return false;
			}
			return true;
		}

		internal override void EndReadDictionaryKey()
		{
			ReadCharIgnoreWhitespace(':');
		}

		internal override void StartReadDictionaryValue(){}

		internal override bool EndReadDictionaryValue()
		{
			char c = ReadCharIgnoreWhitespace();
			// If we find a ']', we reached the end of the array.
			if(c == '}')
				return true;
			// Else, we should expect a comma.
			else if(c != ',')
				throw new FormatException("Expected ',' seperating Dictionary items or '}' terminating Dictionary, found '"+c+"'.");
			return false;
		}


		internal override bool StartReadCollection()
		{
			return ReadNullOrCharIgnoreWhitespace('[');
		}

		internal override void EndReadCollection(){}

		internal override bool StartReadCollectionItem()
		{
			// If this is an empty collection, return false.
			if(PeekCharIgnoreWhitespace() == ']')
			{
				ReadCharIgnoreWhitespace();
				return false;
			}
			return true;
		}

		internal override bool EndReadCollectionItem()
		{
			char c = ReadCharIgnoreWhitespace();
			// If we find a ']', we reached the end of the array.
			if (c == ']')
				return true;
			// Else, we should expect a comma.
			else if (c != ',')
			{
				char bad = c;
				string str = "";
				for (int i = 0; i < 36; i++)
					str += (char)baseReader.Read();
				throw new FormatException("Expected ',' seperating collection items or ']' terminating collection, found '" + bad + "', before :\n" + str);
			}
			return false;
		}

		#endregion

		#region Seeking Methods

		/* 
		 * 	Reads a string value into a StreamWriter.
		 * 	Reader should be positioned after the opening quotation mark.
		 * 	Will also read the closing quotation mark.
		 * 	If the 'skip' parameter is true, data will not be written into a StreamWriter and will return null.
		 */
		private void ReadString(StreamWriter writer, bool skip=false)
		{
			bool endOfString = false;
			// Read to end of string, or throw error if we reach end of stream.
			while(!endOfString)
			{
				char c = ReadOrSkipChar(writer, skip);
				switch(c)
				{
					case endOfStreamChar:
						throw new FormatException("String without closing quotation mark detected.");
					case '\\':
						ReadOrSkipChar(writer, skip);
						break;
					default:
						if(IsQuotationMark(c))
							endOfString = true;
						break;
				}
			}
		}

		/*
		 * 	Reads the current object in the stream.
		 * 	Stream position should be somewhere before the opening brace for the object.
		 * 	When this method successfully exits, it will be on the closing brace for the object.
		 * 	If the 'skip' parameter is true, data will not be written into a StreamWriter and will return null.
		 */
		internal override byte[] ReadElement(bool skip=false)
		{
			// If 'skip' is enabled, don't create a stream or writer as we'll discard all bytes we read.
			StreamWriter writer = skip ? null : new StreamWriter(new MemoryStream(settings.bufferSize));

			using(writer)
			{
				int nesting = 0;
				char c = (char)baseReader.Peek();

				// Determine if we're skipping a primitive type.
				// First check if it's an opening object or array brace.
				if(!IsOpeningBrace(c))
				{
					// If we're skipping a string, use SkipString().
					if(c == '\"')
					{
						// Skip initial quotation mark as SkipString() requires this.
						ReadOrSkipChar(writer, skip);
						ReadString(writer, skip);
					}
					// Else we just need to read until we reach a closing brace.
					else
						// While we've not peeked a closing brace.
						while(!IsEndOfValue((char)baseReader.Peek()))
							ReadOrSkipChar(writer, skip);

					if(skip)
						return null;
					writer.Flush();
					return ((MemoryStream)writer.BaseStream).ToArray();
				}

				// Else, we're skipping a type surrounded by braces.
				// Iterate through every character, logging nesting.
				while(true)
				{
					c = ReadOrSkipChar(writer, skip);

					if(c == endOfStreamChar) // Detect premature end of stream, which denotes missing closing brace.
						throw new FormatException("Missing closing brace detected, as end of stream was reached before finding it.");

					// Handle quoted strings.
					// According to the RFC, only '\' and '"' must be escaped in strings.
					if(IsQuotationMark(c))
					{
						ReadString(writer, skip);
						continue;
					}

					// Handle braces and other characters.
					switch(c)
					{
						case '{': // Entered another level of nesting.
						case '[': 
							nesting++;
							break;
                        case '}': // Exited a level of nesting.
						case ']':
							nesting--;
							// If nesting < 1, we've come to the end of the object.
							if(nesting<1)
							{
								if(skip)
									return null;
								writer.Flush();
								return ((MemoryStream)writer.BaseStream).ToArray();
							}
							break;
						default:
							break;
					}
				}
			}
		}

		/*
		 * 	Reads the next char into a stream, or ignores it if 'skip' is true.
		 */
		private char ReadOrSkipChar(StreamWriter writer, bool skip)
		{
			char c = (char)baseReader.Read();
			if(!skip) writer.Write(c);
			return c;
		}

		#endregion

		#region Line-specific methods.

		/*
		 * 	Reads a char from the stream and ignores leading and trailing whitespace.
		 */
		private char ReadCharIgnoreWhitespace(bool ignoreTrailingWhitespace=true)
		{
			char c;
			// Skip leading whitespace and read char.
			while(IsWhiteSpace(c = (char)baseReader.Read()))
			{}

			// Skip trailing whitespace.
            if(ignoreTrailingWhitespace)
			    while(IsWhiteSpace((char)baseReader.Peek()))
				    baseReader.Read();

			return c;
		}

		/*
		 * 	Reads a char, or the NULL value, from the stream and ignores leading and trailing whitespace.
		 * 	Returns true if NULL was read.
		 */
		private bool ReadNullOrCharIgnoreWhitespace(char expectedChar)
		{
			char c = ReadCharIgnoreWhitespace();

			// Check for null
			if(c == 'n')
			{
				var chars = new char[3];
				baseReader.ReadBlock(chars, 0, 3);
				if((char)chars[0] == 'u' && (char)chars[1] == 'l' && (char)chars[2] == 'l')
					return true;
			}

			if(c != expectedChar)
			{
				if (c == endOfStreamChar)
				{
					throw new FormatException("End of stream reached when expecting '" + expectedChar + "'.");
				}
				else
				{
					string str = "";
					char bad = c;
					for (int i = 0; c != endOfStreamChar && i < 36; i++)
					{
						c = (char)baseReader.Read();
						str += c;
					}
					throw new FormatException("Expected \'" + expectedChar + "\' or \"null\", found \'" + bad + "\' before \n" + str + "");
				}
			}
			return false;
		}

		/*
		 * 	Reads a char from the stream and ignores leading and trailing whitespace.
		 * 	Throws an error if the char isn't equal to the one specificed as a parameter, or if it's the end of stream.
		 */
		private char ReadCharIgnoreWhitespace(char expectedChar)
		{
			char c = ReadCharIgnoreWhitespace();
			if(c != expectedChar)
			{
				if (c == endOfStreamChar)
					throw new FormatException("End of stream reached when expecting '" + expectedChar + "'.");
				else
				{
					char bad = c;
					string str = "";
					for (int i = 0; c != endOfStreamChar && i < 36; i++)
					{
						c = (char)baseReader.Read();
						str += c;
					}
					throw new FormatException("Expected \'" + expectedChar + "\', found \'" + bad + "\' before \n" + str + "");
				}
			}
			return c;
		}

		private bool ReadQuotationMarkOrNullIgnoreWhitespace()
		{
			char c = ReadCharIgnoreWhitespace(false); // Don't read trailing whitespace as this is the value.

			if(c == 'n')
			{
				var chars = new char[3];
				baseReader.ReadBlock(chars, 0, 3);
				if((char)chars[0] == 'u' && (char)chars[1] == 'l' && (char)chars[2] == 'l')
					return true;
			}
			else if(!IsQuotationMark(c))
			{
				if(c == endOfStreamChar)
					throw new FormatException("End of stream reached when expecting quotation mark.");
				else
					throw new FormatException("Expected quotation mark, found \'"+c+"\'.");
			}
			return false;
		}

		/*
		 * 	Peeks the next char in the stream, ignoring leading whitespace, but not trailing whitespace.
		 */
		private char PeekCharIgnoreWhitespace(char expectedChar)
		{
			char c = PeekCharIgnoreWhitespace();
			if(c != expectedChar)
			{
				if(c == endOfStreamChar)
					throw new FormatException("End of stream reached while peeking, when expecting '"+expectedChar+"'.");
				else
					throw new FormatException("Expected \'"+expectedChar+"\', found \'"+c+"\'.");
			}
			return c;
		}

		/*
		 * 	Peeks the next char in the stream, ignoring leading whitespace, but not trailing whitespace.
		 *	Throws an error if the char isn't equal to the one specificed as a parameter.
		 */
		private char PeekCharIgnoreWhitespace()
		{
			char c;
			// Skip leading whitespace and read char.
			while(IsWhiteSpace(c = (char)baseReader.Peek()))
				baseReader.Read();
			return c;
		}

		// Skips all whitespace immediately after the current position.
		private void SkipWhiteSpace()
		{
			while(IsWhiteSpace((char)baseReader.Peek()))
				baseReader.Read();
		}

		private void SkipOpeningBraceOfFile()
		{
			// Skip the whitespace and '{' at the beginning of the Line file.
			char firstChar = ReadCharIgnoreWhitespace();
			if(firstChar != '{') // If first char isn't '{', it's not valid Line.
				throw new FormatException("File is not valid Line. Expected '{' at beginning of file, but found '"+firstChar+"'.");
		}

		private static bool IsWhiteSpace(char c)
		{
			return (c == ' ' || c == '\t' || c == '\n' || c == '\r');
		}

		private static bool IsOpeningBrace(char c)
		{
			return (c == '{' || c == '[');
		}

		private static bool IsEndOfValue(char c)
		{
			return (c == '}' || c == ' ' || c == '\t' || c == ']' || c == ',' || c== ':' || c == endOfStreamChar || c == '\n' || c == '\r');
		}

		private static bool IsTerminator(char c)
		{
			return (c == '}' || c == ']');
		}

		private static bool IsQuotationMark(char c)
		{
			return c == '\"' || c == '“' || c == '”';
		}

		private static bool IsEndOfStream(char c)
		{
			return c == endOfStreamChar;
		}

		/*
		 * 	Reads a value (i.e. non-string, non-object) from the stream as a string.
		 * 	Used mostly in Read_[type]() methods.
		 */
		private string GetValueString()
		{
			StringBuilder builder = new StringBuilder();

			while(!IsEndOfValue(PeekCharIgnoreWhitespace()))
				builder.Append((char)baseReader.Read());

			// If it's an empty value, return null.
			if(builder.Length == 0)
				return null;
			return builder.ToString();
		}

		#endregion

		#region Primitive Read() Methods.

		internal override string Read_string()
		{
			if(ReadQuotationMarkOrNullIgnoreWhitespace())
				return null;
			char c;

			StringBuilder sb = new StringBuilder();

			while(!IsQuotationMark((c = (char)baseReader.Read())))
			{
				// If escape mark is found, generate correct escaped character.
				if(c == '\\')
				{
					c = (char)baseReader.Read();
					if(IsEndOfStream(c))
						throw new FormatException("Reached end of stream while trying to read string literal.");

					switch(c)
					{
						case 'b':
							c = '\b';
							break;
						case 'f':
							c = '\f';
							break;
						case 'n':
							c = '\n';
							break;
						case 'r':
							c = '\r';
							break;
						case 't':
							c = '\t';
							break;
						default:
							break;
					}
				}
				sb.Append(c);
			}
			return sb.ToString();
		}
        //internal override long Read_ref()
        //{
        //    if (ADReferenceMgr.Current == null)
        //        throw new InvalidOperationException("An AD Manager is required to load references. To add one to your scene, exit playmode and go to Tools > AD > Add Manager to Scene");
        //    if (IsQuotationMark(PeekCharIgnoreWhitespace()))
        //        return long.Parse(Read_string());
        //    return Read_long();   
        //}


        internal override char		Read_char()		{ return char.Parse(		Read_string()); 	}
		internal override float		Read_float()	{ return float.Parse(		GetValueString(), CultureInfo.InvariantCulture); 	}
		internal override int 		Read_int()		{ return int.Parse(			GetValueString()); 	}
		internal override bool 		Read_bool()		{ return bool.Parse(		GetValueString()); 	}
		internal override decimal 	Read_decimal()	{ return decimal.Parse(		GetValueString(), CultureInfo.InvariantCulture); 	}
		internal override double 	Read_double()	{ return double.Parse(		GetValueString(), CultureInfo.InvariantCulture); 	}
		internal override long 		Read_long()		{ return long.Parse(		GetValueString()); 	}
		internal override ulong 	Read_ulong()	{ return ulong.Parse(		GetValueString()); 	}
		internal override uint 		Read_uint()		{ return uint.Parse(		GetValueString()); 	}
		internal override byte 		Read_byte()		{ return (byte)int.Parse(	GetValueString()); 	}
		internal override sbyte 	Read_sbyte()	{ return (sbyte)int.Parse(	GetValueString()); 	}
		internal override short 	Read_short()	{ return (short)int.Parse(	GetValueString()); 	}
		internal override ushort 	Read_ushort()	{ return (ushort)int.Parse(	GetValueString()); 	}

		internal override byte[] 	Read_byteArray(){ return System.Convert.FromBase64String(Read_string()); }

		#endregion


		public override void Dispose()
		{
			baseReader?.Dispose();
			baseReader = null;

        }
	}
}