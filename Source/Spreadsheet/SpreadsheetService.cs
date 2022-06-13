using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Extensions;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace ChatworkJenkinsBot
{
    public sealed class SpreadsheetService : Singleton<SpreadsheetService>
    {
        //----- params -----

        private const string ClientSecretFileName = "client_secret.json";

        private static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };
        
        //----- field -----
        
        //----- property -----

        //----- method -----

        private SpreadsheetService() { }

        public async Task Initialize()
        {
            UserCredential credential;

            var configFileDirectory = ConfigUtility.GetConfigFolderDirectory();

            var clientSecretFilePath = PathUtility.Combine(configFileDirectory, ClientSecretFileName);

            if (!File.Exists(clientSecretFilePath))
            {
                throw new FileNotFoundException(clientSecretFilePath);
            }

            await using (var stream = new FileStream(clientSecretFilePath, FileMode.Open, FileAccess.Read))
            {
                var credPath = "token.json";

                var secrets = await GoogleClientSecrets.FromStreamAsync(stream);
                var token = CancellationToken.None;
                var fileDataStore = new FileDataStore(credPath, true);

                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(secrets.Secrets, Scopes, "user", token, fileDataStore);
            }

            var applicationName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

            var service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });

            // https://docs.google.com/spreadsheets/d/1UlZeldwDgb2fbASBs94YpNIsAgqQraDIUoo0Fu1Ekkg/edit

            // アクセスしようとしているのは以下のurlのスプレッドシート
            // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
            var spreadsheetId = "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms";
            var range = "Class Data!A2:E";

            var request = service.Spreadsheets.Values.Get(spreadsheetId, range);

            var response = await request.ExecuteAsync();
            var values = response.Values;

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    var str = string.Format("{0}, {1}", row[0], row[4]);

                    Console.WriteLine(str);
                }
            }
        }
    }
}
