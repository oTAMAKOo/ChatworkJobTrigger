
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class JobTriggerConfig : IniFile<JobTriggerConfig>
    {
        //----- params -----
        
        private const string BuildSection = "Build";
        private const string MasterSection = "Master";
        private const string ResourceSection = "Resource";

        private const string JobNameFormatField = "JobNameFormat";
        private const string BuildTypeField = "BuildType";
        private const string PlatformField = "Platform";
        private const string ServerField = "Server";

        //----- field -----
        
        //----- property -----

        public override string FileName { get { return "trigger"; } }

        public string BuildJobNameFormat { get { return GetData<string>(BuildSection, JobNameFormatField); } }
        public string BuildType { get { return GetData<string>(BuildSection, BuildTypeField); } }
        public string BuildPlatform { get { return GetData<string>(BuildSection, PlatformField); } }
        
        public string MasterJobNameFormat { get { return GetData<string>(MasterSection, JobNameFormatField); } }
        public string MasterServer { get { return GetData<string>(MasterSection, ServerField); } }

        public string ResourceJobNameFormat { get { return GetData<string>(ResourceSection, JobNameFormatField); } }
        public string ResourceServer { get { return GetData<string>(ResourceSection, ServerField); } }
        public string ResourcePlatform { get { return GetData<string>(ResourceSection, PlatformField); } }
        
        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[BuildSection][JobNameFormatField] = "jobname-#TYPE#-#PLATFORM#";
            data[BuildSection][BuildTypeField] = "development[dev, develop],staging[stg], production[prod]";
            data[BuildSection][PlatformField] = "ios, android";

            data[MasterSection][JobNameFormatField] = "jobname-master";
            data[MasterSection][ServerField] = "dev1, dev2, dev3, staging, production";

            data[ResourceSection][JobNameFormatField] = "jobname-resource-#TYPE#-#PLATFORM#";
            data[ResourceSection][ServerField] = "dev1, dev2, dev3, staging, production";
            data[ResourceSection][PlatformField] = "ios, android";

            /*
            data[ProjectSection][ArgumentNamesRangeFieldName] = "シート1 Data!A2:E";
            */
        }
    }
}
