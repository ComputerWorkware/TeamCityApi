
namespace TeamCityApi.Domain
{
    public sealed class BuildContinuationMode
    {
        private readonly string _value;

        public static readonly BuildContinuationMode MakeFailedToStart = new BuildContinuationMode("MAKE_FAILED_TO_START");
        public static readonly BuildContinuationMode RunAddProblem = new BuildContinuationMode("RUN_ADD_PROBLEM");
        public static readonly BuildContinuationMode Run = new BuildContinuationMode("RUN");
        public static readonly BuildContinuationMode Cancel = new BuildContinuationMode("CANCEL");

        private BuildContinuationMode(string value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return _value;
        }
    }

}