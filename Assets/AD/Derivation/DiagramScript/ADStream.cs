using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AD.BASE;

namespace AD.IO
{
    [Serializable]
    public class IOException : ADException
    {
        public IOException(IIOBase iob) { Base = iob; }
        public IOException(IIOBase iob, string message) : base(message) { Base = iob; }
        public IOException(IIOBase iob, string message, Exception inner) : base(message, inner) { Base = iob; }

        public readonly IIOBase Base;
    }

    [Serializable]
    public class UnknownException : IOException
    {
        public UnknownException(IIOBase iob,Exception exception)
            : base(iob, "Unknow", exception) { }
    }

    [Serializable]
    public class BufferOverflowException : IOException
    {
        public BufferOverflowException(IIOBase iob, int min, int max, int index)
            : base(iob, "Buffer Errir : index(" + index.ToString() + ") need greater than " + min.ToString() + " and less than " + max.ToString()) { }

        public static void Assertion(IIOBase iob, int min, int max, int index)
        {
            if (index >= max || index < min) throw new BufferOverflowException(iob, min, max, index);
        }
    }

    public interface IIOBase
    {
        
    }

    public class BaseBuffer : IIOBase
    {
        public byte[] Data = new byte[0];

        public virtual void InsertByte(byte[] bytes, int start, int length)
        {
            int oriSize = Data == null ? 0 : Data.Length;
            int size = length + oriSize;
            BufferOverflowException.Assertion(this, 0, oriSize, start);
            byte[] newBytes = new byte[size];
            try
            {
                for (int i = 0; i < start; i++)
                {
                    newBytes[i] = Data[i];
                }
                for (int i = start; i < length; i++)
                {
                    newBytes[start + i] = bytes[i];
                }
            }
            catch (Exception ex)
            {
                throw new UnknownException(this, ex);
            }
            Data = newBytes;
        }
        public virtual void RemoveByte(int start, int length)
        {
            int oriSize = Data == null ? 0 : Data.Length;
            int size = oriSize - length;
            BufferOverflowException.Assertion(this, 0, size, start);
            byte[] newBytes = new byte[size];
            try
            {
                for (int i = 0; i < start; i++)
                {
                    newBytes[i] = Data[i];
                }
                for (int i = start + length; i < size; i++)
                {
                    newBytes[start + i] = Data[i];
                }
            }
            catch (Exception ex)
            {
                throw new UnknownException(this, ex);
            }
            Data = newBytes;
        }

        public int Length => Data.Length;
    }

    public abstract class BaseStream: IIOBase
    {
        private readonly BaseBuffer m_buffer;
        /// <summary>
        /// The buffer of this stream
        /// </summary>
        public BaseBuffer BaseBuffer => m_buffer;

        private int m_State;
        /// <summary>
        /// The current state of this stream
        /// </summary>
        public int State => m_State;

        public BaseStream(BaseBuffer buffer,int initStatus)
        {
            m_buffer = buffer;
            m_State = initStatus;
        }

        public abstract void Next();
        public abstract void Refresh();
        public abstract void Close();

        public int Position;
    }

    public abstract class IStream : BaseStream
    {
        public IStream(BaseBuffer buffer, int initStatus) : base(buffer, initStatus)
        {
        }

        protected void DoImport()
        {
            DoImportStart();
            DoImportProcess();
            DoImportEnd();
        }
        protected abstract void DoImportStart();
        protected abstract void DoImportProcess();
        protected abstract void DoImportEnd();
    }

    public abstract class OStream : BaseStream
    {
        public OStream(BaseBuffer buffer, int initStatus) : base(buffer, initStatus)
        {
        }

        protected void DoExport()
        {
            DoExportStart();
            DoExportProcess();
            DoExportEnd();
        }
        protected abstract void DoExportStart();
        protected abstract void DoExportProcess();
        protected abstract void DoExportEnd();
    }

    public class StringBuffer : BaseBuffer
    {
        private System.Text.Encoding m_Encoding;
        public System.Text.Encoding Encoding
        {
            get
            {
                m_Encoding ??= System.Text.Encoding.UTF8;
                return m_Encoding;
            }
        }
        public string GetString()
        {
            return Encoding.GetString(this.Data);
        }
        public void SetString(string str)
        {
            this.Data = Encoding.GetBytes(str);
        }

        public virtual void InsertString(string str, int start, int length)
        {
            string oriStr = GetString();
            int oriSize = oriStr.Length;
            BufferOverflowException.Assertion(this, 0, oriSize, start);
            string newStr;
            try
            {
                newStr = oriStr[..start] + str + oriStr[start..];
            }
            catch (Exception ex)
            {
                throw new UnknownException(this, ex);
            }
            SetString(newStr);
        }
        public virtual void RemoveString(int start, int length)
        {
            string oriStr = GetString();
            int oriSize = oriStr.Length;
            BufferOverflowException.Assertion(this, 0, oriSize, start);
            string newStr;
            try
            {
                newStr = oriStr[..start] + oriStr[(start + length)..];
            }
            catch (Exception ex)
            {
                throw new UnknownException(this, ex);
            }
            SetString(newStr);
        }
    }

    public abstract class IStringStream : IStream
    {
        public IStringStream(StringBuffer buffer, int initStatus) : base(buffer, initStatus)
        {
        }
    }

    public abstract class OStringStream : OStream
    {
        public OStringStream(StringBuffer buffer, int initStatus) : base(buffer, initStatus)
        {
        }
    }
}

namespace AD.IO.Standard
{
    [Serializable]
    public class IFormatStream : IStringStream
    {
        public IFormatStream() : base(new StringBuffer(), 0)
        {

        }

        public override void Close()
        {

        }

        public override void Next()
        {
            throw new NotImplementedException();
        }

        public override void Refresh()
        {

        }

        protected override void DoImportEnd()
        {
            throw new NotImplementedException();
        }

        protected override void DoImportProcess()
        {
            throw new NotImplementedException();
        }

        protected override void DoImportStart()
        {
            throw new NotImplementedException();
        }
    }
}
