
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

        //----- field -----
        
        private ChatworkClient client = null;

        private DateTime nextFetchTime = default;

        private FixedQueue<ulong> receivedMessageIds = null;

        //----- property -----

        public AccountData MyAccount { get; private set; }

        //----- method -----

        private ChatworkService(){ }

        public async Task Initialize(CancellationToken cancelToken)
        {
            Console.WriteLine("ChatWorkService");

            var setting = Setting.Instance;

            client = new ChatworkClient(setting.ChatworkRoomId, setting.ChatworkApiKey);

            nextFetchTime = DateTime.Now;

            receivedMessageIds = new FixedQueue<ulong>(150);

            await GetMyAccountData(cancelToken);

            await Fetch(CancellationToken.None);
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

            try
            {
                var json = await client.GetMessage(cancelToken, true);

                if (!string.IsNullOrEmpty(json))
                {
                    var messages = JsonConvert.DeserializeObject<MessageData[]>(json);
            
                    // 取得済み一覧に存在しないメッセージを新規として扱う.
                    result = messages.Where(x => receivedMessageIds.All(y => y != x.message_id)).ToArray();

                    foreach (var message in messages)
                    {
                        receivedMessageIds.Enqueue(message.message_id);
                    }
                }
                
                nextFetchTime = time.AddSeconds(FetchIntervalSeconds);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

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
            try
            {
                await client.SendMessage(message, cancelToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task SendFile(string filePath, string message, string displayName, CancellationToken cancelToken)
        {
            try
            {
                await client.SendFile(filePath, message, displayName, cancelToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
