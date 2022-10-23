using Lucene.Net.Store;

namespace ManagedCode.Lucene.CloudDirectory;

internal class LuceneDirectoryTypes
{
    private void All()
    {
        FSDirectory fs;
        FilterDirectory fd;
        CompoundFileDirectory cfd;
        FileSwitchDirectory fsw;
        MMapDirectory md;
        TrackingDirectoryWrapper trk;
        RAMDirectory rd;
        RateLimitedDirectoryWrapper rate;
        SimpleFSDirectory sfs;
        NRTCachingDirectory crt;
        NIOFSDirectory nio;
    }
}