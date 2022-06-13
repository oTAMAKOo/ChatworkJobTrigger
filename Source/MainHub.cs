
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

        public async Task Initialize()
        {
            ConsoleUtility.Separator();

            // ChatWork.

            var chatworkService = ChatworkService.Instance;

            await chatworkService.Initialize();

            ConsoleUtility.Separator();

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
            var chatworkService = ChatworkService.Instance;

            await chatworkService.Fetch();
        }
    }
}
