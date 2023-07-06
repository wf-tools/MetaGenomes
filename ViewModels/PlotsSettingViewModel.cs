using Livet.Messaging.Windows;
using ShotgunMetagenome.External;
using ShotgunMetagenome.ViewModels.Properties;
using System.Collections.ObjectModel;
using System.Linq;

namespace ShotgunMetagenome.ViewModels
{
    public class PlotsSettingViewModel : BaseGroupViewModel
    {

        public PlotsSettingViewModel() : base()
        {
            // CommandInit();
            System.Diagnostics.Debug.WriteLine("PlotsSettingViewModell constructor.");
        }

        public new void Initialize()
        {
            System.Diagnostics.Debug.WriteLine("PlotsSettingViewModell initialize.");
            PcoaDistances = new ObservableCollection<string>(Pcoa.pcoaDistanceMetric);
            NmdsDistances = new ObservableCollection<string>(Pcoa.nmdsDistanceMetric);
            PermanovaPermutations = new ObservableCollection<string>(Permanova.permutations);

            if (string.IsNullOrEmpty(_selectedPcoaDistance))
                SelectedPcoaDistance = Pcoa.defaultPcoaDistanceMetric; // default
            if(string.IsNullOrEmpty(_selectedNmdsDistance))
                SelectedNmdsDistance = Pcoa.defaultNmdsDistanceMetric; // default
            if (string.IsNullOrEmpty(_selectedPermanovaPermutations))
                SelectedPermanovaPermutations = Permanova.defaultPermutations; // default
        }

        // PCoA distances
        private string _selectedPcoaDistance;
        public string SelectedPcoaDistance
        {
            get => _selectedPcoaDistance;
            set => RaisePropertyChangedIfSet(ref _selectedPcoaDistance, value);
        }
        private ObservableCollection<string> _pcoaDistances;
        public ObservableCollection<string> PcoaDistances
        {
            get => _pcoaDistances;
            set => RaisePropertyChangedIfSet(ref _pcoaDistances, value);
        }

        // nMDS distances
        private string _selectedNmdsDistance;
        public string SelectedNmdsDistance
        {
            get => _selectedNmdsDistance;
            set => RaisePropertyChangedIfSet(ref _selectedNmdsDistance, value);
        }
        private ObservableCollection<string> _nmdsDistances;
        public ObservableCollection<string> NmdsDistances
        {
            get => _nmdsDistances;
            set => RaisePropertyChangedIfSet(ref _nmdsDistances, value);
        }

        // permanova distances
        private string _selectedPermanovaPermutations;
        public string SelectedPermanovaPermutations
        {
            get => _selectedPermanovaPermutations;
            set => RaisePropertyChangedIfSet(ref _selectedPermanovaPermutations, value);
        }
        private ObservableCollection<string> _permanovaPermutations;
        public ObservableCollection<string> PermanovaPermutations
        {
            get => _permanovaPermutations;
            set => RaisePropertyChangedIfSet(ref _permanovaPermutations, value);
        }



        public bool IsPlotsDisnable = false;

        // ok button
        public void CallEndPlotsSetting()
        {
            // close. default setting....
            Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
        }

        // Disnable button
        public void CallEndDisnable()
        {
            IsPlotsDisnable = true;
            // close. default setting....
            Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));
        }


        public void CallOpenPcoa()
        {
            // TODO 
            // PCoA の説明とかを出す？？
        }

        public void CallOpenNmds()
        {
            // TODO 
            // nMDS の説明とかを出す？？
        }

        public void CallOpenPermanova()
        {
            // TODO 
            // nMDS の説明とかを出す？？
        }
        // このView では何もない。
        public override void PropertyChangedSelectDataList() {}
    }
}
