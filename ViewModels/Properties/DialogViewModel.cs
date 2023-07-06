using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ShotgunMetagenome.ViewModels.Properties
{
    public abstract class DialogViewModel : ViewModel
    {

        public const string anlysesButtonEnable = "Analyses";
        public const string anlysesButtonCancel = "Cancel";

        protected ObservableCollection<string> _selectDataList;
        public ObservableCollection<string> SelectDataList
        {
            get => _selectDataList;
            set {
                if (RaisePropertyChangedIfSet(ref _selectDataList, value))
                    PropertyChangedSelectDataList();
            }

        }
        abstract public void PropertyChangedSelectDataList();


        #region sample list clear
        protected virtual void CallSelectedSampleClear()
        {
            SelectDataList = new ObservableCollection<string>();
            RaisePropertyChanged(SelectDataList.GetType().Name);
        }
        #endregion

        // Select Data ListView Drag & Drop
        ListenerCommand<IEnumerable<Uri>> m_addItemsCommand;
        // ICommandを公開する
        public ICommand AddItemsCommand
        {
            get
            {
                if (m_addItemsCommand == null)
                {
                    m_addItemsCommand = new ListenerCommand<IEnumerable<Uri>>(AddItems);
                }
                return m_addItemsCommand;
            }
        }

        private void AddItems(IEnumerable<Uri> urilist)
        {
            //var urilist = (IEnumerable<Uri>)arg;
            var list = urilist.Select(s => s.LocalPath).ToList();
            CreateSelectDataList(list);
            // IsDirty = true;
        }

        // Folder File Select
        protected virtual void CreateSelectDataList(IEnumerable<string> files)
        {
            var newData = new List<string>();
            var cautionData = new List<string>();
            foreach (var file in files)
            {
                if (!WfComponent.Utils.FileUtils.IsOneByteString(file))
                    cautionData.Add(file);
                else
                    newData.Add(file);
            }

            // 同じFolder/Fileがある？
            if (newData.Any())
            {
                foreach (var dat in newData.Distinct())
                {
                    if (!_selectDataList.Contains(dat))
                        _selectDataList.Add(dat);
                }
                RaisePropertyChanged(nameof(SelectDataList));
                PropertyChangedSelectDataList();
            }
        }


        // DataList フォルダー・ファイルの削除
        public string SelectDataItem { get; set; }



        public string InformationMessageKey = "Information";
        public string ErrorMessageKey = "Error";
        public string ConfirmMessageKey = "Confirm";
        
        public EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public void ShowInfoDialog(string message, string title = "Information")
        {
            Messenger.Raise(new InformationMessage(message, title, MessageBoxImage.Asterisk, InformationMessageKey));
        }

        public void ShowErrorDialog(string message, string title = "Error")
        {
            Messenger.Raise(new InformationMessage(message, title, MessageBoxImage.Error, ErrorMessageKey));
        }

        public bool ShowConfirmDialog(string message, string title = "Confirm")
        {
            ConfirmationMessage confirmationMessage = new ConfirmationMessage(message, title, MessageBoxImage.Question, MessageBoxButton.OKCancel, ConfirmMessageKey);
            Messenger.Raise(confirmationMessage);
            return confirmationMessage.Response.GetValueOrDefault();
        }

        public string SelectedDir
        {
            get
            {
                if (string.IsNullOrEmpty(ShotgunMetagenome.Properties.Settings.Default.select_dir))
                    return Path.Combine(@"C:\Users", Environment.UserName, "Documents");

                return ShotgunMetagenome.Properties.Settings.Default.select_dir;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(value);
                    if (directoryInfo.Name == directoryInfo.Root.Name)
                        ShotgunMetagenome.Properties.Settings.Default.select_dir = directoryInfo.Root.Name;
                    else
                        ShotgunMetagenome.Properties.Settings.Default.select_dir = directoryInfo.Parent.FullName;

                }
                ShotgunMetagenome.Properties.Settings.Default.Save();
            }
        }

        public string SaveDir
        {
            get
            {
                if (string.IsNullOrEmpty(ShotgunMetagenome.Properties.Settings.Default.save_dir))
                    return Path.Combine(@"C:\Users", Environment.UserName, "Documents");

                return ShotgunMetagenome.Properties.Settings.Default.save_dir;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(value);

                    if (directoryInfo.Name == directoryInfo.Root.Name)
                        ShotgunMetagenome.Properties.Settings.Default.save_dir = directoryInfo.Root.Name;
                    else
                        ShotgunMetagenome.Properties.Settings.Default.save_dir = directoryInfo.Parent.FullName;

                    ShotgunMetagenome.Properties.Settings.Default.Save();
                }
            }
        }

        
        // cancel-commit = View Close 
        protected void ViewClose()
        {
            DispatcherHelper.UIDispatcher.BeginInvoke((Action)(() =>
            {
                Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
            }));
        }

        public enum CommonFileDialogResult
        {
            None,
            Ok,
            Cancel
        }

    }
}

