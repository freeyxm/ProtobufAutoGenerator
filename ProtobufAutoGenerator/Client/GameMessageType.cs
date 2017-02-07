using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ProtobufAutoGenerator.Client
{
    class GameMessageType
    {
        private Dictionary<string, int> m_msgCodes = new Dictionary<string, int>();
        private static Regex m_regex = new Regex("\\s+(\\w+)\\s+=\\s+([-0-9]+);.*", RegexOptions.Compiled);

        public void LoadProtoFile(string filePath)
        {
            m_msgCodes.Clear();
            using (var reader = File.OpenText(filePath))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    var match = m_regex.Match(line);
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value;
                        int code = int.Parse(match.Groups[2].Value);
                        m_msgCodes.Add(key, code);
                    }
                }
            }
        }

        public bool ContainsMsg(string msg)
        {
            return m_msgCodes.ContainsKey(msg);
        }

        public int GetCode(string msg)
        {
            if (m_msgCodes.ContainsKey(msg))
                return m_msgCodes[msg];
            else
                return 0;
        }

        public Dictionary<string, int> GetData()
        {
            return m_msgCodes;
        }
    }
}
