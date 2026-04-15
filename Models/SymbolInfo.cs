namespace TextEditorLab.Models
{
    public enum ValueType
    {
        Int,
        Bool,
        Byte,
        Unknown
    }

    public class SymbolInfo
    {
        public string Name { get; set; } = "";
        public ValueType Type { get; set; } = ValueType.Unknown;
        public bool IsConst { get; set; }
        public int ScopeLevel { get; set; }
    }
}