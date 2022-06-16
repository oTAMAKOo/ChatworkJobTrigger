
using System.Collections.Generic;
using System.Linq;
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class JenkinsConfig : ConfigBase<JenkinsConfig>
    {
        //----- params -----

        private const string ApiSection = "API";

        private const string BaseUrlFieldName = "BaseUrl";
        private const string UserNameFieldName = "UserName";
        private const string ApiTokenFieldName = "ApiToken";

        //----- field -----
        
        //----- property -----

        public override string ConfigIniName { get { return "jenkins.ini"; } }

        public string BaseUrl { get { return GetData<string>(ApiSection, BaseUrlFieldName); } }
        
        public string UserName { get { return GetData<string>(ApiSection, UserNameFieldName); } }

        public string ApiToken { get { return GetData<string>(ApiSection, ApiTokenFieldName); } }
        
        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[ApiSection][BaseUrlFieldName] = "http://localhost:8080/";
            data[ApiSection][UserNameFieldName] = "admin";
            data[ApiSection][ApiTokenFieldName] = "0123456789ABCDEF";
        }
    }
}
