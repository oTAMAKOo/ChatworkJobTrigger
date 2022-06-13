using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatworkJenkinsBot
{
    public class ChatworkClient
    {
        //----- params -----

        private const string ApiUrl = "https://api.chatwork.com/v2/";

        [Serializable]
        public sealed class MessageData
        {
            public ulong message_id;
            public AccountData account;
            public string body;
            public long send_time;
            public long update_time;
        }

        [Serializable]
        public sealed class AccountData
        {
            public ulong account_id;
            public string name;
            public string avatar_image_url;
        }

        //----- field -----

        //----- property -----

        public long RoomId { get; private set; }

        public string ApiToken { get; private set; }

        //----- method -----

        public ChatworkClient(long roomId, string apiToken)
        {
            RoomId = roomId;
            ApiToken = apiToken;
        }

        public async Task<string> GetMessage(bool force = false)
        {
            var result = string.Empty;

            var client = new HttpClient();

            var forceFlag = force ? 1 : 0;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = GetRequestUri($"messages?force={forceFlag}"),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "X-ChatWorkToken", ApiToken },
                },
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        result = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        Console.WriteLine(response.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return result;
        }

        public async Task<string> SendMessage(string message, bool selfUnRead = false)
        {
            var result = string.Empty;

            var body = $"?body={ Uri.EscapeDataString(message)}&self_unread={(selfUnRead ? 1 : 0)}";

            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = GetRequestUri("messages" + body),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "X-ChatWorkToken", ApiToken },
                },
            };

            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.SendAsync(requestMessage))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        result = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        Console.WriteLine(response.ToString());
                    }
                }
            }

            return result;
        }

        private Uri GetRequestUri(string url)
        {
            return new Uri(string.Format("{0}rooms/{1}/{2}", ApiUrl, RoomId, url));
        }
    }
}
