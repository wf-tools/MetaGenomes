using Livet.Commands;
using Livet.Messaging;
using ShotgunMetagenome.Models;
using ShotgunMetagenome.Proc;
using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Utils;
using ShotgunMetagenome.ViewModels.Properties;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ShotgunMetagenome.ViewModels
{
    public partial class MainWindowViewModel : BaseGroupViewModel
    {
        public string Title => "WHMetagenome@KIN";
        public string Version => "version 0.1.9  Available until " + Expiration;
        public bool isLicenseActivate = false;

        // ライセンスの代わりに日付指定
        private static int[] ExpirationTerm = new int[] { 2023, 08, 31 };
        public static string Expiration = string.Join(".", ExpirationTerm);
        DateTime ExpirationDate = new DateTime(ExpirationTerm[0],
                                                                        ExpirationTerm[1],
                                                                        ExpirationTerm[2]);


        public IProgress<string> mainlog;
        private DispatcherTimer dispTimer;
        private long timerSpan = 10000000 * 45; // 20sec
        private string timerLog = " is running....";
        public static readonly string logClear = "MainLogClear";

        // constractor...
        public MainWindowViewModel() :base()
        {
            // mainview logger 
            this.mainlog = new Progress<string>(OnLogAppend);

            var message = string.Empty;
            this._databases = Several.AnywayUtils.GetDatabase(ref message).ToList();
            if(! string.IsNullOrEmpty(message))
                mainlog.Report(message);   // Database name not found?

            this._databases.Insert(0, string.Empty);
            Databases = _databases;
            DatabasesIdx = 0;


            // mapping references list
            this._mappingReference = Several.AnywayUtils.GetMappingReference(ref message).ToList();
            if (!string.IsNullOrEmpty(message))
                mainlog.Report(message);   // reference file not found?
            this._mappingReference.Insert(0, string.Empty);
            MappingReference = _mappingReference;
            MappingReferenceIdx = 0;


            // fastq TreeView initilaize
            this._sampleGroups = new ObservableCollection<SampleGroup>();
            this._selectDataList = new ObservableCollection<string>();

            // 処理中にログエリアへ出力するタイマー
            dispTimer = new DispatcherTimer(DispatcherPriority.Normal)
                                {   Interval = new TimeSpan(timerSpan)　};
            dispTimer.Tick += new EventHandler(DispTimerEvent);
            dispTimer.IsEnabled = true; // 初期無効
        }

        // Some useful code snippets for ViewModel are defined as l*(llcom, llcomn, lvcomm, lsprop, etc...).
        public new void Initialize()
        {
            System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            // License invalid....
            if(ExpirationDate < DateTime.Now)
            {
                mainlog.Report("##### Expiration of a term #####");
                _ = SleepTimeAsync(3);
                System.Windows.Application.Current.Shutdown();  // 強制終了
            }
        }

        public void OpenInformation()
        {
            System.Diagnostics.Debug.WriteLine("call information");
            using (var infoView = new InformationViewModel())
            {
                Messenger.Raise(new TransitionMessage(infoView, "InformationCommand"));
                this.isLicenseActivate = infoView.IsLicenceActivate;  // license が正常に認識されれば true

                System.Diagnostics.Debug.WriteLine("retuen license is " + infoView.IsLicenceActivate); // open URL or PDF
                // ライセンス認証が通って居ればそのまま。
                if (this.isLicenseActivate) return;

                // ライセンス認証通ってなければ再検査。
                if (EnvInfo.IsLicenceInvalid())// license invalid
                {

                    IsAnalysisExecute = false;
                }
                else
                {
                    // new license?
                    System.Diagnostics.Debug.WriteLine("retuen license is true " + infoView.IsLicenceActivate); // open URL or PDF
                    IsAnalysisExecute = true;
                }
            };
        }

        private string _logMessage;
        public string LogMessage
        {
            get => _logMessage;
            set { RaisePropertyChangedIfSet(ref _logMessage, value); }
        }


        private void OnLogAppend(string log)
        {
            if (string.IsNullOrEmpty(log)) return;
            if (log.Equals(logClear))
            {
                LogMessage = string.Empty;
                return;
            }

            log = (log.Length > 250) 
                                    ? log.Substring(0, 250) + "....." 
                                    :log;
            log = log.EndsWith(Environment.NewLine) 
                                    ? log 
                                    : log + Environment.NewLine;

            log = DateTime.Now.ToString("yyyy/MM/dd/ HH:mm.ss") + " " + log;
            LogMessage += log;
        }

        protected string WriteLog(string writeFilePath)
        {
            // ログTimerEventで発生したログは除去する
            var writelog = _logMessage.Split(Environment.NewLine).Where(s => !s.EndsWith(timerLog));

            var message = string.Empty;
            WfComponent.Utils.FileUtils.WriteFile(writeFilePath, writelog, ref message);
            if (!string.IsNullOrEmpty(message))
                System.Diagnostics.Debug.WriteLine("Logfile init error, " + message);

            mainlog.Report(message);
            return message;
        }

        // タイマーイベント本体
        private void DispTimerEvent(object sender, EventArgs e)
        {
            if (this.flow == null) return;


            if (string.IsNullOrEmpty(this.flow.ExecuteMethod))
                mainlog.Report("process" + timerLog);

            mainlog.Report(this.flow.ExecuteMethod + timerLog  );
        }

        private async Task SleepTimeAsync(int sec)
        {
            await Task.Delay(sec * 1000);
        }

        // tab select...
        private int _tabPage;
        public int TabPage 
        {
            get { return _tabPage;  }
            set 
            {
                if (RaisePropertyChangedIfSet(ref _tabPage, value))
                    AnarysisCheck();   // database select check is not seq-count check
            } 
        }


        // 各Process の 非同期処理
        private string ProcessResultMessage;
        protected async Task<string> ProcessAsync(IFlow flow)
        {
            // Log puts
            mainlog.Report(logClear);  // 処理の開始時に下部に表示されているログをクリア
            mainlog.Report(Version);
            mainlog.Report("----- start analysis. -----");
            mainlog.Report(WfComponent.Utils.FileUtils.LogDateString());

            // 非同期
            ProcessResultMessage = await flow.CallFlowAsync().ConfigureAwait(true);
            mainlog.Report(ProcessResultMessage);
            ProcessEnd(ProcessResultMessage);

            return ProcessResultMessage;
        }


        // 通常は此処に戻る。
        internal void ProcessEnd(string resValue)
        {
            // 作業終了
            mainlog.Report("process is finish... code:" + resValue);
            this.dispTimer.Stop();

            // log write。。。
            WriteLog(Path.Combine(
                                this.outDir,
                                startDateTime + ".log"));

            var resHtml = WfComponent.Utils.FileUtils.FindFile(
                                        outDir,
                                        BasicAnalysisProc.recentrifugeName,
                                        ".html");
            var RecentrifugeHtml = (resHtml != null && resHtml.Any())
                                                ? resHtml.First()
                                                : string.Empty;

            if (File.Exists(RecentrifugeHtml))
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(RecentrifugeHtml)
                    { UseShellExecute = true });


            System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(outDir)
                    { UseShellExecute = true });

            AnalysisButton = anlysesButtonEnable;
            IsAnalysisExecute = true;  // 
        }

        private void ProcessCancel()
        {
            // TODO cancel 作る？ ×ボタンで終了にする？
            if (this.flow != null)
                this.flow.CancelFlow();

            // 強制策駆除
            // this.flow = null;
        }
    }
}
