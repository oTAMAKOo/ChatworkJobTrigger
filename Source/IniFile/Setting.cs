
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class Setting : IniFile<Setting>
    {
        //----- params -----

        private const string ChatworkSection = "Chatwork";
        
        private const string ChatworkApiKeyField = "ApiKey";
        private const string ChatworkRoomIdField = "RoomId";

        private const string SpreadsheetSection = "Spreadsheet";

        private const string SpreadsheetIdField = "SpreadsheetId";

        private const string JenkinsSection = "Jenkins";

        private const string JenkinsBaseUrlField = "BaseUrl";
        private const string JenkinsUserNameField = "UserName";
        private const string JenkinsApiTokenField = "ApiToken";

        //----- field -----
        
        //----- property -----

        public override string FileName { get { return "setting"; } }
        
        public string ChatworkApiKey { get { return GetData<string>(ChatworkSection, ChatworkApiKeyField); } }
        public string ChatworkRoomId { get { return GetData<string>(ChatworkSection, ChatworkRoomIdField); } }
        
        public string SpreadsheetId { get { return GetData<string>(SpreadsheetSection, SpreadsheetIdField); } }
        
        public string JenkinsBaseUrl { get { return GetData<string>(JenkinsSection, JenkinsBaseUrlField); } }
        public string JenkinsUserName { get { return GetData<string>(JenkinsSection, JenkinsUserNameField); } }
        public string JenkinsApiToken { get { return GetData<string>(JenkinsSection, JenkinsApiTokenField); } }
        
        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[ChatworkSection][ChatworkApiKey] = "ABCDEFG123456789";
            data[ChatworkSection][ChatworkRoomId] = "0123456789";
            
            data[SpreadsheetSection][SpreadsheetIdField] = "ABCDEFG123456789";

            data[JenkinsSection][JenkinsBaseUrlField] = "http://localhost:8080/";
            data[JenkinsSection][JenkinsUserNameField] = "admin";
            data[JenkinsSection][JenkinsApiTokenField] = "ABCDEFG123456789";
        }
    }
}
