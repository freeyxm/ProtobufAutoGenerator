using System;
using System.Collections.Generic;
using ProtobufAutoGenerator.Core;

namespace ProtobufAutoGenerator.Client
{
    class MsgResponse : IndentStringWriter, IFileWriter
    {
        private GameMessageType m_msgType;
        private List<ProtoParser> m_protos;

        public MsgResponse(GameMessageType msgType, List<ProtoParser> protos)
        {
            m_msgType = msgType;
            m_protos = protos;
        }

        public void WriteToFile(string filePath)
        {
            if (!base.OpenFile(filePath))
                return;

            WriteLine("using System.Collections;");
            WriteLine("using System.IO;");
            WriteLine("using Google.Protobuf;");
            WriteLine(string.Format("using {0};", ProtoMsg.MsgNameSpcace));
            WriteLine("");
            WriteLine("public class MsgResponse");
            WriteLine("{");
            WriteLine("public static void HandleMemoryStream(int msgID, MemoryStream stream)", 1);
            WriteLine("{", 1);
            {
                WriteLine("if (msgID > 100000 || msgID < -100000)", 2);
                WriteLine("{", 2);
                {
                    WriteLine("LuaNetManager.OnHandlerMessage(msgID, stream.ToArray());", 3);
                }
                WriteLine("}", 2);
                WriteLine("else", 2);
                WriteLine("{", 2);
                {
                    WriteLine("CodedInputStream cis = new CodedInputStream(stream);", 3);
                    WriteLine("IMessage imessage;", 3);
                    WriteLine("switch ((GameMessageType)msgID)", 3);
                    WriteLine("{", 3);
                    {
                        var e = m_msgType.GetData().GetEnumerator();
                        while (e.MoveNext())
                        {
                            if (e.Current.Value < 0)
                            {
                                string msgName = e.Current.Key;
                                string msgClass = GetMsgClass(msgName);
                                if (string.IsNullOrEmpty(msgClass))
                                    continue;

                                string enumName = ProtoMsg.ConvertMsgNameToFuncName(msgName);
                                WriteLine(string.Format("case GameMessageType.{0}:", enumName), 4);
                                {
                                    WriteLine(string.Format("imessage = new {0}();", msgName), 5);
                                    WriteLine(string.Format("imessage.MergeFrom(cis);"), 5);
                                    WriteLine(string.Format("{0}.{1}(({2})imessage);", msgClass, enumName, msgName), 5);
                                    WriteLine(string.Format("break;"), 5);
                                }
                            }
                        }
                        WriteLine(string.Format("default:"), 4);
                        WriteLine(string.Format("break;"), 5);
                    }
                    WriteLine("}", 3);
                }
                WriteLine("}", 2); // end else
            }
            WriteLine("}", 1);
            WriteLine("}");
        }

        private string GetMsgClass(string msgName)
        {
            for (int i = 0; i < m_protos.Count; ++i)
            {
                if (m_protos[i].ContainsMsg(msgName))
                {
                    return Utility.ToUpperCaseFirst(m_protos[i].GetName());
                }
            }
            return null;
        }
    }
}
