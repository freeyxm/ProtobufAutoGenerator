using System;
using System.Collections.Generic;
using System.Text;

namespace ProtobufAutoGenerator.Core
{
    interface IFileWriter
    {
        void WriteToFile(string filePath);
    }
}
