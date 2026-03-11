namespace TextEditorLab.Models
{
    public class Token
    {
        public int Code { get; set; }
        public TokenType TokenType { get; set; }
        public string TypeName { get; set; } = "";
        public string Lexeme { get; set; } = "";
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public bool IsError { get; set; }

        public string Location => $"строка {Line}, {StartColumn}-{EndColumn}";
    }
}