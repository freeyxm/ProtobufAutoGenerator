using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ProtobufAutoGenerator.Core
{
    abstract class IndentStringWriter : IDisposable
    {
        private FileStream m_file;
        private StreamWriter m_writer;

        public virtual bool OpenFile(string filePath)
        {
            Close();

            m_file = File.Open(filePath, FileMode.Create);
            if (m_file.CanWrite)
            {
                m_writer = new StreamWriter(m_file);
                return true;
            }

            return false;
        }

        public virtual void Close()
        {
            if (m_writer != null)
            {
                m_writer.Close();
                m_writer = null;
            }
            if (m_file != null)
            {
                m_file.Close();
                m_file = null;
            }
        }

        public virtual IndentStringWriter WriteLine(string text, int indent = 0)
        {
            if (m_writer != null)
            {
                while (indent-- > 0)
                    m_writer.Write("    ");
                m_writer.WriteLine(text);
            }
            return this;
        }

        public virtual IndentStringWriter Write(string text, int indent = 0)
        {
            if (m_writer != null)
            {
                while (indent-- > 0)
                    m_writer.Write("    ");
                m_writer.Write(text);
            }
            return this;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
