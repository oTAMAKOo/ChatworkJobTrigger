
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
        private const string ArgumentNotFoundErrorField = "ArgumentNotFound";
        private const string UndefinedValueErrorField = "UndefinedValue";
        private const string UndefinedTypeErrorField = "UndefinedType";
        
        //----- field -----
        
        //----- property -----

        public override string FileName { get { return "text"; } }

        public string JobQueued { get { return GetData<string>(JobSection, JobQueuedField); } }
        public string JobSuccess { get { return GetData<string>(JobSection, JobSuccessField); } }
        public string JobFailed { get { return GetData<string>(JobSection, JobFailedField); } }
        public string JobCanceled { get { return GetData<string>(JobSection, JobCanceledField); } }

        public string CommandError { get { return GetData<string>(ErrorSection, CommandErrorField); } }
        public string ArgumentNotFoundError { get { return GetData<string>(ErrorSection, ArgumentNotFoundErrorField); } }
        public string UndefinedValueError { get { return GetData<string>(ErrorSection, UndefinedValueErrorField); } }
        public string UndefinedTypeError { get { return GetData<string>(ErrorSection, UndefinedTypeErrorField); } }

        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[JobSection][JobQueuedField] = "Job Queued! #BUILD_TOKEN#";
            data[JobSection][JobSuccessField] = "JobSuccess! [#BUILD_NUMBER#]";
            data[JobSection][JobFailedField] = "JobFailed... [#BUILD_NUMBER#]";
            data[JobSection][JobCanceledField] = "JobCanceled. [#BUILD_NUMBER#]";

            data[ErrorSection][CommandErrorField] = "Request command error.";
            data[ErrorSection][ArgumentNotFoundErrorField] = "Argument #FIELD_NAME# is required.";
            data[ErrorSection][UndefinedValueErrorField] = "Undefined value #VALUE#.";
            data[ErrorSection][UndefinedTypeErrorField] = "Undefined type #TYPE_NAME# is defined.";
        }
    }
}
