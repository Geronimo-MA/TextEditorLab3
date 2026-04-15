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
                        Code = 3,
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

                // 2. Идентификатор или логический оператор-слово
                if (char.IsLetter(c) || c == '_')
                {
                    string lexeme = "";

                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    {
                        lexeme += text[i];
                        i++;
                        col++;
                    }

                    if (lexeme == "and" || lexeme == "or" || lexeme == "not")
                    {
                        tokens.Add(new Token
                        {
                            Code = 4,
                            TokenType = TokenType.Operator,
                            TypeName = "логический оператор",
                            Lexeme = lexeme,
                            Line = startLine,
                            StartColumn = startCol,
                            EndColumn = col - 1,
                            StartIndex = startIndex,
                            Length = i - startIndex,
                            IsError = false
                        });
                    }
                    else
                    {
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
                    }

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
                        Code = 5,
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
                        Code = 6,
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

                // 6. ;
                if (c == ';')
                {
                    tokens.Add(new Token
                    {
                        Code = 7,
                        TokenType = TokenType.Semicolon,
                        TypeName = "конец оператора",
                        Lexeme = ";",
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

                // 7. (
                if (c == '(')
                {
                    tokens.Add(new Token
                    {
                        Code = 8,
                        TokenType = TokenType.LeftParen,
                        TypeName = "открывающая скобка",
                        Lexeme = "(",
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

                // 8. )
                if (c == ')')
                {
                    tokens.Add(new Token
                    {
                        Code = 9,
                        TokenType = TokenType.RightParen,
                        TypeName = "закрывающая скобка",
                        Lexeme = ")",
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

                // 9. Операторы
                if ("=<>!&|".Contains(c))
                {
                    string lexeme;

                    if (i + 1 < text.Length)
                    {
                        string twoChar = text.Substring(i, 2);

                        if (twoChar == ">=" || twoChar == "<=" || twoChar == "==" ||
                            twoChar == "!=" || twoChar == "&&" || twoChar == "||")
                        {
                            lexeme = twoChar;
                            i += 2;
                            col += 2;
                        }
                        else
                        {
                            lexeme = c.ToString();
                            i++;
                            col++;
                        }
                    }
                    else
                    {
                        lexeme = c.ToString();
                        i++;
                        col++;
                    }

                    tokens.Add(new Token
                    {
                        Code = 4,
                        TokenType = TokenType.Operator,
                        TypeName = GetOperatorTypeName(lexeme),
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

                // 10. Ошибка
                string errorLexeme = "";

                while (i < text.Length && !IsValidChar(text[i]))
                {
                    errorLexeme += text[i];
                    i++;
                    col++;
                }

                tokens.Add(new Token
                {
                    Code = -1,
                    TokenType = TokenType.Error,
                    TypeName = "ошибка: недопустимый символ",
                    Lexeme = errorLexeme,
                    Line = startLine,
                    StartColumn = startCol,
                    EndColumn = col - 1,
                    StartIndex = startIndex,
                    Length = i - startIndex,
                    IsError = true
                });
            }

            return tokens;
        }

        private bool IsWhitespace(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        private bool IsValidChar(char c)
        {
            return char.IsLetterOrDigit(c)
                || c == '_'
                || IsWhitespace(c)
                || c == '?'
                || c == ':'
                || c == ';'
                || c == '('
                || c == ')'
                || "=<>!&|".Contains(c);
        }

        private string GetOperatorTypeName(string lexeme)
        {
            return lexeme switch
            {
                "=" => "оператор присваивания",
                ">" or "<" or ">=" or "<=" or "==" or "!=" => "оператор отношения",
                "and" or "or" or "not" or "&&" or "||" or "!" => "логический оператор",
                _ => "оператор"
            };
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