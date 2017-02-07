using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ProtobufAutoGenerator.Core
{
    class CsParser
    {
        public class VarInfo
        {
            public string type;
            public string name;
            public string defaultValue;
            public bool isRef;
            public bool isOut;
        }

        public class Member
        {
            public string scope;
            public bool isStatic;
        }

        public class Field : Member
        {
            public bool isConst;
            public bool isReadonly;
            public VarInfo var = new VarInfo();
            public string comment = "";
        }

        public class Method : Member
        {
            public string name = "";
            public string retType = "";
            public string body = "";
            public string comment = "";
            public List<VarInfo> args = new List<VarInfo>();
        }

        private List<Field> m_fields = new List<Field>();
        private List<Method> m_methods = new List<Method>();
        private List<Method> m_delegates = new List<Method>();
        private List<string> m_using = new List<string>();

        private static Regex m_fieldRegex = new Regex("\\s*((private|protected|public)\\s+){0,1}(static\\s+){0,1}(const\\s+){0,1}(readonly\\s+){0,1}([a-zA-Z0-9_]+)\\s+([a-zA-Z0-9_]+)\\s*(=\\s*(\\S+)){0,1};\\s*(//.+){0,1}", RegexOptions.Compiled);
        private static Regex m_methodRegex = new Regex("\\s*((private|protected|public)\\s+){0,1}((static|delegate)\\s+){0,1}([a-zA-Z0-9_]+)\\s+([a-zA-Z0-9_]+)\\s*\\(", RegexOptions.Compiled);
        private static Regex m_argRegex = new Regex("\\s*((ref|out)\\s+){0,1}([a-zA-Z0-9_<>]+)\\s+([a-zA-Z0-9_]+)\\s*(=\\s*(\\S+)){0,1}", RegexOptions.Compiled);
        private static Regex m_usingRegex = new Regex("using\\s+([a-zA-Z0-9\\.]+)\\s*;\\s*", RegexOptions.Compiled);

        public void Parse(string filePath)
        {
            Clear();

            using (var reader = File.OpenText(filePath))
            {
                string line = null;
                while (!string.IsNullOrEmpty(line) || !reader.EndOfStream)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        line = reader.ReadLine();
                    }

                    Match match;
                    if ((match = m_usingRegex.Match(line)).Success)
                    {
                        m_using.Add(match.Groups[1].Value);
                        line = null;
                    }
                    else if ((match = m_fieldRegex.Match(line)).Success)
                    {
                        Field field = new Field();
                        field.scope = match.Groups[1].Value.Trim();
                        field.isStatic = !string.IsNullOrEmpty(match.Groups[3].Value);
                        field.isConst = !string.IsNullOrEmpty(match.Groups[4].Value);
                        field.isReadonly = !string.IsNullOrEmpty(match.Groups[5].Value);
                        field.var.type = match.Groups[6].Value;
                        field.var.name = match.Groups[7].Value;
                        field.var.defaultValue = match.Groups[9].Value;
                        field.comment = match.Groups[10].Value;
                        m_fields.Add(field);
                        line = null;
                    }
                    else if ((match = m_methodRegex.Match(line)).Success)
                    {
                        string decorator = match.Groups[3].Value.Trim();
                        bool isDelegate = (decorator == "delegate");

                        Method method = new Method();
                        method.scope = match.Groups[1].Value.Trim();
                        method.isStatic = (decorator == "static");
                        method.retType = match.Groups[5].Value;
                        method.name = match.Groups[6].Value;

                        StringBuilder temp = new StringBuilder();

                        // 解析参数列表
                        int frontIndex = -1;
                        int backIndex = line.IndexOf(')');
                        if (backIndex > 0)
                        {
                            frontIndex = line.IndexOf('(');
                            string args = line.Substring(frontIndex + 1, backIndex - frontIndex - 1);
                            ParseArgs(args.Trim(), ref method.args);
                            line = line.Substring(backIndex + 1).Trim();
                        }
                        else
                        {
                            frontIndex = line.IndexOf('(');
                            temp.Clear();
                            temp.Append(line.Substring(frontIndex + 1));
                            line = "";

                            while (!reader.EndOfStream)
                            {
                                line = reader.ReadLine();
                                backIndex = line.IndexOf(')');
                                if (backIndex > 0)
                                {
                                    temp.Append(line.Substring(backIndex));
                                    line = line.Substring(backIndex + 1).Trim();
                                    break;
                                }
                                else
                                {
                                    temp.Append(line);
                                    line = "";
                                }
                            }

                            temp.Replace("\n", "");
                            temp.Replace("\r", "");
                            ParseArgs(temp.ToString().Trim(), ref method.args);
                        }

                        // 解析函数体
                        if (!isDelegate)
                        {
                            temp.Clear();
                            bool bStart = false;
                            bool bEnd = false;
                            int braceCount = 0;

                            while (true)
                            {
                                for (int i = 0; i < line.Length; ++i)
                                {
                                    char ch = line[i];
                                    switch (ch)
                                    {
                                        case '{':
                                            {
                                                if (!bStart)
                                                {
                                                    bStart = true;
                                                }
                                                else
                                                {
                                                    braceCount++;
                                                    temp.Append(ch);
                                                }
                                            }
                                            break;
                                        case '}':
                                            {
                                                if (!bStart)
                                                {
                                                    throw new Exception("brace not match");
                                                }

                                                if (braceCount == 0)
                                                {
                                                    bEnd = true;
                                                }
                                                else
                                                {
                                                    braceCount--;
                                                    temp.Append(ch);
                                                }
                                            }
                                            break;
                                        default:
                                            {
                                                if (bStart)
                                                {
                                                    temp.Append(ch);
                                                }
                                            }
                                            break;
                                    }

                                    if (bEnd)
                                    {
                                        line = line.Substring(i + 1);
                                        break;
                                    }
                                }

                                if (bStart)
                                {
                                    temp.Append("\r\n");
                                }

                                if (bEnd)
                                {
                                    break;
                                }

                                if (reader.EndOfStream)
                                {
                                    throw new Exception("Method body incomplete");
                                }

                                line = reader.ReadLine();
                            }
                            method.body = temp.ToString().Trim();
                        }

                        if (isDelegate)
                        {
                            m_delegates.Add(method);
                        }
                        else
                        {
                            m_methods.Add(method);
                        }
                    }
                    else
                    {
                        // discard unexpected string
                        line = null;
                    }
                }
            }
        }

        public void Clear()
        {
            m_using.Clear();
            m_fields.Clear();
            m_methods.Clear();
            m_delegates.Clear();
        }

        private void ParseArgs(string argStr, ref List<VarInfo> args)
        {
            if (string.IsNullOrEmpty(argStr))
                return;

            string[] argArray = argStr.Split(',');
            for (int i = 0; i < argArray.Length; ++i)
            {
                Match match = m_argRegex.Match(argArray[i]);
                if (match.Success)
                {
                    VarInfo var = new VarInfo();
                    var.isRef = match.Groups[2].Value == "ref";
                    var.isOut = match.Groups[2].Value == "out";
                    var.type = match.Groups[3].Value;
                    var.name = match.Groups[4].Value;
                    var.defaultValue = match.Groups[6].Value;
                    args.Add(var);
                }
                else
                {
                    throw new Exception("ParseArgs failed");
                }
            }
        }

        public Method GetMethod(string name)
        {
            for (int i = 0; i < m_methods.Count; ++i)
            {
                Method method = m_methods[i];
                if (method.name == name)
                {
                    return method;
                }
            }
            return null;
        }

        public List<Method> GetMethods()
        {
            return m_methods;
        }

        public List<Field> GetFields()
        {
            return m_fields;
        }

        public List<Method> GetDelegates()
        {
            return m_delegates;
        }
    }
}
