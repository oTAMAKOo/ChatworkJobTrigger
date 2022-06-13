
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class ChatworkConfig : ConfigBase
    {
        //----- params -----

        private const string MainSection = "API";

        private const string ApiKeyFieldName = "ApiKey";
        private const string RoomIdFieldName = "RoomId";

        //----- field -----

        //----- property -----

        public override string ConfigIniName { get { return "chatwork.ini"; } }

        public string ApiKey { get { return GetData<string>(MainSection, ApiKeyFieldName); } }

        public long RoomId { get { return  GetData<long>(MainSection, RoomIdFieldName); } }

        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[MainSection][ApiKeyFieldName] = "---";
            data[MainSection][RoomIdFieldName] = "---";
        }
    }
}
