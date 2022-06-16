using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Extensions;

using DateTime = System.DateTime;

namespace ChatworkJenkinsBot
{
    public sealed class SpreadsheetService : Singleton<SpreadsheetService>
    {
        //----- params -----

        private const string ClientSecretFileName = "client_secret.json";

        private static string[] Scopes = { SheetsService.Scope.SpreadsheetsReadonly };

        private const int UpdateInterval = 30;
        
        //----- field -----

        private SheetsService service = null;

        private DateTime nextUpDateTime = default;
        
        //----- property -----

        //----- method -----

        private SpreadsheetService() { }

        public async Task Initialize()
        {
            Console.WriteLine("SpreadsheetService");

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

            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });

            var config = SpreadsheetConfig.Instance;

            await config.Load();

            nextUpDateTime = DateTime.Now;
        }

        public async Task UpdateProjectSettings(CancellationToken cancelToken)
        {
            var time = DateTime.Now;

            if (time < nextUpDateTime){ return; }

            var model = Model.Instance;

            var spreadsheetConfig = SpreadsheetConfig.Instance;

            var spreadsheetId = spreadsheetConfig.SpreadsheetId;

            // ArgumentNames.
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, spreadsheetConfig.ArgumentNamesRange);

                var response = await request.ExecuteAsync(cancelToken);

                var values = response.Values;

                if (values != null && 0 < values.Count)
                {
                    foreach (var row in values)
                    {
                        var key = row[0].ToString();
                        var names = row.Skip(1).Cast<string>().ToArray();

                        model.SetArgumentNames(key, names);
                    }
                }
            }

            // ArgumentCandidate.
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, spreadsheetConfig.ArgumentCandidateRange);

                var response = await request.ExecuteAsync(cancelToken);

                var values = response.Values;

                if (values != null && 0 < values.Count)
                {
                    foreach (var row in values)
                    {
                        var key = row[0].ToString();
                        var candidates = row.Skip(1).Cast<string>().ToArray();

                        model.SetCandidates(key, candidates);
                    }
                }
            }
            
            // JobName.
            {
                var request = service.Spreadsheets.Values.Get(spreadsheetId, spreadsheetConfig.JobNameRange);

                var response = await request.ExecuteAsync(cancelToken);

                var values = response.Values;

                if (values != null && 0 < values.Count)
                {
                    foreach (var row in values)
                    {
                        var key = row[0].ToString();
                        var jobName = row.Skip(1).Cast<string>().First();

                        model.SetJobName(key, jobName);
                    }
                }
            }

            nextUpDateTime = time.AddSeconds(UpdateInterval);
        }
    }
}
