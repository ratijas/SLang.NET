using System.Collections.Generic;

namespace SLang.IR.JSON
{
    public class JsonEntity
    {
        public string Type { get; set; }
        public List<JsonEntity> Children { get; set; }
        public string Value { get; set; }
    }
}