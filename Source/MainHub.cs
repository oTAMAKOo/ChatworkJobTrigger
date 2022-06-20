
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Extensions;

namespace ChatworkJenkinsBot
{
    public sealed class MainHub
    {
        //----- params -----
        
        //----- field -----

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

            var jobTriggerConfig = JobTriggerConfig.CreateInstance();

            await jobTriggerConfig.Load();

            // ChatWork.

            var chatworkService = ChatworkService.CreateInstance();

            await chatworkService.Initialize(cancelToken);

            // Jenkins.

            var jenkinsService = JenkinsService.CreateInstance();

            await jenkinsService.Initialize();

            // Spreadsheet.

            var spreadsheetService = SpreadsheetService.CreateInstance();

            await spreadsheetService.Initialize();

            // JobTrigger.

            var jobTriggerService = JobTriggerService.CreateInstance();

            await jobTriggerService.Initialize();

            ConsoleUtility.Separator();
        }

        public async Task Update(CancellationToken cancelToken)
        {
            var jobTriggerService = JobTriggerService.Instance;
            var chatworkService = ChatworkService.Instance;

            try
            {
                // 設定取得.
                await jobTriggerService.Fetch(cancelToken);

                // 新規メッセージ取得.
                var messages = await chatworkService.Fetch(cancelToken);

                // ジョブトリガー実行.
                if (messages.Any())
                {
                    foreach (var message in messages)
                    {
                        if(!jobTriggerService.IsTriggerMessage(message)){ continue; }

                        await jobTriggerService.InvokeTrigger(message, cancelToken);
                    }
                }
            }
            catch (Exception e)
            {
                ConsoleUtility.Separator();

                Console.WriteLine(e);
                
                ConsoleUtility.Separator();
            }
        }
    }
}
