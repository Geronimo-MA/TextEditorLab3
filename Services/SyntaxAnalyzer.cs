using System.Collections.Generic;
using TextEditorLab.Models;

namespace TextEditorLab.Services
{
    public class SyntaxAnalyzer
    {
        private List<Token> _tokens = new();
        private int _position;
        private SyntaxAnalysisResult _result = new();

        public SyntaxAnalysisResult Analyze(List<Token> tokens)
        {
            _tokens = tokens ?? new List<Token>();
            _position = 0;
            _result = new SyntaxAnalysisResult();

            if (_tokens.Count == 0)
            {
                AddErrorFromCurrent("Пустая строка. Ожидалась конструкция тернарного оператора.");
                return _result;
            }

            ParseStatement();

            if (!IsAtEnd())
            {
                while (!IsAtEnd())
                {
                    AddErrorFromCurrent("Лишний фрагмент после завершения корректной конструкции.");
                    _position++;
                }
            }

            return _result;
        }

        private void ParseStatement()
        {
            if (!TryParseIdentifier("Ожидался идентификатор в начале выражения."))
            {
                SkipToEndOfStatement();
                return;
            }

            ParseWhitespace("Ожидался пробел после идентификатора.");

            if (!TryExpectOperator("=", "Ожидался оператор присваивания '='."))
            {
                SkipToEndOfStatement();
                return;
            }

            ParseWhitespace("Ожидался пробел после '='.");

            if (!ParseTernaryExpression())
            {
                SkipToEndOfStatement();
                return;
            }

            ExpectCode(7, "Ожидалась ';' в конце выражения.");
        }

        private bool ParseTernaryExpression()
        {
            if (!ParseCondition())
                return false;

            ParseWhitespace("Ожидался пробел перед '?'.");

            if (!TryExpectCode(5, "Ожидался знак '?'."))
                return false;

            ParseWhitespace("Ожидался пробел после '?'.");

            if (!TryParseOperand("Ожидался операнд после '?'."))
                return false;

            ParseWhitespace("Ожидался пробел перед ':'.");

            if (!TryExpectCode(6, "Ожидался знак ':'."))
                return false;

            ParseWhitespace("Ожидался пробел после ':'.");

            if (!TryParseOperand("Ожидался операнд после ':'."))
                return false;

            return true;
        }

        private bool ParseCondition()
        {
            if (!TryParseOperand("Ожидался левый операнд условия."))
                return false;

            ParseWhitespace("Ожидался пробел после левого операнда условия.");

            if (!TryParseRelationOperator("Ожидался оператор отношения (>, <, >=, <=, ==, !=)."))
                return false;

            ParseWhitespace("Ожидался пробел после оператора отношения.");

            if (!TryParseOperand("Ожидался правый операнд условия."))
                return false;

            return true;
        }

        private void ParseIdentifier(string errorMessage)
        {
            if (MatchCode(2))
                return;

            AddErrorFromCurrent(errorMessage);
            RecoverToNextRelevantToken();
        }

        private void ParseWhitespace(string errorMessage)
        {
            if (MatchCode(3))
                return;

            AddErrorFromCurrent(errorMessage);
        }

        private void ParseRelationOperator(string errorMessage)
        {
            if (Current() is Token token &&
                token.Code == 4 &&
                (token.Lexeme == ">" ||
                 token.Lexeme == "<" ||
                 token.Lexeme == ">=" ||
                 token.Lexeme == "<=" ||
                 token.Lexeme == "==" ||
                 token.Lexeme == "!="))
            {
                _position++;
                return;
            }

            AddErrorFromCurrent(errorMessage);
            RecoverToRelationBoundary();
        }

        private void ExpectOperator(string lexeme, string errorMessage)
        {
            if (Current() is Token token && token.Code == 4 && token.Lexeme == lexeme)
            {
                _position++;
                return;
            }

            AddErrorFromCurrent(errorMessage);
            RecoverToLexeme(lexeme);
        }

        private void ExpectCode(int code, string errorMessage)
        {
            if (MatchCode(code))
                return;

            AddErrorFromCurrent(errorMessage);
            RecoverToCode(code);
        }

        private bool MatchCode(int code)
        {
            if (Current()?.Code == code)
            {
                _position++;
                return true;
            }

            return false;
        }

        private Token? Current()
        {
            return _position < _tokens.Count ? _tokens[_position] : null;
        }

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count;
        }

        private void AddErrorFromCurrent(string description)
        {
            if (IsAtEnd())
            {
                _result.Errors.Add(new SyntaxError
                {
                    InvalidFragment = "<конец строки>",
                    Line = _tokens.Count > 0 ? _tokens[^1].Line : 1,
                    StartColumn = _tokens.Count > 0 ? _tokens[^1].EndColumn + 1 : 1,
                    EndColumn = _tokens.Count > 0 ? _tokens[^1].EndColumn + 1 : 1,
                    StartIndex = _tokens.Count > 0 ? _tokens[^1].StartIndex + _tokens[^1].Length : 0,
                    Length = 1,
                    Description = description
                });

                return;
            }

            var token = _tokens[_position];

            _result.Errors.Add(new SyntaxError
            {
                InvalidFragment = token.Lexeme,
                Line = token.Line,
                StartColumn = token.StartColumn,
                EndColumn = token.EndColumn,
                StartIndex = token.StartIndex,
                Length = token.Length > 0 ? token.Length : 1,
                Description = description
            });
        }

        private void RecoverToLexeme(string lexeme)
        {
            while (!IsAtEnd())
            {
                var token = Current();
                if (token != null && token.Lexeme == lexeme)
                {
                    _position++;
                    return;
                }

                if (token != null && token.Code == 7)
                    return;

                _position++;
            }
        }

        private void RecoverToCode(int code)
        {
            while (!IsAtEnd())
            {
                var token = Current();
                if (token != null && token.Code == code)
                {
                    _position++;
                    return;
                }

                _position++;
            }
        }

        private void RecoverToRelationBoundary()
        {
            while (!IsAtEnd())
            {
                var token = Current();
                if (token == null)
                    return;

                if (token.Code == 3 || token.Code == 5 || token.Code == 6 || token.Code == 7)
                    return;

                if (token.Code == 4 &&
                    (token.Lexeme == ">" || token.Lexeme == "<" ||
                     token.Lexeme == ">=" || token.Lexeme == "<=" ||
                     token.Lexeme == "==" || token.Lexeme == "!="))
                {
                    _position++;
                    return;
                }

                _position++;
            }
        }

        private void RecoverToOperandBoundary()
        {
            while (!IsAtEnd())
            {
                var token = Current();
                if (token == null)
                    return;

                if (token.Code == 1 || token.Code == 2)
                {
                    _position++;
                    return;
                }

                if (token.Code == 5 || token.Code == 6 || token.Code == 7)
                    return;

                _position++;
            }
        }

        private void RecoverToNextRelevantToken()
        {
            while (!IsAtEnd())
            {
                var token = Current();
                if (token == null)
                    return;

                if (token.Code == 2 || token.Code == 3 || token.Code == 4 || token.Code == 5 || token.Code == 6 || token.Code == 7)
                    return;

                _position++;
            }
        }
        private void SkipToEndOfStatement()
        {
            while (!IsAtEnd())
            {
                var token = Current();
                if (token == null)
                    return;

                if (token.Code == 7)
                {
                    _position++;
                    return;
                }

                _position++;
            }
        }
    private bool TryParseIdentifier(string errorMessage)
        {
            if (MatchCode(2))
                return true;

            AddErrorFromCurrent(errorMessage);
            return false;
        }

        private bool TryParseOperand(string errorMessage)
        {
            if (MatchCode(2) || MatchCode(1))
                return true;

            AddErrorFromCurrent(errorMessage);
            return false;
        }

        private bool TryParseRelationOperator(string errorMessage)
        {
            if (Current() is Token token &&
                token.Code == 4 &&
                (token.Lexeme == ">" ||
                 token.Lexeme == "<" ||
                 token.Lexeme == ">=" ||
                 token.Lexeme == "<=" ||
                 token.Lexeme == "==" ||
                 token.Lexeme == "!="))
            {
                _position++;
                return true;
            }

            AddErrorFromCurrent(errorMessage);
            return false;
        }

        private bool TryExpectOperator(string lexeme, string errorMessage)
        {
            if (Current() is Token token && token.Code == 4 && token.Lexeme == lexeme)
            {
                _position++;
                return true;
            }

            AddErrorFromCurrent(errorMessage);
            return false;
        }

        private bool TryExpectCode(int code, string errorMessage)
        {
            if (MatchCode(code))
                return true;

            AddErrorFromCurrent(errorMessage);
            return false;
        }

    }
}
