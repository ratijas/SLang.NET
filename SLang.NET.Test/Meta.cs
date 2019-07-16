using System.IO;
using Newtonsoft.Json;

namespace SLang.NET.Test
{
    public class Meta
    {
        [JsonProperty(PropertyName = "stages")]
        public _Stages Stages = new _Stages();

        public class _Stages
        {
            [JsonProperty(PropertyName = "parser")]
            public _Parser Parser = new _Parser();

            [JsonProperty(PropertyName = "compiler")]
            public _Compiler Compiler = new _Compiler();

            [JsonProperty(PropertyName = "peverify")]
            public _PeVerify PeVerify = new _PeVerify();

            [JsonProperty(PropertyName = "run")]
            public _Run Run = new _Run();

            public class _Parser
            {
                [JsonProperty(PropertyName = "pass")]
                public bool Pass = true;

                [JsonProperty(PropertyName = "error")]
                public string Error = string.Empty;
            }

            public class _Compiler
            {
                [JsonProperty(PropertyName = "pass")]
                public bool Pass = true;
                
                [JsonProperty(PropertyName = "error")]
                public string Error = string.Empty;
            }

            public class _PeVerify
            {
                [JsonProperty(PropertyName = "pass")]
                public bool Pass = true;

                [JsonProperty(PropertyName = "error")]
                public string Error = string.Empty;
            }

            public class _Run
            {
                [JsonProperty(PropertyName = "run")]
                public bool Run = true;
                
                [JsonProperty(PropertyName = "args")]
                public string[] Args = new string[0];

                [JsonProperty(PropertyName = "exit_code")]
                public int ExitCode = 0;

                [JsonProperty(PropertyName = "output")]
                public string Output = string.Empty;

                [JsonProperty(PropertyName = "error")]
                public string Error = string.Empty;
            }
        }

        public static Meta FromFile(FileInfo file)
        {
            using (var inputStream = file.OpenText())
            {
                using (var reader = new JsonTextReader(inputStream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    return serializer.Deserialize<Meta>(reader);
                }
            }
        }
    }
}