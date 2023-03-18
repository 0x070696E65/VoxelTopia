using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
public class NotNull : Attribute {
}

public class JsonDecodeException : Exception {
    public JsonDecodeException(string message) : base(message) {
    }
    
    public JsonDecodeException(string message, Exception innerException) : base(message, innerException) {
    }
}

public class JsonDeserializer {
    /// <summary>
    /// JSON配列をからオブジェクトを作成する。内部でJsonDeserializer.FromJSON<T>を呼び出している
    /// </summary>
    /// <param name="json">json配列</param>
    /// <typeparam name="T">変換したいオブジェクト</typeparam>
    /// <returns>オブジェクト配列</returns>
    public static List<T> FromJsonArray<T>(string json) {
        return Parser.ParseList(json).Select(s => FromJSON<T>(s)).ToList();
    }

    
    /// <summary>
    /// JSONからオブジェクトを生成し、Nullチェックを行う。
    /// [NotNull]をつけたフィールドがnullの場合、JsonDecodeExceptionが発生する。
    /// </summary>
    /// <param name="json"></param>
    /// <param name="errorIfNull">trueの場合、JSON作成に失敗したらJsonDecodeExceptionが発生する</param>
    /// <typeparam name="T">変換したいオブジェクト</typeparam>
    /// <returns>デシリアライズしたオブジェクト</returns>
    public static T FromJSON<T>(string json, bool errorIfNull = false) {
        try {
            var res = JsonUtility.FromJson<T>(json);
            if (res == null && errorIfNull) {
                throw new JsonDecodeException($"failed to parse json.");
            }

            var _type = res.GetType();
            Validate(_type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance), res, true);
            Validate(_type.GetFields(BindingFlags.Public | BindingFlags.Instance), res, false);
            return res;
        }
        catch (System.ArgumentException e) {
            throw new JsonDecodeException("failed to parse json.", e);                
        }
    }
    
    private static void Validate<T>(FieldInfo[] fields, T res, bool shouldHasSerializeField) {
        foreach (var field in fields) {
            if (shouldHasSerializeField && field.GetCustomAttribute(typeof(SerializeField)) == null) {
                continue;
            }                
            
            if (field.GetCustomAttribute(typeof(NotNull)) == null) {
                continue;
            }

            var val = field.GetValue(res);

            if (val == null || string.IsNullOrEmpty(val.ToString())) {
                throw new JsonDecodeException($"{field.Name} is null.");
            }
        }
    }
    
    /// <summary>
    /// Jsonの配列を[文字列,文字列...]という形でパースするクラス。
    /// https://gist.github.com/darktable/1411710 を参考に作成
    /// </summary>
    private sealed class Parser : IDisposable {
        const string WORD_BREAK = "{}[],:\"";

        private static bool IsWordBreak(char c) {
            return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
        }

        enum TOKEN {
            NONE, CURLY_OPEN, CURLY_CLOSE, SQUARED_OPEN, SQUARED_CLOSE, COMMA, OTHER, NULL
        };

        private StringReader json;

        private Parser(string jsonString) {
            json = new StringReader(jsonString);
        }

        public static IList<string> ParseList(string jsonString) {
            using (var instance = new Parser(jsonString)) {
                return instance.ParseValue();
            }
        }

        IList<string> ParseValue() {
            TOKEN nextToken = NextToken;
            switch (nextToken) {
                case TOKEN.SQUARED_OPEN:
                    return ParseArray();
                default:
                    throw new JsonDecodeException("ParseList must list.");
            }
        }

        IList<string> ParseArray() {
            var closeCount = 1;
            IList<string> array = new List<string>();
            json.Read();

            var parsing = true;
            while (parsing) {
                if (json.Peek() == -1) {
                    break;
                }

                var nextToken = NextToken;

                switch (nextToken) {
                    case TOKEN.NONE:
                        return null;
                    case TOKEN.COMMA:
                        continue;
                    case TOKEN.SQUARED_OPEN:
                        closeCount++;
                        continue;
                    case TOKEN.SQUARED_CLOSE:
                        closeCount--;
                        if (closeCount <= 0) {
                            parsing = false;
                            break;
                        }

                        continue;
                    default:
                        var value = ParseListElement(nextToken);
                        array.Add(value);
                        break;
                }
            }

            return array;
        }

        string ParseListElement(TOKEN currentToken) {
            var parsing = true;
            var s = new StringBuilder();
            json.Read();
            var objectNum = 0;
            var aryNum = 0;
            var isStr = false;
            if (currentToken == TOKEN.CURLY_OPEN) {
                s.Append('{');
                objectNum++;
            }

            if (currentToken == TOKEN.SQUARED_OPEN) {
                s.Append('[');
                aryNum++;
            }
            
            while (parsing) {
                if (json.Peek() == -1) {
                    break;
                }

                var c = NextChar;
                switch (c) {
                    case '"':
                        isStr = !isStr;
                        s.Append(c);
                        break;
                    case '\\':
                        if (json.Peek() == -1) {
                            parsing = false;
                            break;
                        }

                        c = NextChar;
                        switch (c) {
                            case '"':
                            case '\\':
                            case '/':
                                s.Append(c);
                                break;
                            case 'b':
                                s.Append('\b');
                                break;
                            case 'f':
                                s.Append('\f');
                                break;
                            case 'n':
                                s.Append('\n');
                                break;
                            case 'r':
                                s.Append('\r');
                                break;
                            case 't':
                                s.Append('\t');
                                break;
                            case 'u':
                                var hex = new char[4];

                                for (int i = 0; i < 4; i++) {
                                    hex[i] = NextChar;
                                }
                                s.Append((char) Convert.ToInt32(new string(hex), 16));
                                break;
                        }

                        break;
                    case '{':
                        if (isStr) {
                            s.Append(c);
                            break;
                        }
                        objectNum++;
                        s.Append(c);
                        break;
                    case '[':
                        if (isStr) {
                            s.Append(c);
                            break;
                        }
                        aryNum++;
                        s.Append(c);
                        break;
                    case '}':
                        if (isStr) {
                            s.Append(c);
                            break;
                        }
                        objectNum--;
                        s.Append(c);
                        break;
                    case ']':
                        if (isStr) {
                            s.Append(c);
                            break;
                        }
                        if (aryNum == 0) {
                            parsing = false;
                            break;
                        }

                        aryNum--;
                        s.Append(c);
                        break;
                    case ',':
                        if (isStr == false && aryNum == 0 && objectNum == 0) {
                            parsing = false;
                            break;
                        }

                        s.Append(c);
                        break;
                    default:
                        s.Append(c);
                        break;
                }
            }

            return s.ToString();
        }

        public void Dispose() {
            json.Dispose();
            json = null;
        }

        private string NextWord {
            get {
                var word = new StringBuilder();

                while (!IsWordBreak(PeekChar)) {
                    word.Append(NextChar);

                    if (json.Peek() == -1) {
                        break;
                    }
                }

                return word.ToString();
            }
        }

        void EatWhitespace() {
            while (Char.IsWhiteSpace(PeekChar)) {
                json.Read();

                if (json.Peek() == -1) {
                    break;
                }
            }
        }

        char PeekChar => Convert.ToChar(json.Peek());
        char NextChar => Convert.ToChar(json.Read());

        TOKEN NextToken {
            get {
                EatWhitespace();

                if (json.Peek() == -1) {
                    return TOKEN.NONE;
                }

                switch (PeekChar) {
                    case '{':
                        return TOKEN.CURLY_OPEN;
                    case '}':
                        json.Read();
                        return TOKEN.CURLY_CLOSE;
                    case '[':
                        return TOKEN.SQUARED_OPEN;
                    case ']':
                        json.Read();
                        return TOKEN.SQUARED_CLOSE;
                    case ',':
                        json.Read();
                        return TOKEN.COMMA;
                    case '"':
                    case ':':
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                    case '-':
                        return TOKEN.OTHER;
                }

                switch (NextWord) {
                    case "null":
                        return TOKEN.NULL;
                    default:
                        return TOKEN.OTHER;
                }
            }
        }
    }
}