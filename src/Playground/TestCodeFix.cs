using System.Collections.Generic;
using Generator.Equals;

namespace Playground
{
    [Equatable]
    public partial class TestCodeFix
    {
        // This property should trigger GE001 and offer code fixes
        public List<string> TestList { get; set; } = new();

        // This property should trigger GE001 and offer code fixes
        public Dictionary<string, int> TestDict { get; set; } = new();
    }
}