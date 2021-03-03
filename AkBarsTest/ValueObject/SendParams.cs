namespace AkBarsTest.ValueObject
{
    class SendParams
    {
        public string SourcePath { get; }
        public string DestinationPath { get; }

        public SendParams(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }
    }
}
