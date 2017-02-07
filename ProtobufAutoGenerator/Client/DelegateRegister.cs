using System;
using System.Collections.Generic;
using ProtobufAutoGenerator.Core;

namespace ProtobufAutoGenerator.Client
{
    class DelegateRegister : IndentStringWriter, IFileWriter
    {
        private GameMessageType m_msgType;

        public DelegateRegister(GameMessageType msgType)
        {
            m_msgType = msgType;
        }

        public void WriteToFile(string filePath)
        {
            if (!base.OpenFile(filePath))
                return;

            WriteLine("using System.Collections;");
            WriteLine("");
            WriteLine("public class DelegateRegister");
            WriteLine("{");
            WriteLine("public static void Register()", 1);
            WriteLine("{", 1);
            {
                var e = m_msgType.GetData().GetEnumerator();
                while (e.MoveNext())
                {
                    if (e.Current.Value < 0)
                    {
                        string name = ProtoMsg.ConvertMsgNameToFuncName(e.Current.Key);
                        string line = string.Format("ClientSession.Instance.Register((int)GameMessageType.{0}, MsgResponse.HandleMemoryStream);", name);
                        WriteLine(line, 2);
                    }
                }
            }
            WriteLine("}", 1);
            WriteLine("}");
        }
    }
}
