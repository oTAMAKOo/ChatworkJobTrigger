
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using JenkinsNET;
using JenkinsNET.Exceptions;
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

    public sealed class JobResult
    {
        public string JobName { get; set; }

        public IDictionary<string, string> Parameters { get; set; }

        public JobStatus Status { get; set; }

        public JenkinsBuildBase ResultInfo { get; set; }

        public int? BuildNumber { get; set; }
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

        public async Task<JobResult> ReqestBuild(string jobName, IDictionary<string, string> jobParameters, Action<JobStatus, int?, int?> onJobStatusChanged)
        {
            var setting = Setting.Instance;
            
            var jobInfo = new JobResult()
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

            JenkinsBuildBase build = null;
            
            try
            {
                if(jobParameters != null && jobParameters.Any())
                {
                    build = await runner.RunWithParametersAsync(jobName, jobParameters);
                }
                else
                {
                    build = await runner.RunAsync(jobName);
                }

                if (build != null)
                {
                    jobInfo.BuildNumber = build.Number;
                }
            }
            catch (JenkinsJobBuildException)
            {
                /* エラーとして扱わない */
            }
            catch (JenkinsJobGetBuildException)
            {
                /* エラーとして扱わない */
            }

            // ビルド開始を暫く待つ.

            await Task.Delay(TimeSpan.FromSeconds(5f));

            // ビルド完了後の後処理を待つ.

            if (!jobInfo.BuildNumber.HasValue){ return null; }

            var buildNumberStr = jobInfo.BuildNumber.ToString();

            var retryCount = 0;

            while (true)
            {
                try
                {
                    build = await client.Builds.GetAsync<JenkinsBuildBase>(jobName, buildNumberStr);

                    if (!build.Building.HasValue || !build.Building.Value) { break; }
                
                    await Task.Delay(TimeSpan.FromSeconds(30));

                    retryCount = 0;
                }
                catch (TimeoutException)
                {
                    retryCount++;
                }
                catch (JenkinsJobGetBuildException)
                {
                    retryCount++;
                }

                if (0 < retryCount)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3f));
                }

                if (5 < retryCount){ break; }
            }

            // ビルド結果.
             
            jobInfo.ResultInfo = build;

            switch (build.Result)
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

        public string GetJobMessage(JobStatus status, int? buildNumber)
        {
            var textDefine = TextDefine.Instance;

            var message = string.Empty;

            switch (status)
            {
                case JobStatus.Success:
                    message += textDefine.JobSuccess;
                    break;
                case JobStatus.Failed:
                    message += textDefine.JobFailed;
                    break;
                case JobStatus.Canceled:
                    message += textDefine.JobCanceled;
                    break;
                default:
                    message += $"{status} #BUILD_NUMBER#";
                    break;
            }

            message = message.Replace("#BUILD_NUMBER#", buildNumber.HasValue ? buildNumber.ToString() : string.Empty);

            return message;
        }

        public string GetLogFilePath(string jobName, int buildNumber)
        {
            var setting = Setting.Instance;

            var logFilePath = setting.JenkinsLogFilePath;

            return logFilePath.Replace("#JOB_NAME#", jobName).Replace("#BUILD_NUMBER#", buildNumber.ToString());
        }
    }
}
