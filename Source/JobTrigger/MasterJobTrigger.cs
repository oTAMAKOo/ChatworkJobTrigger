using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Extensions;

namespace ChatworkJenkinsBot
{
    public sealed class MasterJobTrigger : JobTrigger<MasterJobTrigger>
    {
        //----- params -----

        public sealed class ArgumentDefine
        {
            /// <summary>  </summary>
            public string Name { get; set; }
            /// <summary>  </summary>
            public string[] NamePatterns { get; set; }
            /// <summary>  </summary>
            public string DefaultValue { get; set; }
            /// <summary>  </summary>
            public bool Require { get; set; }
        }

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "master"; } }

        //----- method -----

        protected override string GetJobName(string[] arguments)
        {
            return "";
        }

        protected override Dictionary<string, string> GetJobParameters(string[] arguments)
        {
            return new Dictionary<string, string>();
        }
    }
}
