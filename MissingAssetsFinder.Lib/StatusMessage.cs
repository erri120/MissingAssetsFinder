namespace MissingAssetsFinder.Lib
{
    public class StatusMessage
    {
        public string Message { get; set; }

        public StatusMessage(string s)
        {
            Message = s;
        }

        public override string ToString()
        {
            return Message;
        }
    }
}
