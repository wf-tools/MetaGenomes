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

        #region Select Mapping Reference
        private List<string> _mappingReference;  // initial set
        private int _mappingReferenceIdx;
        private string _mappingReferenceTooltip;

        public List<string> MappingReference
        {
            get { return _mappingReference.Select(s => Path.GetFileName(s)).ToList(); }
            set { RaisePropertyChangedIfSet(ref _mappingReference, value); }
        }

        public int MappingReferenceIdx
        {
            get { return _mappingReferenceIdx; }
            set
            {
                if (RaisePropertyChangedIfSet(ref _mappingReferenceIdx, value))
                {
                    MappingReferenceTooltip = _mappingReference[_mappingReferenceIdx];
                    AnarysisCheck();   // database select check is not seq-count check
                }
            }
        }

        public string MappingReferenceTooltip
        {
            get => _mappingReferenceTooltip;
            set { RaisePropertyChangedIfSet(ref _mappingReferenceTooltip, value); }
        }


        #endregion Select TargetDatabase
    }
}
