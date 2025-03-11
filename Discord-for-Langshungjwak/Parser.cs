using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

public partial class Parser
{
    internal static string SkipTokenRemove(string code)
    {
        for (int i = 0; i < Skips.Length; i++)
            code = code.Replace(Skips[i].ToString(), "");
        return code;
    }
    internal static IEnumerable<Token> Tokenize(string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            // 출력
            if (IsKwEquals(line, "비비", ref i))
            {
                int varId = -1;
                SkipCurrentChar(line, 'ㅋ', ref i, () => varId++);
                bool raw = IsKwEquals(line, "보호막", ref i);
                SkipCurrentChar(line, 'ㅋ', ref i, () => varId++);
                SkipCurrentChar(line, '따', ref i);
                if (line[i] != '잇') throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                SkipNextChar(line, 'ㅋ', ref i, () => varId++);
                if (varId < 0) varId = 0;
                yield return new Token(raw ? 4 : 3, varId);
            }

            // 입력
            if (IsKwEquals(line, "순수", ref i))
            {
                int varId = -1;
                SkipCurrentChar(line, 'ㅋ', ref i, () => varId++);
                SkipCurrentChar(line, '따', ref i);
                if (line[i] != '잇') throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
                SkipNextChar(line, 'ㅋ', ref i, () => varId++);
                yield return new Token(5, varId);
            }

            // 조건
            if (IsKwEquals(line, "하는재미", ref i))
                yield return new Token(6, -1);

            // 연산
            if (IsCalculateOperation(line[i]))
                yield return TokenizeCalculateOperation(line, ref i);

            // 숫자 & 변수
            if (IsNumberOrVariable(line[i]))
                yield return TokenizeNumberOrVariable(line, ref i);

            // 이동
            if (IsKwEquals(line, "에잇", ref i))
            {
                int target = line.Count(c => c == 'ㅋ');
                int isIf = line.IndexOf("하는재미");
                if (isIf != -1) target = line.Substring(0, isIf).Count(c => c == 'ㅋ');
                bool down = IsKwEquals(line, "에잇", ref i);
                yield return new Token(7, down ? target : -target);
            }
        }
    }
    private static bool IsNumberOrVariable(char start) => start == '좌' || start == '좍' || start == '슈' || start == '슝';
    private static Token TokenizeNumberOrVariable(string line, ref int i)
    {
        SkipCurrentChar(line, 'ㅋ', ref i);
        // 숫자
        if (line[i] == '좌')
        {
            int value = 2;
            SkipNextChar(line, '아', ref i, () => value++);
            if (line[i] != '악') throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
            return new Token(0, value);
        }
        else if (line[i] == '좍') return new Token(0, 1);
        // 변수
        else if (line[i] == '슈')
        {
            int value = 1;
            SkipNextChar(line, '우', ref i, () => value++);
            if (line[i] != '웅') throw new InvalidOperationException("어떻게 이게 리슝좍이냐!");
            return new Token(2, value);
        }
        else if (line[i] == '슝') return new Token(2, 0);
        throw new InvalidOperationException($"어떻게 이게 리슝좍이냐!");
    }
    private static bool IsCalculateOperation(char c) => Array.IndexOf(Operators, c) != -1;
    private static Token TokenizeCalculateOperation(string line, ref int i)
    {
        // 연산
        int opIndex = Array.IndexOf(Operators, line[i]);
        SkipNextChar(line, Operators[opIndex], ref i);
        if (Array.IndexOf(Operators, line[i]) != -1)
            throw new InvalidOperationException($"어떻게 이게 리슝좍이냐!");
        return new Token(1, opIndex);
    }
    private static bool IsKwEquals(string line, string kw, ref int index)
    {
        if (line.Length - index < kw.Length) return false;
        for (int i = 0; i < kw.Length; i++)
            if (line[index + i] != kw[i]) return false;
        index += kw.Length;
        return true;
    }
    private static void SkipCurrentChar(string line, char c, ref int index, Action? onSkip = null)
    {
        while (index < line.Length && line[index] == c)
        {
            index++;
            onSkip?.Invoke();
        }
    }
    private static void SkipNextChar(string line, char c, ref int index, Action? onSkip = null)
    {
        while (index < line.Length - 1 && line[++index] == c) onSkip?.Invoke();
    }

    private static readonly char[] Operators = { '~', ';', ',', '@' };
    private static readonly char[] Skips = { '?', '!', '.', ' ' };
    internal struct Token
    {
        /// <summary>
        /// 0: Number
        /// 1: Operator (0:Add/1:Sub/2:Mul/3:Div)
        /// 2: Variable
        /// 3: Print ASCII
        /// 4: Print Raw
        /// 5: Input
        /// 6: If
        /// 7: Goto
        /// </summary>
        public int type;
        public object value;
        public Token(int type, object value)
        {
            this.type = type;
            this.value = value;
        }
    }

    [GeneratedRegex("에잇")]
    private static partial Regex BranchMatcher();
}
