
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class JenkinsConfig : ConfigBase
    {
        //----- params -----

        private const string MainSection = "API";

        //----- field -----

        //----- property -----

        public override string ConfigIniName { get { return "jenkins.ini"; } }

        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {

        }
    }
}
