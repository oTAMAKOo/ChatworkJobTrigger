
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public sealed class SpreadsheetConfig : ConfigBase<SpreadsheetConfig>
    {
        //----- params -----

        private const string SpreadsheetSection = "Spreadsheet";

        private const string SpreadsheetIdFieldName = "SpreadsheetId";

        private const string ProjectSection = "Project";

        private const string ArgumentNamesRangeFieldName = "ArgumentNames";

        private const string ArgumentCandidateRangeFieldName = "ArgumentCandidate";

        private const string JobNameRangeFieldName = "JobName";

        //----- field -----
        
        //----- property -----

        public override string ConfigIniName { get { return "spreadsheet.ini"; } }

        public string SpreadsheetId { get { return GetData<string>(SpreadsheetSection, SpreadsheetIdFieldName); } }

        /// <summary> 引数名セルレンジ </summary>
        public string ArgumentNamesRange { get { return GetData<string>(ProjectSection, ArgumentNamesRangeFieldName); } }
        
        /// <summary> 引数候補セルレンジ </summary>
        public string ArgumentCandidateRange { get { return GetData<string>(ProjectSection, ArgumentCandidateRangeFieldName); } }

        /// <summary> ジョブ名フォーマットセルレンジ </summary>
        public string JobNameRange { get { return GetData<string>(ProjectSection, JobNameRangeFieldName); } }

        //----- method -----

        protected override void SetDefaultData(ref IniData data)
        {
            data[SpreadsheetSection][SpreadsheetIdFieldName] = "ABCDEFG123456789";

            data[ProjectSection][ArgumentNamesRangeFieldName] = "シート1 Data!A2:E";
            data[ProjectSection][ArgumentCandidateRangeFieldName] = "シート1 Data!A2:E";
            data[ProjectSection][JobNameRangeFieldName] = "シート1 Data!A2:E";
        }
    }
}
