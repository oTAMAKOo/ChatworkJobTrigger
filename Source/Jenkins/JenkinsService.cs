using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using JenkinsNET;
using JenkinsNET.Models;
using JenkinsNET.Utilities;

namespace ChatworkJenkinsBot
{
    public sealed class JenkinsService : Singleton<JenkinsService>
    {
        //----- params -----
        
        public sealed class JobRequest
        {

        }
        
        //----- field -----
        
        private JenkinsClient client = null;

        private JenkinsConfig config = null;

        //----- property -----


        //----- method -----

        private JenkinsService(){ }

        public async Task Initialize()
        {
            Console.WriteLine("Initialize JenkinsService");

            config = new JenkinsConfig();

            await config.Load();
        }

        private async Task<JenkinsBuildBase> RunJenkinsJob(string jobName, IDictionary<string, string> jobParameters = null)
        {
            var runner = new JenkinsJobRunner(client);

            runner.StatusChanged += () => 
            {
                switch (runner.Status) {
                    case JenkinsJobStatus.Queued:
                        Console.WriteLine("Job is Queued.");
                        break;
                    case JenkinsJobStatus.Building:
                        Console.WriteLine("Job is Running.");
                        break;
                    case JenkinsJobStatus.Complete:
                        Console.WriteLine("Job is Complete.");
                        break;
                }
            };

            Console.WriteLine($"Starting Job '{jobName}'...");

            JenkinsBuildBase buildResult = null;
            
            if(jobParameters != null && jobParameters.Any())
            {
                buildResult = await runner.RunWithParametersAsync(jobName, jobParameters);
            }
            else
            {
                buildResult = await runner.RunAsync(jobName);
            }

            if (buildResult == null){ return null; }

            if (string.Equals(buildResult.Result, "SUCCESS"))
            {
                Console.WriteLine($"Build #{buildResult.Number} completed successfully.");
                Console.WriteLine($"Report: {buildResult.Url}");
            }
            else
            {
                throw new ApplicationException($"Build #{buildResult.Number} Failed!");
            }

            return buildResult;
        }
    }
}
