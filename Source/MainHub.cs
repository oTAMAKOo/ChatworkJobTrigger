
using Extensions;
using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ChatworkJobTrigger
{
    public sealed class MainHub
    {
        //----- params -----
        
        //----- field -----

        private DateTime nextGCExecute = DateTime.MinValue;

        //----- property -----

        //----- method -----

        public async Task Initialize(CancellationToken cancelToken)
        {
            Console.WriteLine("\n------ Initialize ----------------\n");

            // IniFiles.

            var textDefine = TextDefine.CreateInstance();

            await textDefine.Load();

            var setting = Setting.CreateInstance();

            await setting.Load();

            // SSL.

            //ServicePointManager.Expect100Continue = false;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = OnRemoteCertificateValidationCallback;

            // ChatWork.

            var chatworkService = ChatworkService.CreateInstance();

            await chatworkService.Initialize(cancelToken);

            // Jenkins.

            var jenkinsService = JenkinsService.CreateInstance();

            await jenkinsService.Initialize();

            // JobTrigger.

            var jobTriggerService = JobTriggerService.CreateInstance();

            await jobTriggerService.Initialize();

            ConsoleUtility.Separator();

            // GC.

            nextGCExecute = DateTime.Now.AddSeconds(60);
        }

        public async Task Update(CancellationToken cancelToken)
        {
            var jobTriggerService = JobTriggerService.Instance;
            var chatworkService = ChatworkService.Instance;

            try
            {
                // 新規メッセージ取得.
                var messages = await chatworkService.Fetch(cancelToken);

                // ジョブトリガー実行.
                if (messages.Any())
                {
                    foreach (var message in messages)
                    {
                        if(!jobTriggerService.IsTriggerMessage(message)){ continue; }

                        jobTriggerService.InvokeTrigger(message, cancelToken).Forget();
                    }
                }

                var now = DateTime.Now;

                if (nextGCExecute < now)
                {
                    nextGCExecute = now.AddSeconds(30);

                    GC.Collect();
                }
            }
            catch (Exception e)
            {
                ConsoleUtility.Separator();

                Console.WriteLine(e);
                
                ConsoleUtility.Separator();
            }
        }

        // 信頼できないSSL証明書を「問題なし」にするメソッド
        private bool OnRemoteCertificateValidationCallback(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; // 「SSL証明書の使用は問題なし」と示す
        }
    }
}
