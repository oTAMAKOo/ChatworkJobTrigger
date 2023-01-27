
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

    }
}
