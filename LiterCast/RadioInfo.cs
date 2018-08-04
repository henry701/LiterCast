namespace LiterCast
{
    internal sealed class RadioInfo
    {
        public int MetadataInterval { get; private set; }

        public RadioInfo(int metadataInterval)
        {
            MetadataInterval = metadataInterval;
        }
    }
}