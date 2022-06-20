
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class TextDefine : IniFile<TextDefine>
    {
        //----- params -----

        private const string BuildSection = "Build";

        private const string BuildQueuedField = "Queued";
        private const string BuildSuccessField = "Success";
        private const string BuildFailedField = "Failed";
        private const string BuildCanceledField = "Canceled";

        private const string ErrorSection = "Error";

        private const string CommandErrorField = "CommandError";

        //----- field -----
        
        //----- property -----

        public override string FileName { get { return "text"; } }

        public string BuildQueued { get { return GetData<string>(BuildSection, BuildQueuedField); } }
        public string BuildSuccess { get { return GetData<string>(BuildSection, BuildSuccessField); } }
        public string BuildFailed { get { return GetData<string>(BuildSection, BuildFailedField); } }
        public string BuildCanceled { get { return GetData<string>(BuildSection, BuildCanceledField); } }

        public string CommandError { get { return GetData<string>(ErrorSection, CommandErrorField); } }

        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[BuildSection][BuildQueuedField] = "Job Queued!";
            data[BuildSection][BuildSuccessField] = "JobSuccess! [#BUILD_NUMBER#]";
            data[BuildSection][BuildFailedField] = "JobFailed... [#BUILD_NUMBER#]";
            data[BuildSection][BuildCanceledField] = "JobCanceled. [#BUILD_NUMBER#]";

            data[ErrorSection][CommandErrorField] = "Request command error.";
        }
    }
}
