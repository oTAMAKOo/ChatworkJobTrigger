
using IniFileParser.Model;

namespace ChatworkJobTrigger
{
    public sealed class TextDefine : IniFile<TextDefine>
    {
        //----- params -----

        private const string JobSection = "Job";

        private const string JobQueuedField = "Queued";
        private const string JobSuccessField = "Success";
        private const string JobFailedField = "Failed";
        private const string JobCanceledField = "Canceled";

        private const string ErrorSection = "Error";

        private const string CommandErrorField = "CommandError";

        private const string HelpSection = "Help";

        private const string BuildHelpField = "Build";
        private const string MasterHelpField = "Master";
        private const string ResourceHelpField = "Resource";

        //----- field -----
        
        //----- property -----

        public override string FileName { get { return "text"; } }

        public string JobQueued { get { return GetData<string>(JobSection, JobQueuedField); } }
        public string JobSuccess { get { return GetData<string>(JobSection, JobSuccessField); } }
        public string JobFailed { get { return GetData<string>(JobSection, JobFailedField); } }
        public string JobCanceled { get { return GetData<string>(JobSection, JobCanceledField); } }

        public string CommandError { get { return GetData<string>(ErrorSection, CommandErrorField); } }

        public string BuildHelp { get { return GetData<string>(HelpSection, BuildHelpField); } }
        public string MasterHelp { get { return GetData<string>(HelpSection, MasterHelpField); } }
        public string ResourceHelp { get { return GetData<string>(HelpSection, ResourceHelpField); } }

        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[JobSection][JobQueuedField] = "Job Queued!";
            data[JobSection][JobSuccessField] = "JobSuccess! [#BUILD_NUMBER#]";
            data[JobSection][JobFailedField] = "JobFailed... [#BUILD_NUMBER#]";
            data[JobSection][JobCanceledField] = "JobCanceled. [#BUILD_NUMBER#]";

            data[ErrorSection][CommandErrorField] = "Request command error.";

            data[HelpSection][BuildHelpField] = "";
            data[HelpSection][MasterHelpField] = "";
            data[HelpSection][ResourceHelpField] = "";
        }
    }
}
