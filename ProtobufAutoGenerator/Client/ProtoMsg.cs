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
        protected List<ProtoParser.AttrInfo> m_reqCallbackFields = new List<ProtoParser.AttrInfo>();
        protected List<ProtoParser.AttrInfo> m_reqPreMsgFields = new List<ProtoParser.AttrInfo>();
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

#if REQ_CALLBACK
            BuildReqCallbackFields();
#endif
#if REQ_PRE_MSG
            BuildReqPreMsgFields();
#endif

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

        /// <summary>
        /// 生成请求函数回调字段
        /// </summary>
        protected virtual void BuildReqCallbackFields()
        {
            m_reqCallbackFields.Clear();
            foreach (var msg in m_proto.GetMsgs())
            {
                if (!m_msgType.ContainsMsg(msg.name))
                    continue;

                bool isRequest = m_msgType.GetCode(msg.name) > 0;
                if (!isRequest)
                    continue;

                var attr = new ProtoParser.AttrInfo();
                attr.type = "System.Action<int>";
                attr.name = GetReqCallbackName(msg.name);
                m_reqCallbackFields.Add(attr);
            }
        }

        /// <summary>
        /// 生成请求函数前一次参数字段
        /// </summary>
        protected virtual void BuildReqPreMsgFields()
        {
            m_reqPreMsgFields.Clear();
            foreach (var msg in m_proto.GetMsgs())
            {
                if (!m_msgType.ContainsMsg(msg.name))
                    continue;

                bool isRequest = m_msgType.GetCode(msg.name) > 0;
                if (!isRequest)
                    continue;

                var attr = new ProtoParser.AttrInfo();
                attr.type = msg.name;
                attr.name = GetReqPreMsgName(msg.name);
                m_reqPreMsgFields.Add(attr);
            }
        }

        protected string GetReqCallbackName(string msgName)
        {
            if (msgName.EndsWith("_REQ") || msgName.EndsWith("_RES"))
                msgName = msgName.Substring(0, msgName.Length - 4);
            return string.Format("m{0}Callback", ConvertMsgNameToFuncName(msgName));
        }

        protected string GetReqPreMsgName(string msgName)
        {
            return string.Format("m{0}Msg", ConvertMsgNameToFuncName(msgName));
        }

        protected virtual void WriteFields(int indent)
        {
#if REQ_CALLBACK
            // 回调属性
            if (m_reqCallbackFields.Count > 0)
            {
                WriteLine("//-------------请求回调---------------------", indent);
                foreach (var attr in m_reqCallbackFields)
                {
                    Write("", indent);
                    Write("static ").Write(attr.type).Write(" ").Write(attr.name).WriteLine(" = null;");
                }
                WriteLine("//-------------请求回调---------------------", indent);
            }
#endif
#if REQ_PRE_MSG
            if (m_reqPreMsgFields.Count > 0)
            {
                WriteLine("", indent);
                WriteLine("//-----------上次请求参数-------------------", indent);
                foreach (var attr in m_reqPreMsgFields)
                {
                    Write("", indent);
                    Write("static ").Write(attr.type).Write(" ").Write(attr.name).WriteLine(" = null;");
                }
                WriteLine("//-----------上次请求参数-------------------", indent);
            }
#endif
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
#if REQ_CALLBACK
                if (msg.attrs.Count > 0)
                    Write(", ");
                Write("System.Action<int> callback = null");
#endif
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
#if REQ_PRE_MSG
                string msgName = GetReqPreMsgName(msg.name);
                WriteLine(string.Format("{0} = msg;", msgName), indent);
#endif
#if REQ_CALLBACK
                string cbName = GetReqCallbackName(msg.name);
                WriteLine(string.Format("{0} = callback;", cbName), indent);
#endif
                WriteLine(string.Format("ClientSession.Instance.Send((int)GameMessageType.{0}, 0, msg);", ConvertMsgNameToFuncName(msg.name)), indent);
            }
            else
            {
#if REQ_CALLBACK
                string cbName = GetReqCallbackName(msg.name);
                if (m_reqPreMsgFields.Exists((item) => { return item.name == cbName; }))
                {
                    WriteLine(string.Format("if ({0} != null)", cbName), indent);
                    WriteLine("{", indent);
                    WriteLine(string.Format("//{0}(msg.result);", cbName), indent + 1);
                    WriteLine("}", indent);
                }
#endif
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
