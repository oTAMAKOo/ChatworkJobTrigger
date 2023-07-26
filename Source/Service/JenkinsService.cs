
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using JenkinsNET;
using JenkinsNET.Models;
using JenkinsNET.Utilities;
using Extensions;

namespace ChatworkJobTrigger
{
    public enum JobStatus
    {
        None = 0,

        Starting,
        Pending,
        Queued,
        Building,
        Success,
        Failed,
        Canceled,
        Unknown,
    }

    public sealed class JobInfo
    {
        public string JobName { get; set; }

        public IDictionary<string, string> Parameters { get; set; }

        public JobStatus Status { get; set; }

        public JenkinsBuildBase Build { get; set; }

        public int? QueueNumber { get; set; }

        public int? BuildNumber { get; set; }

        public Exception Error { get; set; }
    }

    public sealed class JenkinsService : Singleton<JenkinsService>
    {
        //----- params -----

        //----- field -----
        
        private JenkinsClient client = null;

        //----- property -----

        //----- method -----

        private JenkinsService() { }

        public Task Initialize()
        {
            Console.WriteLine("JenkinsService");

            var setting = Setting.Instance;

            client = new JenkinsClient()
            {
                BaseUrl = setting.JenkinsBaseUrl,
                UserName = setting.JenkinsUserName,
                ApiToken = setting.JenkinsApiToken,
            };

            return Task.CompletedTask;
        }

        public async Task<JobInfo> ReqestBuild(string jobName, IDictionary<string, string> jobParameters, Action<JobStatus, int?, int?> onJobStatusChanged)
        {
            var setting = Setting.Instance;
            
            var jobInfo = new JobInfo()
            {
                JobName = jobName,
                Parameters = jobParameters,
                Status = JobStatus.Starting,
            };

            // ビルド実行.
            
            var runner = new JenkinsJobRunner(client)
            {
                PollInterval = 20000,
                BuildTimeout = setting.JenkinsBuildTimeout,
                QueueTimeout = setting.JenkinsQueueTimeout,
            };
            
            runner.StatusChanged += () => 
            {
                jobInfo.QueueNumber = runner.QueueItemNumber;
                jobInfo.BuildNumber = runner.BuildNumber;

                switch (runner.Status) 
                {
                    case JenkinsJobStatus.Pending:
                        jobInfo.Status = JobStatus.Pending;
                        break;
                    case JenkinsJobStatus.Queued:
                        jobInfo.Status = JobStatus.Queued;
                        break;
                    case JenkinsJobStatus.Building:
                        jobInfo.Status = JobStatus.Building;
                        break;
                }

                if (onJobStatusChanged != null)
                {
                    onJobStatusChanged.Invoke(jobInfo.Status, runner.QueueItemNumber, runner.BuildNumber);
                }
            };

            try
            {
                if(jobParameters != null && jobParameters.Any())
                {
                    jobInfo.Build = await runner.RunWithParametersAsync(jobName, jobParameters);
                }
                else
                {
                    jobInfo.Build = await runner.RunAsync(jobName);
                }
            }
            catch (Exception e)
            {
                jobInfo.Error = e;
            }

            await Task.Delay(TimeSpan.FromSeconds(5f));

            // キュー・ビルドが開始されていない場合はエラーにする.

            if (!runner.QueueItemNumber.HasValue && !runner.BuildNumber.HasValue)
            {
                if (jobInfo.Error == null)
                {
                    jobInfo.Error = new Exception("Jenkins job start failed.");
                }

                return jobInfo;
            }

            // キュー状況を取得.

            jobInfo = await RetrieveQueueProcess(jobInfo);

            if (jobInfo.Error != null) { return jobInfo; }

            await Task.Delay(TimeSpan.FromSeconds(15));

            // ビルド状況を取得.

            jobInfo = await RetrieveBuildProcess(jobInfo);

            if (jobInfo.Error != null) { return jobInfo; }

            await Task.Delay(TimeSpan.FromSeconds(15));

            // ビルド結果.

            if (jobInfo.Build != null)
            {
                switch (jobInfo.Build.Result)
                {
                    case "SUCCESS":
                        jobInfo.Status = JobStatus.Success;
                        break;
                    case "FAILURE":
                        jobInfo.Status = JobStatus.Failed;
                        break;
                    case "ABORTED":
                        jobInfo.Status = JobStatus.Canceled;
                        break;
                    default:
                        jobInfo.Status = JobStatus.Unknown;
                        break;
                }
            }
            else
            {
                jobInfo.Error = new Exception("Build result jobInfo.Build is null.");
                
                return jobInfo;
            }
            
            if (onJobStatusChanged != null)
            {
                onJobStatusChanged.Invoke(jobInfo.Status, null, jobInfo.BuildNumber);
            }

            return jobInfo;
        }

        private async Task<JobInfo> RetrieveQueueProcess(JobInfo jobInfo)
        {
            var retryCount = 0;

            jobInfo.Error = null;

            var errorMessage = "Jenkins get queue progress failed.";

            while (true)
            {
                try
                {
                    if (jobInfo.BuildNumber.HasValue){ break; }

                    if (!jobInfo.QueueNumber.HasValue) { break; }

                    await WaitNetworkConnection();

                    var queue = await client.Queue.GetItemAsync(jobInfo.QueueNumber.Value);

                    if (queue == null)
                    {
                        throw new Exception(errorMessage);
                    }

                    if (queue.Cancelled == true) { break; }

                    await Task.Delay(TimeSpan.FromSeconds(15));

                    retryCount = 0;
                }
                catch
                {
                    retryCount++;
                }

                if (0 < retryCount)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5f));
                }

                if (15 <= retryCount)
                {
                    jobInfo.Error = new Exception(errorMessage);

                    break;
                }
            }

            jobInfo.QueueNumber = null;

            return jobInfo;
        }

        private async Task<JobInfo> RetrieveBuildProcess(JobInfo jobInfo)
        {
            if (!jobInfo.BuildNumber.HasValue)
            {
                if (jobInfo.Error == null)
                {
                    jobInfo.Error = new Exception("RetrieveBuildProcess : BuildNumber not found.");
                }

                return jobInfo;
            }

            var buildNumberStr = jobInfo.BuildNumber.ToString();

            var retryCount = 0;

            var errorMessage = "Jenkins get build progress failed.";

            jobInfo.Error = null;

            while (true)
            {
                try
                {
                    await WaitNetworkConnection();

                    jobInfo.Build = await client.Builds.GetAsync<JenkinsBuildBase>(jobInfo.JobName, buildNumberStr);

                    if (jobInfo.Build == null)
                    {
                        throw new Exception(errorMessage);
                    }

                    if (!jobInfo.Build.Building.HasValue || !jobInfo.Build.Building.Value) { break; }
                
                    await Task.Delay(TimeSpan.FromSeconds(15));

                    retryCount = 0;
                }
                catch
                {
                    retryCount++;
                }

                if (0 < retryCount)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5f));
                }

                if (15 <= retryCount)
                {
                    jobInfo.Error = new Exception(errorMessage);

                    break;
                }
            }

            return jobInfo;
        }

        public async Task<bool> ReqestCancel(string jobName, int? queuedNumber, int? buildNumber)
        {
            var setting = Setting.Instance;

            var result = false;
            
            var url = string.Empty;

            if(buildNumber.HasValue)
            {
                url = setting.JenkinsBaseUrl + $"job/{jobName}/{buildNumber.Value}/stop";
            }
            else if (queuedNumber.HasValue)
            {
                url = setting.JenkinsBaseUrl + $"queue/cancelItem?id={queuedNumber.Value}";
            }

            if (!string.IsNullOrEmpty(url))
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    
                    // Basic認証.

                    var authToken = Encoding.ASCII.GetBytes($"{client.UserName}:{client.ApiToken}");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

                    // 送信.
                    var response = await httpClient.SendAsync(request);
                    
                    if (response != null)
                    {
                        result = response.IsSuccessStatusCode;
                    }
                }
            }

            return result;
        }

        public string GetJobMessage(JobStatus status, int? buildNumber, string buildToken)
        {
            var textDefine = TextDefine.Instance;

            var message = string.Empty;

            switch (status)
            {
                case JobStatus.Queued:
                    message = textDefine.JobQueued;
                    break;
                case JobStatus.Success:
                    message = textDefine.JobSuccess;
                    break;
                case JobStatus.Failed:
                    message = textDefine.JobFailed;
                    break;
                case JobStatus.Canceled:
                    message = textDefine.JobCanceled;
                    break;
                default:
                    message = $"{status} #BUILD_NUMBER#";
                    break;
            }

            if (!string.IsNullOrEmpty(message))
            {
                message = message
                    .Replace("#BUILD_NUMBER#", buildNumber.ToString())
                    .Replace("#BUILD_TOKEN#", buildToken);
            }

            return message;
        }

        public string GetJobStatusMessage(JobStatus status, int? buildNumber)
        {
            var message = $"{status}";

            if (buildNumber.HasValue)
            {
                message += $" [No.{buildNumber}]";
            }

            return message;
        }

        public string GetLogFilePath(string jobName, int buildNumber)
        {
            var setting = Setting.Instance;

            var logFilePath = setting.JenkinsLogFilePath;

            return logFilePath.Replace("#JOB_NAME#", jobName).Replace("#BUILD_NUMBER#", buildNumber.ToString());
        }

        private async Task WaitNetworkConnection()
        {
            while (true)
            {
                if (NetworkInterface.GetIsNetworkAvailable()){ break; }
                
                await Task.Delay(TimeSpan.FromSeconds(5f));
            }
        }
    }
}
