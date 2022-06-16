using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JenkinsNET;
using JenkinsNET.Models;
using JenkinsNET.Utilities;
using Kurukuru;
using Extensions;

namespace ChatworkJenkinsBot
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

    public enum JobResult
    {
        None = 0,

        Success,
        Failed,
        Canceled,
    }
    
    public sealed class JobInfo
    {
        public string JobName { get; set; }

        public IDictionary<string, string> Parameters { get; set; }

        public JobStatus Status { get; set; }

        public JenkinsBuildBase ResultInfo { get; set; }

        public JobResult Result { get; set; }
    }

    public sealed class JenkinsService : Singleton<JenkinsService>
    {
        //----- params -----

        //----- field -----
        
        private JenkinsClient client = null;

        //----- property -----

        //----- method -----

        private JenkinsService() { }

        public async Task Initialize()
        {
            Console.WriteLine("JenkinsService");

            var config = JenkinsConfig.Instance;

            await config.Load();

            client = new JenkinsClient()
            {
                BaseUrl = config.BaseUrl,
                UserName = config.UserName,
                ApiToken = config.ApiToken,
            };
        }

        public string GetJobStatusText(string jobName, string jobArguments, JobStatus status, int? buildNumber = null)
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

        public async Task<JobInfo> RunJenkinsJob(string jobName, IDictionary<string, string> jobParameters = null)
        {
            var jobInfo = new JobInfo()
            {
                JobName = jobName,
                Parameters = jobParameters,
                Status = JobStatus.Starting,
            };

            var jobArguments = string.Empty;

            if (jobParameters != null && jobParameters.Any())
            {
                var items = jobParameters.Select(x => x.ToString());

                jobArguments = string.Join(", ", items);
            }

            var spinner = new Spinner(GetJobStatusText(jobName, jobArguments, JobStatus.Starting), Patterns.Dots, ConsoleColor.Cyan);

            spinner.Start();

            var runner = new JenkinsJobRunner(client);

            runner.StatusChanged += () => 
            {
                switch (runner.Status) {
                    case JenkinsJobStatus.Queued:
                        jobInfo.Status = JobStatus.Queued;
                        spinner.Text = GetJobStatusText(jobName, jobArguments, JobStatus.Queued);
                        break;
                    case JenkinsJobStatus.Building:
                        jobInfo.Status = JobStatus.Running;
                        spinner.Text = GetJobStatusText(jobName, jobArguments, JobStatus.Running);
                        break;
                }
            };

            JenkinsBuildBase buildResult = null;
            
            if(jobParameters != null &&jobParameters.Any())
            {
                buildResult = await runner.RunWithParametersAsync(jobName, jobParameters);
            }
            else
            {
                buildResult = await runner.RunAsync(jobName);
            }

            if (buildResult == null){ return null; }

            switch (buildResult.Result)
            {
                case "SUCCESS":
                    jobInfo.Status = JobStatus.Success;
                    spinner.Succeed(GetJobStatusText(jobName, jobArguments, JobStatus.Success, buildResult.Number));
                    break;
                case "FAILURE":
                    jobInfo.Status = JobStatus.Failed;
                    spinner.Fail(GetJobStatusText(jobName, jobArguments, JobStatus.Failed, buildResult.Number));
                    break;
                case "ABORTED":
                    jobInfo.Status = JobStatus.Canceled;
                    spinner.Fail(GetJobStatusText(jobName, jobArguments, JobStatus.Canceled, buildResult.Number));
                    break;
                default:
                    jobInfo.Status = JobStatus.Unknown;
                    spinner.Stop($"Unknown state : [{buildResult.Number}] {buildResult.Result}.");
                    break;

            }

            return jobInfo;
        }
    }
}
