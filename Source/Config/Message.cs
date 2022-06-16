using System;
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class MessageConfig : ConfigBase<MessageConfig>
    {
        //----- params -----

        private const string MessageSection = "Message";

        private const string JobSuccessFieldName = "JobSuccess";
        private const string JobFailedFieldName = "JobFailed";
        private const string JobCanceledFieldName = "JobCanceled";

        private const string ErrorSection = "Error";

        private const string RequestCommandErrorFieldName = "RequestCommandError";

        //----- field -----
        
        //----- property -----

        public override string ConfigIniName { get { return "message.ini"; } }

        public string JobSuccess { get { return GetData<string>(MessageSection, JobSuccessFieldName); } }
        public string JobFailed { get { return GetData<string>(MessageSection, JobFailedFieldName); } }
        public string JobCanceled { get { return GetData<string>(MessageSection, JobCanceledFieldName); } }

        public string RequestCommandError { get { return GetData<string>(ErrorSection, RequestCommandErrorFieldName); } }

        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[MessageSection][JobSuccessFieldName] = "JobSuccess! [#BUILD_NUMBER#]";
            data[MessageSection][JobFailedFieldName] = "JobFailed... [#BUILD_NUMBER#]";
            data[MessageSection][JobCanceledFieldName] = "JobCanceled. [#BUILD_NUMBER#]";

            data[ErrorSection][RequestCommandErrorFieldName] = "Request command error.";
        }
    }
}
