
using IniFileParser.Model;

namespace ChatworkJobTrigger
{
    public sealed class Setting : IniFile<Setting>
    {
        //----- params -----

        private const string ChatworkSection = "Chatwork";
        
        private const string ChatworkApiKeyField = "ApiKey";
        private const string ChatworkRoomIdField = "RoomId";

        private const string JenkinsSection = "Jenkins";

        private const string JenkinsBaseUrlField = "BaseUrl";
        private const string JenkinsUserNameField = "UserName";
        private const string JenkinsApiTokenField = "ApiToken";
        private const string JenkinsLogFilePathField = "LogFilePath";

        private const string JenkinsBuildTimeoutField = "BuildTimeout";
        private const string JenkinsQueueTimeoutField = "QueueTimeout";

        private const string CommandSection = "Command";

        private const string CommandDefineField = "Define";

        //----- field -----
        
        //----- property -----

        public override string FileName { get { return "setting"; } }
        
        public string ChatworkApiKey { get { return GetData<string>(ChatworkSection, ChatworkApiKeyField); } }
        public string ChatworkRoomId { get { return GetData<string>(ChatworkSection, ChatworkRoomIdField); } }
        
        public string JenkinsBaseUrl { get { return GetData<string>(JenkinsSection, JenkinsBaseUrlField); } }
        public string JenkinsUserName { get { return GetData<string>(JenkinsSection, JenkinsUserNameField); } }
        public string JenkinsApiToken { get { return GetData<string>(JenkinsSection, JenkinsApiTokenField); } }
        public string JenkinsLogFilePath { get { return GetData<string>(JenkinsSection, JenkinsLogFilePathField); } }
        
        public int JenkinsBuildTimeout { get { return GetData<int>(JenkinsSection, JenkinsBuildTimeoutField, 3600); } }
        public int JenkinsQueueTimeout { get { return GetData<int>(JenkinsSection, JenkinsQueueTimeoutField, 3600 * 2); } }

        public string Commands { get { return GetData<string>(CommandSection, CommandDefineField); } }
        
        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[ChatworkSection][ChatworkApiKey] = "ABCDEFG123456789";
            data[ChatworkSection][ChatworkRoomId] = "0123456789";
            
            data[JenkinsSection][JenkinsBaseUrlField] = "http://localhost:8080/";
            data[JenkinsSection][JenkinsUserNameField] = "admin";
            data[JenkinsSection][JenkinsApiTokenField] = "ABCDEFG123456789";
            data[JenkinsSection][JenkinsLogFilePathField] = "/Users/<UserName>/.jenkins/jobs/#JOB_NAME#/builds/#BUILD_NUMBER#/log";

            data[JenkinsSection][JenkinsBuildTimeoutField] = "3600";
            data[JenkinsSection][JenkinsQueueTimeoutField] = "7200";

            data[CommandSection][CommandDefineField] = "build, master";
        }
    }
}
