
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
        Queued,
        Running,
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

        private string GetJobStatusText(string jobName, string jobArguments, JobStatus status, int? buildNumber = null)
        {
            var text = $"{status} : ";

            if (buildNumber.HasValue)
            {
                text += $"({buildNumber.Value})";
            }

            text += $"{jobName}";

            if (!string.IsNullOrEmpty(jobArguments))
            {
                text += $" {jobArguments}";
            }

            return text;
        }

        public async Task<JobResult> ReqestBuild(string jobName, IDictionary<string, string> jobParameters, Action<JenkinsJobStatus, int?, int?> onJobStatusChanged)
        {
            var setting = Setting.Instance;

            var jobInfo = new JobResult()
            {
                JobName = jobName,
                Parameters = jobParameters,
                Status = JobStatus.Starting,
            };

            var runner = new JenkinsJobRunner(client)
            {
                PollInterval = 2500,
                BuildTimeout = setting.JenkinsBuildTimeout,
                QueueTimeout = setting.JenkinsQueueTimeout,
            };
            
            runner.StatusChanged += () => 
            {
                switch (runner.Status) 
                {
                    case JenkinsJobStatus.Queued:
                        jobInfo.Status = JobStatus.Queued;
                        break;
                    case JenkinsJobStatus.Building:
                        jobInfo.Status = JobStatus.Running;
                        break;
                }

                if (onJobStatusChanged != null)
                {
                    onJobStatusChanged.Invoke(runner.Status, runner.QueueItemNumber, runner.BuildNumber);
                }
            };

            JenkinsBuildBase build = null;
            
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
                if (build.Number.HasValue)
                {
                    var buildNumber =  build.Number.Value.ToString();
                    
                    while (true)
                    {
                        build = await client.Builds.GetAsync<JenkinsBuildBase>(jobName, buildNumber);

                        if (build.Building == false) { break; }
                        
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }

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
            }
            else
            {
                return null;
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

        public string GetJobResultMessage(JobResult result)
        {
            var textDefine = TextDefine.Instance;

            var message = string.Empty;

            switch (result.Status)
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
            }

            if (result.ResultInfo != null)
            {
                var buildNumber = result.ResultInfo.Number;

                message = message.Replace("#BUILD_NUMBER#", buildNumber.ToString());
            }

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
