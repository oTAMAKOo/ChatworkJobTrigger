using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatworkJobTrigger
{
    namespace Chatwork
    {
        [Serializable]
        public sealed class AccountData
        {
            public ulong account_id;
            public string name;
            public string avatar_image_url;
        }

        [Serializable]
        public sealed class MessageData
        {
            public ulong message_id;
            public AccountData account;
            public string body;
            public long send_time;
            public long update_time;
        }
    }

    public class ChatworkClient
    {
        //----- params -----

        private const string ApiUrl = "https://api.chatwork.com/v2/";

        private const int MaxRetryCount = 3;

        //----- field -----

        private HttpClient httpClient = null;

        private string processRequestId = null;

        private Queue<string> requestQueue = null;

        //----- property -----

        public string RoomId { get; private set; }

        public string ApiToken { get; private set; }

        //----- method -----

        public ChatworkClient(string roomId, string apiToken)
        {
            RoomId = roomId;
            ApiToken = apiToken;

            httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            httpClient.DefaultRequestHeaders.Add("X-ChatWorkToken", ApiToken);

            requestQueue = new Queue<string>();
        }

        public async Task<string> GetMyAccount(CancellationToken cancelToken)
        {
            Func<HttpRequestMessage> requestGenerator = () => new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = GetRequestUri($"me", false),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "X-ChatWorkToken", ApiToken},
                },
            };
            
            var result = await SendAsync(requestGenerator, cancelToken);

            return result;
        }

        public async Task<string> GetMessage(CancellationToken cancelToken, bool force = false)
        {
            var forceFlag = force ? 1 : 0;

            Func<HttpRequestMessage> requestGenerator = () => new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = GetRequestUri($"messages?force={forceFlag}"),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "X-ChatWorkToken", ApiToken },
                },
            };

            var result = await SendAsync(requestGenerator, cancelToken);

            return result;
        }

        public async Task<string> SendMessage(string message, CancellationToken cancelToken, bool selfUnRead = false)
        {
            var body = $"?body={ Uri.EscapeDataString(message)}&self_unread={(selfUnRead ? 1 : 0)}";

            Func<HttpRequestMessage> requestGenerator = () => new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = GetRequestUri("messages" + body),
                Headers =
                {
                    { "Accept", "application/json" },
                    { "X-ChatWorkToken", ApiToken },
                },
            };

            var result = await SendAsync(requestGenerator, cancelToken);

            return result;
        }

        public async Task<string> SendFile(string filePath, string message = null, string displayName = null, CancellationToken cancelToken = default)
        {
            if (!File.Exists(filePath)){ return null; }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = Path.GetFileName(filePath);
            }

            var result = string.Empty;

            using (var multipart = new MultipartFormDataContent("---boundary---"))
            {
                // ファイル.

                var fileContent = new StreamContent(File.OpenRead(filePath));

                fileContent.Headers.Add("Content-Disposition", $@"form-data; name=""file""; filename=""{displayName}""");

                multipart.Add(fileContent);

                // メッセージ.

                if (!string.IsNullOrEmpty(message))
                {
                    var messageContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(message)));

                    messageContent.Headers.Add("Content-Disposition", $@"form-data; name=""message""");

                    multipart.Add(messageContent);
                }

                // 送信.

                var requestUrl = GetRequestUri("files");

                result = await PostAsync(requestUrl, multipart, cancelToken);
            }
 
            return result;
        }

        private async Task<string> SendAsync(Func<HttpRequestMessage> requestGenerator, CancellationToken cancelToken)
        {
            var result = string.Empty;

            var retryCount = 0;

            var requestId = GetRequestId();

            await WaitRequestQueue(requestId);

            try
            {
                while (retryCount < MaxRetryCount)
                {
                    try
                    {
                        var requestMessage = requestGenerator.Invoke();

                        using (var response = await httpClient.SendAsync(requestMessage, cancelToken))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = await response.Content.ReadAsStringAsync(cancelToken);
                            }
                            else
                            {
                                retryCount++;
                            }
                        }

                        if (!string.IsNullOrEmpty(result)){ break; }
                    }
                    catch (TimeoutException)
                    {
                        retryCount++;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(3f), cancelToken);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                RequestFinish(requestId);
            }

            return result;
        }

        private async Task<string> PostAsync(Uri requestUrl, MultipartFormDataContent multipart, CancellationToken cancelToken)
        {
            var result = string.Empty;

            var retryCount = 0;

            var requestId = GetRequestId();

            await WaitRequestQueue(requestId);

            try
            {
                while (retryCount < MaxRetryCount)
                {
                    try
                    {
                        using (var response = await httpClient.PostAsync(requestUrl, multipart, cancelToken))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                result = await response.Content.ReadAsStringAsync(cancelToken);
                            }
                            else
                            {
                                retryCount++;
                            }
                        }

                        if (!string.IsNullOrEmpty(result)){ break; }
                    }
                    catch (TimeoutException)
                    {
                        retryCount++;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(3f), cancelToken);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                RequestFinish(requestId);
            }

            return result;
        }

        private Uri GetRequestUri(string url, bool room = true)
        {
            var requestUrl = ApiUrl;

            if (room)
            {
                requestUrl += $"rooms/{RoomId}/";
            }

            requestUrl += url;

            return new Uri(requestUrl);
        }

        private string GetRequestId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private async Task WaitRequestQueue(string requestId)
        {
            requestQueue.Enqueue(requestId);

            while (true)
            {
                if (string.IsNullOrEmpty(processRequestId))
                {
                    processRequestId = requestQueue.Dequeue();
                }

                if (processRequestId == requestId){ break; }

                await Task.Delay(TimeSpan.FromSeconds(1f));
            }
        }

        private void RequestFinish(string requestId)
        {
            if (processRequestId == requestId)
            {
                processRequestId = null;
            }
        }
    }
}
