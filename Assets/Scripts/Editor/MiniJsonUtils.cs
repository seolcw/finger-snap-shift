using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ShiftMagicSpell.Editor
{
    /// <summary>
    /// 작은 JSON 읽기 도구. JsonUtility는 사전형 객체({"FIRE": {...}})를 못 읽어서 직접 파싱한다.
    /// 결과: 객체 = Dictionary&lt;string, object&gt;, 배열 = List&lt;object&gt;, 숫자 = double, 문자열, bool, null.
    /// </summary>
    public static class MiniJsonUtils
    {
        public static object Parse(string json)
        {
            int index = 0;
            object value = ParseValue(json, ref index);
            SkipWhitespace(json, ref index);
            if (index != json.Length)
            {
                throw new FormatException($"Unexpected trailing text at {index}");
            }

            return value;
        }

        private static object ParseValue(string json, ref int index)
        {
            SkipWhitespace(json, ref index);
            char ch = json[index];
            switch (ch)
            {
                case '{':
                    return ParseObject(json, ref index);
                case '[':
                    return ParseArray(json, ref index);
                case '"':
                    return ParseString(json, ref index);
                case 't':
                    index += 4;
                    return true;
                case 'f':
                    index += 5;
                    return false;
                case 'n':
                    index += 4;
                    return null;
                default:
                    return ParseNumber(json, ref index);
            }
        }

        private static Dictionary<string, object> ParseObject(string json, ref int index)
        {
            var result = new Dictionary<string, object>();
            index++;
            while (true)
            {
                SkipWhitespace(json, ref index);
                if (json[index] == '}')
                {
                    index++;
                    return result;
                }

                string key = ParseString(json, ref index);
                SkipWhitespace(json, ref index);
                index++; // ':'
                result[key] = ParseValue(json, ref index);
                SkipWhitespace(json, ref index);
                if (json[index] == ',')
                {
                    index++;
                }
            }
        }

        private static List<object> ParseArray(string json, ref int index)
        {
            var result = new List<object>();
            index++;
            while (true)
            {
                SkipWhitespace(json, ref index);
                if (json[index] == ']')
                {
                    index++;
                    return result;
                }

                result.Add(ParseValue(json, ref index));
                SkipWhitespace(json, ref index);
                if (json[index] == ',')
                {
                    index++;
                }
            }
        }

        private static string ParseString(string json, ref int index)
        {
            var builder = new StringBuilder();
            index++;
            while (json[index] != '"')
            {
                char ch = json[index++];
                if (ch != '\\')
                {
                    builder.Append(ch);
                    continue;
                }

                char escaped = json[index++];
                switch (escaped)
                {
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'u':
                        builder.Append((char)Convert.ToInt32(json.Substring(index, 4), 16));
                        index += 4;
                        break;
                    default:
                        builder.Append(escaped);
                        break;
                }
            }

            index++;
            return builder.ToString();
        }

        private static double ParseNumber(string json, ref int index)
        {
            int start = index;
            while (index < json.Length && "+-0123456789.eE".IndexOf(json[index]) >= 0)
            {
                index++;
            }

            return double.Parse(json.Substring(start, index - start), CultureInfo.InvariantCulture);
        }

        private static void SkipWhitespace(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }
    }
}
