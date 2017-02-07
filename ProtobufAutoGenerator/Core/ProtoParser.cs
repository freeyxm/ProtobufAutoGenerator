using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace ProtobufAutoGenerator.Core
{
    class ProtoParser
    {
        public class AttrInfo
        {
            public string type;
            public string name;
            public string comment;
            public bool isList;
            public int index;

            private static Regex regex = new Regex("\\s+(\\w+\\s+){0,1}(\\w+)\\s+(\\w+)\\s+=\\s+([0-9]+);(//.+){0,}", RegexOptions.Compiled);

            public static AttrInfo Parse(string text)
            {
                AttrInfo attrInfo = null;
                var ms = regex.Matches(text);
                var match = regex.Match(text);
                if (match.Success)
                {
                    attrInfo = new AttrInfo();
                    attrInfo.isList = !string.IsNullOrEmpty(match.Groups[1].Value);
                    attrInfo.type = match.Groups[2].Value;
                    attrInfo.name = match.Groups[3].Value;
                    attrInfo.index = int.Parse(match.Groups[4].Value);
                    attrInfo.comment = match.Groups[5].Value;
                    if (attrInfo.comment.StartsWith("//"))
                        attrInfo.comment = attrInfo.comment.Substring(2);
                }
                return attrInfo;
            }
        }

        public class MsgInfo
        {
            public string name;
            public string comment;
            public List<AttrInfo> attrs = new List<AttrInfo>();
        }
        private string m_name;
        private List<MsgInfo> m_msgs = new List<MsgInfo>();
        private static Regex m_regex = new Regex("(//.+){0,}(\r\n){0,}^message\\s(\\w+)\\s\\{\r\n(([^\\}]*){0,})\\}", RegexOptions.Compiled | RegexOptions.Multiline);
        private static string[] m_attrSplit = new string[] { "\r\n" };

        public ProtoParser()
        {
        }

        public void Parse(string filePath)
        {
            m_msgs.Clear();
            m_name = new FileInfo(filePath).Name;
            m_name = m_name.Substring(0, m_name.Length - ".proto".Length);

            using (var reader = File.OpenText(filePath))
            {
                string text = reader.ReadToEnd();
                var matches = m_regex.Matches(text);
                for (int i = 0; i < matches.Count; ++i)
                {
                    Match match = matches[i];
                    MsgInfo msgInfo = new MsgInfo();

                    msgInfo.comment = match.Groups[1].Value;
                    if (msgInfo.comment.StartsWith("//"))
                        msgInfo.comment = msgInfo.comment.Substring(2);

                    msgInfo.name = match.Groups[3].Value;
                    string attrsText = match.Groups[4].Value;

                    string[] attrs = attrsText.Split(m_attrSplit, StringSplitOptions.RemoveEmptyEntries);
                    for (int k = 0; k < attrs.Length; ++k)
                    {
                        var attrInfo = AttrInfo.Parse(attrs[k]);
                        if (attrInfo != null)
                        {
                            msgInfo.attrs.Add(attrInfo);
                        }
                    }

                    m_msgs.Add(msgInfo);
                }
            }
        }

        public string GetName()
        {
            return m_name;
        }

        public List<MsgInfo> GetMsgs()
        {
            return m_msgs;
        }

        public bool ContainsMsg(string name)
        {
            foreach (var msg in m_msgs)
            {
                if (msg.name == name)
                    return true;
            }
            return false;
        }
    }
}
