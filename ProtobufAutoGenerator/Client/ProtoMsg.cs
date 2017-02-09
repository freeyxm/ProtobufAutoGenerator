using System;
using System.Collections.Generic;
using System.Text;
using ProtobufAutoGenerator.Core;

namespace ProtobufAutoGenerator.Client
{
    class ProtoMsg : IndentStringWriter, IFileWriter
    {
        public const string MsgNameSpcace = "ProtoMsg";
        protected ProtoParser m_proto;
        protected GameMessageType m_msgType;
        private static Dictionary<string, string> m_typeMapping;

        public ProtoMsg()
        {
        }

        public ProtoMsg(ProtoParser proto, GameMessageType msgType)
        {
            SetProto(proto);
            SetMsgType(msgType);
        }

        public void SetProto(ProtoParser proto)
        {
            m_proto = proto;
        }

        public void SetMsgType(GameMessageType msgType)
        {
            m_msgType = msgType;
        }

        public virtual void WriteToFile(string filePath)
        {
            if (m_proto == null || m_msgType == null)
            {
                Console.WriteLine("proto or msgType is null.");
                return;
            }

            if (!OpenFile(filePath))
                return;

            WriteLine("using System;");
            WriteLine("using System.Collections.Generic;");
            WriteLine("");
            WriteLine("namespace " + MsgNameSpcace);
            WriteLine("{");

            // class
            string className = Utility.ToUpperCaseFirst(m_proto.GetName());
            WriteLine("public class " + className, 1);
            WriteLine("{", 1);
            WriteFields(2);
            WriteFunctions(2);
            WriteLine("}", 1);

            WriteLine("}");

            Close();
        }

        protected virtual void WriteFields(int indent)
        {
        }

        protected virtual void WriteFunctions(int indent)
        {
            foreach (var msg in m_proto.GetMsgs())
            {
                if (!m_msgType.ContainsMsg(msg.name))
                    continue;

                WriteLine("", indent);

                var name = ConvertMsgNameToFuncName(msg.name);
                bool isRequest = m_msgType.GetCode(msg.name) > 0;

                // 函数注释
                WriteFunctionComment(msg, isRequest, indent);

                Write("public static void ", indent).Write(name).Write("(");
                WriteFunctionArgs(msg, indent, isRequest);
                WriteLine(")");

                WriteLine("{", indent);
                WriteFunctionBody(msg, indent + 1, isRequest);
                WriteLine("}", indent);
            }
        }

        protected virtual void WriteFunctionArgs(ProtoParser.MsgInfo msg, int indent, bool isRequest)
        {
            if (isRequest)
            {
                // 参数列表
                for (int i = 0; i < msg.attrs.Count; ++i)
                {
                    var attr = msg.attrs[i];
                    if (i > 0)
                        Write(", ");
                    Write(ConvertType(attr)).Write(" ").Write(attr.name);
                }
            }
            else
            {
                // 参数列表
                Write(string.Format("{0} msg", msg.name));
            }
        }

        protected virtual void WriteFunctionBody(ProtoParser.MsgInfo msg, int indent, bool isRequest)
        {
            // 函数体
            if (isRequest)
            {
                WriteLine(string.Format("{0} msg = new {0}();", msg.name), indent);
                for (int i = 0; i < msg.attrs.Count; ++i)
                {
                    var attr = msg.attrs[i];
                    var pname = ConvertAttrName(attr.name);
                    if (attr.isList)
                        WriteLine(string.Format("msg.{0}.AddRange({1});", pname, attr.name), indent);
                    else
                        WriteLine(string.Format("msg.{0} = {1};", pname, attr.name), indent);
                }
                WriteLine(string.Format("ClientSession.Instance.Send((int)GameMessageType.{0}, 0, msg);", ConvertMsgNameToFuncName(msg.name)), indent);
            }
            else
            {
                // null
            }
        }

        protected virtual void WriteFunctionComment(ProtoParser.MsgInfo msg, bool param, int indent)
        {
            WriteLine("/// <summary>", indent);
            Write("/// ", indent);
            if (!string.IsNullOrEmpty(msg.comment))
                WriteLine(msg.comment);
            else
                WriteLine(ConvertMsgNameToFuncName(msg.name));
            WriteLine("/// </summary>", indent);

            if (param)
            {
                for (int i = 0; i < msg.attrs.Count; ++i)
                {
                    var attr = msg.attrs[i];
                    WriteLine(string.Format("/// <param name=\"{0}\">{1}</param>", attr.name, attr.comment), indent);
                }
            }
        }

        public static string ConvertMsgNameToFuncName(string name)
        {
            StringBuilder temp = new StringBuilder();
            var values = name.Split('_');
            for (int i = 0; i < values.Length; ++i)
            {
                temp.Append(Utility.ToUpperCaseFirst(values[i].ToLower()));
            }
            return temp.ToString();
        }

        public static string ConvertFuncNameToMsgName(string name)
        {
            StringBuilder temp = new StringBuilder();
            for (int i = 0; i < name.Length; ++i)
            {
                char ch = name[i];
                if (i > 0 && char.IsUpper(ch))
                {
                    temp.Append('_');
                }
                temp.Append(char.ToUpper(ch));
            }
            return temp.ToString();
        }

        private static string ConvertAttrName(string name)
        {
            if (name.Contains("_"))
            {
                StringBuilder temp = new StringBuilder();
                var values = name.Split('_');
                for (int i = 0; i < values.Length; ++i)
                {
                    temp.Append(Utility.ToUpperCaseFirst(values[i].ToLower()));
                }
                return temp.ToString();
            }
            else
            {
                return Utility.ToUpperCaseFirst(name);
            }
        }

        protected static string ConvertType(ProtoParser.AttrInfo attr)
        {
            string type;
            if (m_typeMapping.ContainsKey(attr.type))
                type = m_typeMapping[attr.type];
            else
                type = attr.type;

            if (attr.isList)
                return string.Format("List<{0}>", type);
            else
                return type;
        }

        static ProtoMsg()
        {
            // protobuf 类型到 C# 类型的映射
            m_typeMapping = new Dictionary<string, string>();
            m_typeMapping.Add("int32", "int");
            m_typeMapping.Add("int64", "long");
            m_typeMapping.Add("float", "float");
            m_typeMapping.Add("string", "string");
        }
    }
}
