using ShotgunMetagenome.Models;
using System.Collections.Generic;

namespace ShotgunMetagenome.Proc.Properties
{
    // mapping consensus cds blast flow
    public class FuncGeneAnalysisProperties : BaseProperties
    {

        // public IEnumerable<string> filePaths;
        public IEnumerable<SampleGroup> groups;

        public string MappingReferene;
        // public string GffFile;

        public bool isFullReference = false;


    }
}
