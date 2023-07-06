using System;
using System.Threading;
using System.Threading.Tasks;
using WfComponent.Base;

namespace ShotgunMetagenome.Proc.Flow
{
    public abstract class BaseFlow : IFlow
    {
        protected IProgress<string> progress;
        protected IProcess specificProcess;
        public CancellationTokenSource cancellationTokenSource { get; set; }
        public string ExecuteMethod { get ; set ; }

        public BaseFlow(IProgress<string> progress)
        {
            if (progress == null)
                progress = new Progress<string>(s => System.Diagnostics.Debug.WriteLine(s));
            this.progress = progress;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task<string> CallFlowAsync()
        {
            // Cancel 発行していたらそのまま。
            if (this.cancellationTokenSource == null || this.cancellationTokenSource.IsCancellationRequested)
            {
                progress.Report("Called flow is cancel.....  CancellationRequested :" + this.cancellationTokenSource.IsCancellationRequested);

                return IFlow.CanceledMessage; // 
            }
                

            var returnCodeMessage = IFlow.NormalEndMessage;
            try
            {
                await Task.Run(() =>
                {
                    // スタート
                    var res = StartFlow();
                    if (res.Contains(IFlow.ErrorEndMessage))
                        returnCodeMessage = IFlow.ErrorEndMessage;

                    // }, cancellationTokenSource.Token).ConfigureAwait(true);
                }, cancellationTokenSource.Token);

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    progress.Report("Cancellation Requested");
                    throw new Exception();
                }
                progress.Report("end of a term");
            }
            catch (OperationCanceledException e)
            {  // Cancel
                progress.Report("Canceled \n" + e.Message);
                returnCodeMessage = IFlow.CanceledMessage;
            }
            catch (Exception e)
            {   // Error
                progress.Report(e.Message);
                // Utils.VariousUtils.WriteError("BaseFlow.err", e.Message);
                returnCodeMessage = IFlow.ErrorEndMessage;
            }

            this.cancellationTokenSource.Dispose();
            return returnCodeMessage;
        }

        protected bool IsCancel()
        {
           // progress.Report("Cancel call...");
            return cancellationTokenSource.IsCancellationRequested;
        }

        // force cansel.
        public string CancelProcess()
        {
            LogReport("CancelProcess (force cancel)");
            this.cancellationTokenSource.Cancel();

            var res = string.Empty;
            if (this.specificProcess != null)
            {
                res = specificProcess.StopProcess(); // process.kill
                specificProcess = null;
            }
            return res;
        }

        protected void LogReport(string report)
            => this.progress.Report(report);

        public bool IsFlowEnable()
        {
            return this.specificProcess != null; // TODO Check-process
        }

        public abstract string StartFlow();
        public string CancelFlow()
        {
            // Flow Cancel.
            this.progress.Report("call Cancel Flow....");
            try
            {
                CancelProcess();
                this.cancellationTokenSource.Dispose();

                // throwable ...
                if (this.specificProcess != null)
                    specificProcess.StopProcess();

                specificProcess = null; // 必要？

            }
            catch (Exception e)
            {
                this.progress.Report("##### #####");
                this.progress.Report("flow cancel exception!");
                this.progress.Report(nameof(this.specificProcess));
                this.progress.Report(e.Message);
                this.progress.Report("##### #####");
            }
            return IFlow.CanceledMessage;
        }

        public bool IsProcessEnable()
              => string.IsNullOrEmpty(this.specificProcess.GetMessage());

    }




}

