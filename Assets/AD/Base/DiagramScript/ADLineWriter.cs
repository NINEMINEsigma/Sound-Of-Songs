using System.Collections.Generic;
using System.IO;
using System;
using System.Globalization;
using AD.Types;

namespace AD.BASE.IO
{
	internal class ADLineWriter : ADWriter
	{
		internal StreamWriter baseWriter;

		internal class Entry
		{
			public ADType type;
			public object value;
		}
		internal enum WriteMode
		{
			Ref, Def
		}
		internal WriteMode mode = WriteMode.Ref;
		internal Dictionary<object, int> RefSource = new();
		internal Queue<Entry> NextTree = new();
		internal bool IsNeedUpdate = false;

		private bool isFirstProperty = true;

		public ADLineWriter(Stream stream, ADSettings settings) : this(stream, settings, true, true) { }

		internal ADLineWriter(Stream stream, ADSettings settings, bool writeHeaderAndFooter, bool mergeKeys) : base(settings, writeHeaderAndFooter, mergeKeys)
		{
			isSupportCycleType = true;
			baseWriter = new StreamWriter(stream);
			StartWriteFile();
		}

		#region WritePrimitive(value) methods.

		internal override void WritePrimitive(int value) { baseWriter.Write(value); }
		internal override void WritePrimitive(float value) { baseWriter.Write(value.ToString("R", CultureInfo.InvariantCulture)); }
		internal override void WritePrimitive(bool value) { baseWriter.Write(value ? "true" : "false"); }
		internal override void WritePrimitive(decimal value) { baseWriter.Write(value.ToString(CultureInfo.InvariantCulture)); }
		internal override void WritePrimitive(double value) { baseWriter.Write(value.ToString("R", CultureInfo.InvariantCulture)); }
		internal override void WritePrimitive(long value) { baseWriter.Write(value); }
		internal override void WritePrimitive(ulong value) { baseWriter.Write(value); }
		internal override void WritePrimitive(uint value) { baseWriter.Write(value); }
		internal override void WritePrimitive(byte value) { baseWriter.Write(System.Convert.ToInt32(value)); }
		internal override void WritePrimitive(sbyte value) { baseWriter.Write(System.Convert.ToInt32(value)); }
		internal override void WritePrimitive(short value) { baseWriter.Write(System.Convert.ToInt32(value)); }
		internal override void WritePrimitive(ushort value) { baseWriter.Write(System.Convert.ToInt32(value)); }
		internal override void WritePrimitive(char value) { WritePrimitive(value.ToString()); }
		internal override void WritePrimitive(byte[] value) { WritePrimitive(System.Convert.ToBase64String(value)); }


		internal override void WritePrimitive(string value)
		{
			baseWriter.Write("\"");

			// Escape any quotation marks within the string.
			for (int i = 0; i < value.Length; i++)
			{
				char c = value[i];
				switch (c)
				{
					case '\"':
					case '“':
					case '”':
					case '\\':
					case '/':
						baseWriter.Write('\\');
						baseWriter.Write(c);
						break;
					case '\b':
						baseWriter.Write("\\b");
						break;
					case '\f':
						baseWriter.Write("\\f");
						break;
					case '\n':
						baseWriter.Write("\\n");
						break;
					case '\r':
						baseWriter.Write("\\r");
						break;
					case '\t':
						baseWriter.Write("\\t");
						break;
					default:
						baseWriter.Write(c);
						break;
				}
			}
			baseWriter.Write("\"");
		}

		internal override void WriteNull()
		{
			baseWriter.Write("null");
		}

		#endregion

		#region Format-specific methods

		private static bool CharacterRequiresEscaping(char c)
		{
			return c == '\"' || c == '\\' || c == '“' || c == '”';
		}

		private void WriteCommaIfRequired()
		{
			if (!isFirstProperty)
				baseWriter.Write(',');
			else
				isFirstProperty = false;
			WriteNewlineAndTabs();
		}

		internal override void WriteRawProperty(string name, byte[] value)
		{
			StartWriteProperty(name); baseWriter.Write(settings.encoding.GetString(value, 0, value.Length)); EndWriteProperty(name);
		}

		internal override void StartWriteFile()
		{
			if (writeHeaderAndFooter)
				baseWriter.Write("{");
			base.StartWriteFile();
		}

		internal override void EndWriteFile()
		{
			base.EndWriteFile();
			WriteNewlineAndTabs();
			if (writeHeaderAndFooter)
				baseWriter.Write("}");
		}

		internal override void StartWriteProperty(string name)
		{
			base.StartWriteProperty(name);
			WriteCommaIfRequired();
			Write(name);

			if (settings.prettyPrint)
				baseWriter.Write(' ');
			baseWriter.Write(':');
			if (settings.prettyPrint)
				baseWriter.Write(' ');
		}

		internal override void EndWriteProperty(string name)
		{
			// It's not necessary to perform any operations after writing the property in Line.
			base.EndWriteProperty(name);
		}

		internal override void StartWriteObject(string name)
		{
			base.StartWriteObject(null);
			isFirstProperty = true;
			baseWriter.Write('\n');
			WriteTabs(serializationDepth - 1);
			baseWriter.Write('{');
		}

		internal override void EndWriteObject(string name)
		{
			base.EndWriteObject(name);
			isFirstProperty = false;
			WriteNewlineAndTabs();
			baseWriter.Write('}');
		}

		internal override void StartWriteCollection()
		{
			base.StartWriteCollection();
			baseWriter.Write('[');
			WriteNewlineAndTabs();
		}

		internal override void EndWriteCollection()
		{
			base.EndWriteCollection();
			WriteNewlineAndTabs();
			baseWriter.Write(']');
		}

		internal override void StartWriteCollectionItem(int index)
		{
			if (index != 0)
				baseWriter.Write(',');
		}

		internal override void EndWriteCollectionItem(int index)
		{
		}

		internal override void StartWriteDictionary()
		{
			StartWriteObject(null);
		}

		internal override void EndWriteDictionary()
		{
			EndWriteObject(null);
		}

		internal override void StartWriteDictionaryKey(int index)
		{
			if (index != 0)
				baseWriter.Write(',');
		}

		internal override void EndWriteDictionaryKey(int index)
		{
			baseWriter.Write(':');
		}

		internal override void StartWriteDictionaryValue(int index)
		{
		}

		internal override void EndWriteDictionaryValue(int index)
		{
		}

		#endregion

		public override void Dispose()
		{
			baseWriter?.Dispose();
			baseWriter = null;

		}

		public void WriteNewlineAndTabs()
		{
			if (settings.prettyPrint)
			{
				baseWriter.Write(Environment.NewLine);
				WriteTabs(serializationDepth);
			}
		}

		public void WriteTabs(int depth)
		{
			for (int i = 0; i < depth; i++)
				baseWriter.Write('\t');
		}

		public override void WriteUnknownObject(object value, ADType type)
		{
			if (mode == WriteMode.Ref)
			{
				if (RefSource.TryAdd(value, RefSource.Count.Share(out int id)))
				{
					NextTree.Enqueue(new()
					{
						value = value,
						type = type
					});
					IsNeedUpdate = true;
				}
				else id = RefSource[value];

				//baseWriter.Write("\n");
				//WriteTabs(serializationDepth);
				baseWriter.Write($" Ref[{id}]");
			}
			else
			{
				StartWriteObject(null);
				mode = WriteMode.Ref;
				type.Write(value, this);
				mode = WriteMode.Def;
				EndWriteObject(null);
			}
		}

		public override void Write(Type type, string key, object value)
		{
			StartWriteProperty(key);
			StartWriteObject(key);
			WriteType(type);

			//mode = WriteMode.Ref;
			NextTree.Enqueue(new()
			{
				type = ADType.GetOrCreateADType(type),
				value = value
			});
			RefSource.Add(value, 0);
			do
			{
				IsNeedUpdate = false;
				mode = WriteMode.Def;
				while (NextTree.Count > 0)
				{
					var next = NextTree.Dequeue();
					int id = RefSource[next.value];
					base.WriteProperty($"Def[{id}]", next.value, next.type);
				}
			} while (IsNeedUpdate);

			EndWriteObject(key);
			EndWriteProperty(key);
			MarkKeyForDeletion(key);
		}

		public override void Save()
		{
			base.Save();

			RefSource = new();
			NextTree = new();
			IsNeedUpdate = false;
		}
	}
}
