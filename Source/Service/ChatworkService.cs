using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Extensions;
using ChatworkJobTrigger.Chatwork;

namespace ChatworkJobTrigger
{
    public sealed class ChatworkService : Singleton<ChatworkService>
    {
        //----- params -----

        private const int FetchIntervalSeconds = 5;

        private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        //----- field -----
        
        private ChatworkClient client = null;

        private DateTime fetchTime = default;

        private DateTime nextFetchTime = default;

        //----- property -----

        public AccountData MyAccount { get; private set; }

        //----- method -----

        private ChatworkService(){ }

        public async Task Initialize(CancellationToken cancelToken)
        {
            Console.WriteLine("ChatWorkService");

            var setting = Setting.Instance;

            client = new ChatworkClient(setting.ChatworkRoomId, setting.ChatworkApiKey);

            await GetMyAccountData(cancelToken);

            fetchTime = DateTime.Now;
            nextFetchTime = fetchTime;
        }

        public async Task GetMyAccountData(CancellationToken cancelToken)
        {
            var json = await client.GetMyAccount(cancelToken);

            MyAccount = JsonConvert.DeserializeObject<AccountData>(json);
        }

        public async Task<MessageData[]> Fetch(CancellationToken cancelToken)
        {
            var result = new MessageData[0];

            var time = DateTime.Now;

            if (time < nextFetchTime){ return result; }

            var json = await client.GetMessage(cancelToken, true);

            if (!string.IsNullOrEmpty(json))
            {
                var unixTime = (long)fetchTime.ToUniversalTime().Subtract(UNIX_EPOCH).TotalSeconds;

                var messages = JsonConvert.DeserializeObject<MessageData[]>(json);
            
                // 前回取得後以降に投稿・更新された対象にフィルタリング.
                result = messages.Where(x =>　unixTime <= x.send_time).ToArray();
            }

            fetchTime = time;
            nextFetchTime = time.AddSeconds(FetchIntervalSeconds);

            return result;
        }

        public string GetReplyStr(MessageData messageData)
        {
            var setting = Setting.Instance;

            var aid = messageData.account.account_id;
            var roomId = setting.ChatworkRoomId;
            var messageId = messageData.message_id;

            var message = $"[rp aid={aid} to={roomId}-{messageId}]\n";

            return message;
        }

        public async Task SendMessage(string message, CancellationToken cancelToken)
        {
            await client.SendMessage(message, cancelToken);
        }

        public async Task SendFile(string filePath, string message, string displayName, CancellationToken cancelToken)
        {
            await client.SendFile(filePath, message, displayName, cancelToken);
        }
    }
}
