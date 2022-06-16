
using System.Collections.Generic;
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class ChatworkConfig : ConfigBase<ChatworkConfig>
    {
        //----- params -----

        private const string ApiSection = "API";

        private const string ApiKeyFieldName = "ApiKey";
        private const string RoomIdFieldName = "RoomId";

        // To付ける対象ID, 引数1の名前、引数2の名前、引数3の名前
        // 引数名:引数で順番飛ばしで引数定義できるようにする

        //----- field -----

        //----- property -----

        public override string ConfigIniName { get { return "chatwork.ini"; } }

        public string ApiKey { get { return GetData<string>(ApiSection, ApiKeyFieldName); } }

        public long RoomId { get { return  GetData<long>(ApiSection, RoomIdFieldName); } }

        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            // API.

            data[ApiSection][ApiKeyFieldName] = "---";
            data[ApiSection][RoomIdFieldName] = "---";
        }
    }
}
