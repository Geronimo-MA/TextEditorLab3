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
                AddErrorAtEnd("Пустая строка. Ожидалась конструкция тернарного оператора.");
                return _result;
            }

            ParseStatement();
            ReportTrailingTokens();

            return _result;
        }

        private void ParseStatement()
        {
            if (!TryParseIdentifier("Ожидался идентификатор в начале выражения."))
            {
                RecoverToStatementEnd();
                return;
            }

            TryParseWhitespace("Ожидался пробел после идентификатора.");

            if (!TryExpectOperator("=", "Ожидался оператор присваивания '='."))
            {
                RecoverToStatementEnd();
                return;
            }

            TryParseWhitespace("Ожидался пробел после '='.");

            bool ternaryOk = ParseTernaryExpression();
            if (!ternaryOk)
            {
                RecoverToStatementEnd();
                return;
            }

            if (!TryExpectCode(7, "Ожидалась ';' в конце выражения."))
            {
                RecoverToStatementEnd();
            }
        }

        private bool ParseTernaryExpression()
        {
            bool conditionOk = ParseCondition();

            if (!conditionOk)
            {
                RecoverToAny("?", ";");
                if (!CurrentCodeIs(5))
                    return false;
            }
            else
            {
                TryParseWhitespace("Ожидался пробел перед '?'.");
            }

            if (!TryExpectCode(5, "Ожидался знак '?'."))
            {
                RecoverToAny(":", ";");
                return false;
            }

            TryParseWhitespace("Ожидался пробел после '?'.");

            bool leftBranchOk = TryParseOperand("Ожидался операнд после '?'.");
            if (!leftBranchOk)
            {
                RecoverToAny(":", ";");
                if (!CurrentCodeIs(6))
                    return false;

                _position++; // :
                TryParseWhitespace("Ожидался пробел после ':'.");
                if (!TryParseOperand("Ожидался операнд после ':'."))
                    return false;

                return true;
            }

            TryParseWhitespace("Ожидался пробел перед ':'.");

            if (!TryExpectCode(6, "Ожидался знак ':'."))
            {
                return false;
            }

            TryParseWhitespace("Ожидался пробел после ':'.");

            if (!TryParseOperand("Ожидался операнд после ':'."))
            {
                return false;
            }

            return true;
        }

        private bool ParseCondition()
        {
            if (!TryParseOperand("Ожидался левый операнд условия."))
                return false;

            TryParseWhitespace("Ожидался пробел после левого операнда условия.");

            if (!TryParseRelationOperator("Ожидался оператор отношения (>, <, >=, <=, ==, !=)."))
                return false;

            TryParseWhitespace("Ожидался пробел после оператора отношения.");

            if (!TryParseOperand("Ожидался правый операнд условия."))
                return false;

            return true;
        }

        private bool TryParseWhitespace(string errorMessage)
        {
            if (MatchCode(3))
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

        private bool TryParseIdentifier(string errorMessage)
        {
            if (MatchCode(2))
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
            if (Current() is Token token &&
                token.Code == 4 &&
                token.Lexeme == lexeme)
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

        private void ReportTrailingTokens()
        {
            while (!IsAtEnd())
            {
                AddErrorFromCurrent("Лишний фрагмент после завершения корректной конструкции.");
                _position++;
            }
        }

        private void RecoverToStatementEnd()
        {
            while (!IsAtEnd())
            {
                if (CurrentCodeIs(7))
                {
                    _position++;
                    return;
                }

                _position++;
            }
        }

        private void RecoverToAny(params string[] lexemes)
        {
            while (!IsAtEnd())
            {
                var token = Current();
                if (token == null)
                    return;

                foreach (var lexeme in lexemes)
                {
                    if (token.Lexeme == lexeme)
                        return;
                }

                _position++;
            }
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

        private bool CurrentCodeIs(int code)
        {
            return Current()?.Code == code;
        }

        private Token? Current()
        {
            return _position < _tokens.Count ? _tokens[_position] : null;
        }

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count;
        }

        private void AddErrorAtEnd(string description)
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
        }

        private void AddErrorFromCurrent(string description)
        {
            if (IsAtEnd())
            {
                AddErrorAtEnd(description);
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
    }
}