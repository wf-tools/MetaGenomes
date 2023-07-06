using ShotgunMetagenome.External;
using ShotgunMetagenome.Models;
using System.Collections.Generic;
using System.Linq;

namespace ShotgunMetagenome.Proc.Properties
{
    public class AnalysisProperties : BaseProperties
    {


        // public IEnumerable<string> filePaths;
        public IEnumerable<SampleGroup> groups;

        public string useDb;
        // public string kmer;

        public string useThreads = WfComponent.Utils.ProcessUtils.CpuCore();
        
        // sec-analysis 
        public bool isPlotsEnable = false;
        public string usePcoaDistance = Pcoa.defaultPcoaDistanceMetric;  // default distance..
        public string useNmdsDistance = Pcoa.defaultNmdsDistanceMetric;  // default distance..
        public string usePermanovaPermutations = Permanova.defaultPermutations;
        public IEnumerable<string> plotColors;

        // permanova
        public string permutations ;
    }
}
