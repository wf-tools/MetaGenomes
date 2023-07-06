using Livet.Commands;
using Livet.Messaging.Windows;
using ShotgunMetagenome.Models;
using ShotgunMetagenome.ViewModels.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ShotgunMetagenome.ViewModels
{
    public class NewGroupViewModel : BaseGroupViewModel
    {

        public NewGroupViewModel() :base()
        {
            // CommandInit();
            System.Diagnostics.Debug.WriteLine("NewGroup ViewModel constructor.");

        }

        // この画面で追加されたサンプル情報
        public SampleGroup addedGroup { get; set; }

        // add group button
        public void CallAddGroup()
        {
            // close.
            this.addedGroup = new SampleGroup
            {
                Id = (int)groupCnt,
                Name = this._groupName,
                Color = this._groupColor,
            };
            Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));

        }

        public override void PropertyChangedSelectDataList()
        {
            // 特にやる事ない。
        }
    }
}
