using Livet.Messaging.IO;
using Livet.Messaging.Windows;
using ShotgunMetagenome.Models;
using ShotgunMetagenome.ViewModels.Properties;
using System.Linq;

namespace ShotgunMetagenome.ViewModels
{
    public class AddGroupViewModel : BaseGroupViewModel
    {
        // この画面に設定されたグループ数　呼び出し元から設定される
        // public int? groupCnt { get; set; }


        // この画面で追加されたサンプル情報
        public SampleGroup addedGroup { get; set; }

        public AddGroupViewModel() : base()
        {
            // CommandInit();
            System.Diagnostics.Debug.WriteLine("AddGroup ViewModel constructor.");

        }

        // Select Data ListView Drag & Drop
        // sample file drag&drop 
        public override void PropertyChangedSelectDataList()
        {

            // 特にやる事ない。

        }

        // File select 
        public void CallAddSequenceGroup(OpeningFileSelectionMessage msg)
        {
            if (msg.Response != null && msg.Response.Any())
            {
                // selectedFastqs = msg.Response;
                CreateSelectDataList(msg.Response);  // add SampleList 
            }
        }

        // add group button
        public void CallAddGroup()
        {
            if( this._groupColor == null)
                this._groupColor = new ColorModel();

            // close.
            this.addedGroup = new SampleGroup
            {
                Name = this._groupName,
                Color = this._groupColor,
            };
            addedGroup.SetFilePaths(this._selectDataList);
            Messenger.Raise(new WindowActionMessage(WindowAction.Close, "Close"));

        }


    }
}
