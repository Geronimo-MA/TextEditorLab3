using System.Collections.Generic;
using TextEditorLab.Models;

namespace TextEditorLab.Services
{
    public class LexicalAnalyzer
    {
        public List<Token> Analyze(string text)
        {
            var tokens = new List<Token>();

            int i = 0;
            int line = 1;
            int col = 1;

            while (i < text.Length)
            {
                char c = text[i];
                int startIndex = i;
                int startLine = line;
                int startCol = col;

                // 1. Пробельные символы
                if (IsWhitespace(c))
                {
                    string lexeme = "";

                    while (i < text.Length && IsWhitespace(text[i]))
                    {
                        lexeme += text[i];

                        if (text[i] == '\n')
                        {
                            i++;
                            line++;
                            col = 1;
                        }
                        else
                        {
                            i++;
                            col++;
                        }
                    }

                    tokens.Add(new Token
                    {
                        Code = 11,
                        TokenType = TokenType.Whitespace,
                        TypeName = "разделитель (пробел)",
                        Lexeme = MakeWhitespaceVisible(lexeme),
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = startLine == line ? col - 1 : startCol,
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        IsError = false
                    });

                    continue;
                }

                // 2. Идентификатор
                if (char.IsLetter(c) || c == '_')
                {
                    string lexeme = "";

                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    {
                        lexeme += text[i];
                        i++;
                        col++;
                    }

                    tokens.Add(new Token
                    {
                        Code = 2,
                        TokenType = TokenType.Identifier,
                        TypeName = "идентификатор",
                        Lexeme = lexeme,
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = col - 1,
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        IsError = false
                    });

                    continue;
                }

                // 3. Число
                if (char.IsDigit(c))
                {
                    string lexeme = "";

                    while (i < text.Length && char.IsDigit(text[i]))
                    {
                        lexeme += text[i];
                        i++;
                        col++;
                    }

                    tokens.Add(new Token
                    {
                        Code = 1,
                        TokenType = TokenType.Number,
                        TypeName = "целое без знака",
                        Lexeme = lexeme,
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = col - 1,
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        IsError = false
                    });

                    continue;
                }

                // 4. ?
                if (c == '?')
                {
                    tokens.Add(new Token
                    {
                        Code = 20,
                        TokenType = TokenType.TernaryQuestion,
                        TypeName = "знак тернарного оператора",
                        Lexeme = "?",
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = startCol,
                        StartIndex = startIndex,
                        Length = 1,
                        IsError = false
                    });

                    i++;
                    col++;
                    continue;
                }

                // 5. :
                if (c == ':')
                {
                    tokens.Add(new Token
                    {
                        Code = 21,
                        TokenType = TokenType.TernaryColon,
                        TypeName = "знак тернарного оператора",
                        Lexeme = ":",
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = startCol,
                        StartIndex = startIndex,
                        Length = 1,
                        IsError = false
                    });

                    i++;
                    col++;
                    continue;
                }

                // 6. Разделители
                if (";(),{}".Contains(c))
                {
                    tokens.Add(new Token
                    {
                        Code = 16,
                        TokenType = TokenType.Separator,
                        TypeName = c == ';' ? "конец оператора" : "разделитель",
                        Lexeme = c.ToString(),
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = startCol,
                        StartIndex = startIndex,
                        Length = 1,
                        IsError = false
                    });

                    i++;
                    col++;
                    continue;
                }

                // 7. Операторы
                if ("=+-*/<>!".Contains(c))
                {
                    tokens.Add(new Token
                    {
                        Code = 10,
                        TokenType = TokenType.Operator,
                        TypeName = c == '=' ? "оператор присваивания" : "оператор",
                        Lexeme = c.ToString(),
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = startCol,
                        StartIndex = startIndex,
                        Length = 1,
                        IsError = false
                    });

                    i++;
                    col++;
                    continue;
                }

                // 8. Ошибка
                tokens.Add(new Token
                {
                    Code = -1,
                    TokenType = TokenType.Error,
                    TypeName = "ошибка: недопустимый символ",
                    Lexeme = c.ToString(),
                    Line = startLine,
                    StartColumn = startCol,
                    EndColumn = startCol,
                    StartIndex = startIndex,
                    Length = 1,
                    IsError = true
                });

                i++;
                col++;
            }

            return tokens;
        }

        private bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        private string MakeWhitespaceVisible(string text)
        {
            return text
                .Replace(" ", "(пробел)")
                .Replace("\t", "(tab)")
                .Replace("\r", "(CR)")
                .Replace("\n", "(LF)");
        }
    }
}