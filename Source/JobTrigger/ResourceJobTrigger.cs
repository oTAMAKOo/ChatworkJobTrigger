using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Extensions;

namespace ChatworkJenkinsBot
{
    public sealed class ResourceJobTrigger : JobTrigger<ResourceJobTrigger>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public override string CommandName { get { return "resource"; } }

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
