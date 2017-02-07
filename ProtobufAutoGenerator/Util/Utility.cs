using System;
using System.Collections.Generic;
using System.Text;

namespace ProtobufAutoGenerator
{
    class Utility
    {
        /// <summary>
        /// 首字母大写
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToUpperCaseFirst(string str)
        {
            StringBuilder temp = new StringBuilder();
            temp.Append(char.ToUpper(str[0]));
            temp.Append(str, 1, str.Length - 1);
            return temp.ToString();
        }
    }
}
