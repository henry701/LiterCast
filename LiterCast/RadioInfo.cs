namespace LiterCast
{
    public sealed class RadioInfo
    {
        public int MetadataInterval { get; private set; }

        public RadioInfo(int metadataInterval = 1024 * 8)
        {
            MetadataInterval = metadataInterval;
        }
    }
}