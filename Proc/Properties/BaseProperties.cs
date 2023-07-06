using System;

namespace ShotgunMetagenome.Proc.Properties
{
    public abstract class BaseProperties
    {
        // MainView Log-string...
        public IProgress<string> progress;
        public string outDirectory;
        public string startDateTime;
        public string memo;

    }
}
