using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WfComponent.Utils;
using static ShotgunMetagenome.Several.AnywayUtils;

namespace ShotgunMetagenome.Several
{
    public class CentrifugeDatabase
    {

        public static string defaultDatabase = "p_compressed"; // rename after... original: p_compressed_2018_4_15.tar.gz
        public static string defaultDatabaseUrl = "https://genome-idx.s3.amazonaws.com/centrifuge/p_compressed_2018_4_15.tar.gz";


        // default database の有無
        public static bool IsDefaultDatabseExist(IProgress<string> logger = null)
        {
            if (logger == null)
                logger = new Progress<string>(delegate (string s)
                { System.Diagnostics.Debug.WriteLine( s); });
            // 
            var message = string.Empty;
            var dat = AnywayUtils.GetDatabase(ref message);
            if( string.IsNullOrEmpty(message))
                // System.Diagnostics.Debug.WriteLine(message);
                logger.Report(message);

            if (dat == null || !dat.Any() || !dat.Contains(defaultDatabase))
                return false;  // p_compressed がない。。。

            return true;
        }



        public static string GetDatabase(ref string message, IProgress<string> progress = null)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));

            // database-dir
            var downDir = Path.Combine(currentDir, dataDir, databaseDir);

            WebClientWrap client = new WebClientWrap();
            var res = client.DownloadHttpWebRequest(defaultDatabaseUrl, downDir, ref message , progress);


            return string.Empty;
        }


    }
}
