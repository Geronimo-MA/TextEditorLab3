namespace TextEditorLab.Models
{
    public class SyntaxError
    {
        public string InvalidFragment { get; set; } = "";
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string Description { get; set; } = "";

        public string Location => $"строка {Line}, {StartColumn}-{EndColumn}";
    }
}