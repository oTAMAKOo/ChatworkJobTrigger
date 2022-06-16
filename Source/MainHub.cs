
using System;
using System.Collections.Generic;
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

            var messageConfig = MessageConfig.Instance;

            await messageConfig.Load();

            // ChatWork.

            var chatworkService = ChatworkService.Instance;

            await chatworkService.Initialize(cancelToken);

            // Jenkins.

            var jenkinsService = JenkinsService.Instance;

            await jenkinsService.Initialize();

            // Spreadsheet.

            var spreadsheetService = SpreadsheetService.Instance;

            await spreadsheetService.Initialize();

            ConsoleUtility.Separator();
        }

        public async Task Update(CancellationToken cancelToken)
        {
            var spreadsheetService = SpreadsheetService.Instance;
            var chatworkService = ChatworkService.Instance;

            await spreadsheetService.UpdateProjectSettings(cancelToken);

            await chatworkService.Fetch(cancelToken);
        }
    }
}
