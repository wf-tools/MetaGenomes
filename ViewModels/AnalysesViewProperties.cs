using Livet.Messaging;
using Livet.Messaging.IO;
using ShotgunMetagenome.External;
using ShotgunMetagenome.Models;
using ShotgunMetagenome.Proc;
using ShotgunMetagenome.Proc.Flow;
using ShotgunMetagenome.Proc.Properties;
using ShotgunMetagenome.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ShotgunMetagenome.ViewModels
{
    public partial class MainWindowViewModel
    {

        #region Add Sequence Group
        public void OpenAddSequenceGroup()
        {
            System.Diagnostics.Debug.WriteLine("call AddSequence");
            SampleGroup addGroup = null;
            using (var addSeqView = new AddGroupViewModel())
            {
                addSeqView.groupCnt = this._sampleGroups.Count();
                addSeqView.registeredGroupNames = this._sampleGroups.Select(s => s.Name);

                Messenger.Raise(new TransitionMessage(addSeqView, "AddGoupCommand"));
                addGroup = addSeqView.addedGroup;

            };

            if (addGroup == null || addGroup.Name == null || ! addGroup.FilePaths.Any()) 
                return; // ×押してClose

            // 同じグループ名なら追加する。
            if (this._sampleGroups.Where(s => s.Name == addGroup.Name).Any())
            {
                var addFileGroup = this._sampleGroups.Where(s => s.Name == addGroup.Name).First();
                addFileGroup.AddFilePath(addGroup.FilePaths);
            }
            else
            {
                SampleGroups.Add(addGroup);
            }

            // 変更があったハズ
            PropertyChangedSelectDataList();
            return;
        }
        #endregion

        #region New Sequence Group 
        public SampleGroup OpenNewSequenceGroup()
        {
            System.Diagnostics.Debug.WriteLine("call NewSequence");
            SampleGroup newSampleGroup = null;
            using (var newSeqView = new NewGroupViewModel())
            {
                newSeqView.groupCnt = this._sampleGroups.Count();
                newSeqView.registeredGroupNames = this._sampleGroups.Select(s => s.Name);

                Messenger.Raise(new TransitionMessage(newSeqView, TransitionMode.Modal ,"NewGoupCommand"));
                newSampleGroup = newSeqView.addedGroup;

            };

            return newSampleGroup;
        }
        #endregion

        #region DialogViewModel:CreateSelectDataList override
        protected override void CreateSelectDataList(IEnumerable<string> files)
        {
            System.Diagnostics.Debug.Write(files);

            var newGroup = OpenNewSequenceGroup();
            if (newGroup == null || string.IsNullOrEmpty(newGroup.Name) ) 
                return;  // Sequence の登録がないとか。名前がないとか。

            SelectDataList.Clear(); // 

            // 同じグループ名なら追加する。
            if( this._sampleGroups.Where(s => s.Name == newGroup.Name).Any())
            {
                var addFileGroup = this._sampleGroups.Where(s => s.Name == newGroup.Name).First();
                addFileGroup.AddFilePath(files);
            }
            else
            {
                newGroup.SetFilePaths(files);   // file drop した結果
                SampleGroups.Add(newGroup);
            }
            // RaisePropertyChanged(nameof(SampleGroups));
            PropertyChangedSelectDataList();
        }

        public override void PropertyChangedSelectDataList() {
            // 
            RaisePropertyChanged(nameof(SampleGroups));
            AnarysisCheck();
        }

        // clear button, sample tree clear
        public void CallSampleTreeClear()
        {
            this._sampleGroups.Clear();
            PropertyChangedSelectDataList();
        }

        public void CallRemoveItem()
        {
            System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            var newSampleGroups = new ObservableCollection<SampleGroup>();
            foreach (var group in _sampleGroups)
            {
                if (group.IsExpanded && group.IsSelected)
                    continue;

                var remove = group.Sequences.Where(s => s.IsSelected);
                if (remove != null && remove.Any()) // 取り敢えず
                {
                    var removed = group.Sequences.Where(s => s.Name != remove.First().Name);
                    var newGroup = new SampleGroup()
                    {
                        Name = group.Name,
                        Color = group.Color,
                        Description = group.Description,
                        IsExpanded = group.IsExpanded,
                        Sequences = removed
                    };

                    if(removed.Any())
                        newSampleGroups.Add(newGroup);
                }
                else
                {
                    newSampleGroups.Add(group);
                }

            }
                SampleGroups = newSampleGroups;
        }

        // sample tree 
        private ObservableCollection<SampleGroup> _sampleGroups;
        public ObservableCollection<SampleGroup> SampleGroups
        {
            get => _sampleGroups;
            set
            {
                if (RaisePropertyChangedIfSet(ref _sampleGroups, value))
                    PropertyChangedSelectDataList();
            }
        }

        private SampleGroup _selectedSampleGroup;
        public SampleGroup SelectedSampleGroup
        {
            get => this._selectedSampleGroup;
            set
            {
                if (RaisePropertyChangedIfSet(ref _selectedSampleGroup, value))
                    PropertyChangedSelectDataList();
            }
        }
        #endregion
        
        #region Select TargetDatabase

        private List<string> _databases;  // initial set
        private int _databasesIdx;
        private string _tooltipDatabasePath;

        private IFlow flow = null;
        private string startDateTime;

        public List<string> Databases
        {
            get { return _databases.Select(s => Path.GetFileName(s)).ToList(); }
            set { RaisePropertyChangedIfSet(ref _databases, value); }
        }

        public int DatabasesIdx
        {
            get { return _databasesIdx; }
            set
            {
                if (RaisePropertyChangedIfSet(ref _databasesIdx , value))
                {
                    TooltipDatabasePath = _databases[_databasesIdx];
                    AnarysisCheck();   // database select check is not seq-count check
                }
            }
        }

        public string TooltipDatabasePath
        {
            get => _tooltipDatabasePath;
            set { RaisePropertyChangedIfSet(ref _tooltipDatabasePath, value); }
        }

        #endregion

        #region Create Plots
        private bool _isPlots = false;
        private bool _isEnablePlot = true;
        public bool IsPlots
        {
            get { return _isPlots; }
            set { RaisePropertyChangedIfSet(ref _isPlots, value); }
        }


        // Distance default value
        private string _pcoaDistance = Pcoa.defaultPcoaDistanceMetric;
        private string _nmdsDistance = Pcoa.defaultNmdsDistanceMetric;
        private string _permanovaPermutations = Permanova.defaultPermutations;
        public void OpenPlotsSetting()  // ボタン押下
        {
            AnarysisCheck();
            System.Diagnostics.Debug.WriteLine("plots setting call. ");
            if (_sampleGroups.Count() < 2)
            {
                IsPlots = false;
                return;
            }

            using (var plotsView = new PlotsSettingViewModel() {
                                                        SelectedNmdsDistance = _nmdsDistance,
                                                        SelectedPcoaDistance = _pcoaDistance,
                                                        SelectedPermanovaPermutations = _permanovaPermutations })
            {
                Messenger.Raise(new TransitionMessage(plotsView, TransitionMode.Modal, "PlotsSettingCommand"));
                _pcoaDistance = plotsView.SelectedPcoaDistance;
                _nmdsDistance = plotsView.SelectedNmdsDistance;
                _permanovaPermutations = plotsView.SelectedPermanovaPermutations;
                IsPlots = !plotsView.IsPlotsDisnable;
                _isEnablePlot = !plotsView.IsPlotsDisnable;
            }
        }

        #endregion

        #region Kmer target
        private List<string> _kmers;
        public List<string> Kmers
        {
            get { return _kmers; }
            set { RaisePropertyChangedIfSet(ref _kmers, value); }
        }

        // private string _selectedKmers;
        // public string SelectedKmers
        // {
        // get { return _selectedKmers; }
        // set 
        //     {
        //          if (RaisePropertyChangedIfSet(ref _selectedKmers, value))
        // AnarysisCheck();
        // }
        // }

        private void SetKmers()
        {
            // var kmers = Several.AnywayUtils.GatKmers(this._tooltipDatabasePath);
            // Kmers = kmers.ToList();
        }
        #endregion

        #region  Analyses button 
        private string _analysisButton = anlysesButtonEnable;
        public string AnalysisButton
        {
            get => _analysisButton;
            set { RaisePropertyChangedIfSet(ref _analysisButton, value); }
        }

        private bool _isAnalysisExecute = false;
        public bool IsAnalysisExecute
        {
            get => _isAnalysisExecute;
            set { RaisePropertyChangedIfSet(ref _isAnalysisExecute, value); }
        }

        // 解析開始ボタン
        private string outDir;
        public void CallAnarysisExecute(FolderSelectionMessage m)
        {
            System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

            if (m == null || ! m.Response.Any())  // 出力フォルダの選択が無かった。
                return;

            // out put directory
            if (!Directory.Exists(m.Response.First()))
            {
                mainlog.Report("not found directory " + m.Response.First());
                return;
            }
            // ユーザ指定の出力フォルダ。
            this.outDir = m.Response.First();

            switch (this._analysisButton)
            {
                case anlysesButtonEnable:
                    Anarysis();
                    return;

                case anlysesButtonCancel:
                    ProcessCancel();
                    return;

                default:
                    ShowErrorDialog("Fatal error !!");
                    Application.Current.Shutdown();
                    return;
            }

        }
        #endregion

        // 画面入力チェック
        private void AnarysisCheck()
        {
            switch (TabPage)  // 選択しているTABで変わる。
            {
                case 1:  // Centrifuge tab
                    IsAnalysisExecute = isTaxonomicProfilingEnable();
                    break;
                case 2:  // mapping-blast tab 
                    IsAnalysisExecute = isEstimatedFunctionEnable();
                    break;
            }

        }

        private bool isSelectedSamples()
        {
            if (this._sampleGroups == null) // 初期化前
                return false;

            // sample-group数のチェック
            if (this._sampleGroups.Count() > 2 && this._isEnablePlot)
                IsPlots = true;

            return true;  // 最低限 sample group がある
        }

        private bool isTaxonomicProfilingEnable()  // Centrifuge 
        {
            if (! isSelectedSamples() )
                return false;

            // sample & databese is enable ?
            if (this._sampleGroups.Any()
                    && !string.IsNullOrEmpty(this._tooltipDatabasePath)
                    && _databasesIdx > 0)
                return true;

            return false;
        }

        private bool isEstimatedFunctionEnable()
        {
            if (!isSelectedSamples())
                return false;

            // sample & databese is enable ?
            if (this._sampleGroups.Any()
                    && !string.IsNullOrEmpty(this._mappingReferenceTooltip)
                    && _mappingReferenceIdx > 0)
                return true;

            return false;
        }

        // 解析開始
        private void Anarysis()
        {

            if (!_isAnalysisExecute)    // livet 経由で既にフォルダが指定されている
            {
                mainlog.Report("not set parameter, files.... Unable to execute");
                return;
            }

            this.startDateTime = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            switch (TabPage)  // 選択しているTABで変わる。
            {
                case 1:  // Centrifuge tab
                    TaxonomicProfiling();
                    break;
                case 2:  // mapping-blast tab 
                    EstimatedFunction();
                    break;
            }


        }

        private void TaxonomicProfiling() 
        { 
            var colors = _sampleGroups.Select(s => s.Color.Name);
            this.flow = new BasicAnalysisProc(
                                new AnalysisProperties()
                                {
                                    useDb = _tooltipDatabasePath,
                                    groups = _sampleGroups,
                                    outDirectory = this.outDir,
                                    startDateTime = this.startDateTime,
                                    isPlotsEnable = this._isEnablePlot,
                                    usePcoaDistance = this._pcoaDistance, 
                                    useNmdsDistance = this._nmdsDistance.ToLower(),
                                    plotColors = _sampleGroups.Select(s => s.Color.Name),
                                    progress = mainlog
                                }
                        );

            AnalysisButton = anlysesButtonCancel;
            IsAnalysisExecute = false;  // livet 経由で既にフォルダが指定されている TODO　

            // ログタイマー ここまで来ていれば IFlow は有効
            this.dispTimer.Start();
            _ = ProcessAsync(flow); // 非同期
        }

        // cog data func-gene
        private void EstimatedFunction()
        {
            // mapping reference で Full data の場合は9 の文字がない。。。はず。 TODO：見分ける方法を別途作成？
            var isLarge = ! _mappingReference.ElementAt(_mappingReferenceIdx).Contains("9");


            this.flow = new FuncGeneProc(
                                    new FuncGeneAnalysisProperties()
                                    {
                                        groups = _sampleGroups,
                                        MappingReferene = _mappingReference.ElementAt(_mappingReferenceIdx),
                                        isFullReference = isLarge,
                                        outDirectory = this.outDir,
                                        startDateTime = this.startDateTime,
                                        progress = mainlog
                                    });

            AnalysisButton = anlysesButtonCancel;
            IsAnalysisExecute = false;  // livet 経由で既にフォルダが指定されている TODO　

            // ログタイマー ここまで来ていれば IFlow は有効
            this.dispTimer.Start();
            _ = ProcessAsync(flow); // 非同期

        }

    }
}
