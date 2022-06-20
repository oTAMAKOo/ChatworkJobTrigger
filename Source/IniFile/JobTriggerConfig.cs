
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class JobTriggerConfig : IniFile<JobTriggerConfig>
    {
        //----- params -----

        private const string BuildSection = "Build";

        private const string JobNameFormatField = "JobNameFormat";
        private const string CategoryField = "Category";
        private const string PlatformField = "Platform";

        private const string MasterSection = "Master";

        private const string ResourceSection = "Resource";

        //----- field -----
        
        //----- property -----

        public override string FileName { get { return "trigger"; } }

        public string BuildJobNameFormat { get { return GetData<string>(BuildSection, JobNameFormatField); } }
        public string BuildCategory { get { return GetData<string>(BuildSection, CategoryField); } }
        public string BuildPlatform { get { return GetData<string>(BuildSection, PlatformField); } }
        
        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[BuildSection][JobNameFormatField] = "jobname-#TYPE#-#PLATFORM#";
            data[BuildSection][CategoryField] = "development[dev, develop],staging[stg], production[prod]";
            data[BuildSection][PlatformField] = "ios, android";
            
            /*
            data[ProjectSection][ArgumentNamesRangeFieldName] = "シート1 Data!A2:E";
            data[ProjectSection][ArgumentCandidateRangeFieldName] = "シート1 Data!A2:E";
            data[ProjectSection][JobNameRangeFieldName] = "シート1 Data!A2:E";
            */
        }
    }
}
