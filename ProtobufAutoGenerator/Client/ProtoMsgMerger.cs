using System;
using System.Collections.Generic;
using System.Text;
using ProtobufAutoGenerator.Core;

namespace ProtobufAutoGenerator.Client
{
    /// <summary>
    /// 用于协议文件合并。
    /// </summary>
    class ProtoMsgMerger : ProtoMsg
    {
        private CsParser m_csParser;

        public ProtoMsgMerger(ProtoParser proto, CsParser csParser)
        {
            SetProto(proto);
            m_csParser = csParser;
        }

        protected override void WriteFields(int indent)
        {
            var delegates = m_csParser.GetDelegates();
            for (int i = 0; i < delegates.Count; ++i)
            {
                var dgt = delegates[i];
                Write("", indent);
                WriteDelegate(dgt);
            }

            var fields = m_csParser.GetFields();
            for (int i = 0; i < fields.Count; ++i)
            {
                var field = fields[i];
                Write("", indent);
                WriteField(field);
            }
        }

        protected override void WriteFunctions(int indent)
        {
            base.WriteFunctions(indent);

            bool hasSelfDefinedFunc = false;


            var methods = m_csParser.GetMethods();
            foreach (var method in methods)
            {
                string msgName = ProtoMsg.ConvertFuncNameToMsgName(method.name);
                if (!m_proto.ContainsMsg(msgName))
                {
                    if (!hasSelfDefinedFunc)
                    {
                        hasSelfDefinedFunc = true;
                        WriteLine("");
                        WriteLine("// ----------- 自定义函数 ---------------------", indent);
                        WriteLine("// 协议删除或重命名时，旧函数会移到这里，需要自行手动删除！", indent);
                    }
                    WriteFunction(method, indent);
                }
            }

            if (hasSelfDefinedFunc)
            {
                WriteLine("// ----------- 自定义函数 ---------------------", indent);
            }
        }

        protected override void WriteFunctionArgs(ProtoParser.MsgInfo msg, int indent, bool isRequest)
        {
            //string name = ConvertMsgName(msg.name);
            //var method = m_csParser.GetMethod(name);
            //// 当且仅当现有参数多于消息参数时（增加了参数），才保留现有参数
            //if (method != null && method.args.Count > msg.attrs.Count)
            //{
            //    for (int i = 0; i < method.args.Count; ++i)
            //    {
            //        if (i > 0)
            //            Write(", ");
            //        WriteVariable(method.args[i]);
            //    }
            //}
            //else
            {
                base.WriteFunctionArgs(msg, indent, isRequest);
            }
        }

        protected override void WriteFunctionBody(ProtoParser.MsgInfo msg, int indent, bool isRequest)
        {
            // 只有服务器响应函数才保留现有函数体，请求函数一般不需要额外操作，若有这样的需求，可以增加自定义函数来调用接口函数。
            if (!isRequest)
            {
                var name = ConvertMsgNameToFuncName(msg.name);
                var method = m_csParser.GetMethod(name);
                // 当且仅当现有函数体非空时，才保留现有函数体
                if (method != null && !string.IsNullOrEmpty(method.body))
                {
                    WriteLine(method.body, indent);
                    return;
                }
            }

            base.WriteFunctionBody(msg, indent, isRequest);
        }

        private void WriteFunctionComment(CsParser.Method method, int indent)
        {
            WriteLine("/// <summary>", indent);
            Write("/// ", indent);
            if (!string.IsNullOrEmpty(method.comment))
                WriteLine(method.comment);
            else
                WriteLine(ConvertMsgNameToFuncName(method.name));
            WriteLine("/// </summary>", indent);
        }

        private void WriteFunction(CsParser.Method method, int indent)
        {
            WriteLine("", indent);

            var name = method.name;
            var msgName = ProtoMsg.ConvertFuncNameToMsgName(name);
            bool isRequest = m_msgType.GetCode(msgName) > 0;

            // 函数注释
            WriteFunctionComment(method, indent);

            Write("public static void ", indent).Write(name).Write("(");
            WriteVariables(method.args);
            WriteLine(")");

            WriteLine("{", indent);
            WriteLine(method.body, indent + 1);
            WriteLine("}", indent);
        }

        private void WriteDelegate(CsParser.Method dgt)
        {
            Write(dgt.scope).Write(" ");
            Write("delegate ");
            Write(dgt.retType).Write(" ");
            Write(dgt.name);
            Write("(");
            WriteVariables(dgt.args);
            WriteLine(");");
        }

        private void WriteField(CsParser.Field field)
        {
            Write(field.scope).Write(" ");
            if (field.isStatic)
            {
                Write("static ");
            }
            WriteVariable(field.var);
            WriteLine(";");
        }

        private void WriteVariable(CsParser.VarInfo var)
        {
            Write(var.type).Write(" ");
            Write(var.name);
            if (!string.IsNullOrEmpty(var.defaultValue))
            {
                Write(" = ").Write(var.defaultValue);
            }
        }

        private void WriteVariables(List<CsParser.VarInfo> vars)
        {
            for (int i = 0; i < vars.Count; ++i)
            {
                if (i > 0)
                    Write(", ");
                WriteVariable(vars[i]);
            }
        }
    }
}
