namespace TextEditorLab.Models
{
    public class SemanticError
    {
        public string Message { get; set; } = "";
        public string Fragment { get; set; } = "";
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
    }
}