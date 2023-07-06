using Livet.Commands;
using ShotgunMetagenome.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace ShotgunMetagenome.ViewModels.Properties
{

    // グループ作成の共通パラメータ
    public abstract class BaseGroupViewModel : DialogViewModel
    {

        public BaseGroupViewModel()
        {
            this.ColorList = new ObservableCollection<ColorModel>(Models.ColorList.GetList()); ; // list init
            _groupColor = new ColorModel() { Color = new Color(), Name = "Gray" };

            this._selectDataList = new ObservableCollection<string>();
        }

        // View read される度に Call される。
        public void Initialize()
        {
            // group-count 
            groupCnt = (groupCnt == null || groupCnt == 0)
                                ? 1
                                : groupCnt + 1;

            this._groupName = "Group-" + groupCnt.ToString();

            this._groupNames = new ObservableCollection<string>();
            _groupNames.Add(_groupName); //

            if (registeredGroupNames != null && registeredGroupNames.Any())
                foreach (var name in registeredGroupNames)
                    _groupNames.Add(name);

            RaisePropertyChanged(nameof(GroupNames));
            RaisePropertyChanged(nameof(GroupName));
        }


        // この画面に設定されたグループ数　呼び出し元から設定される
        public int? groupCnt { get; set; }

        // group name 
        protected string _groupName;
        public string GroupName
        {
            get => _groupName;
            set
            {
                if (RaisePropertyChangedIfSet(ref _groupName, value))
                {
                    GroupNameOpacity = 1.0f;
                }
            }
        }

        protected float _groupNameOpacity = 0.7f;
        public float GroupNameOpacity
        {
            get => _groupNameOpacity;
            set { RaisePropertyChangedIfSet(ref _groupNameOpacity, value); }
        }

        public IEnumerable<string> registeredGroupNames;
        protected ObservableCollection<string> _groupNames;
        public ObservableCollection<string> GroupNames
        {
            get => _groupNames;
            set
            {
                if (RaisePropertyChangedIfSet(ref _groupNames, value))
                    PropertyChangedSelectDataList();
            }
        }



        protected ColorModel _groupColor;
        public ColorModel GroupColor
        {
            get => _groupColor;
            set { RaisePropertyChangedIfSet(ref _groupColor, value); }
        }

        protected ObservableCollection<ColorModel> _colorList;
        public ObservableCollection<ColorModel> ColorList
        {
            get => _colorList;
            set
            {
                if (RaisePropertyChangedIfSet(ref _colorList, value))
                    PropertyChangedSelectDataList();
            }
        }

    }
}
