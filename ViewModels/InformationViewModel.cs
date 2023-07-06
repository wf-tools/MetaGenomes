using Livet.Commands;
using Livet.Messaging.IO;
using ShotgunMetagenome.Utils;
using ShotgunMetagenome.ViewModels.Properties;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ShotgunMetagenome.ViewModels
{
    public class InformationViewModel : DialogViewModel
    {
        public InformationViewModel()
        {
            // CommandInit();
            System.Diagnostics.Debug.WriteLine("information ViewModel constructor.");
            _licenseFile = string.Empty;
        }

        public void Initialize() // ContentRendered.
        {
            System.Diagnostics.Debug.WriteLine("information ViewModel Initialize");
            this._selectDataList = new ObservableCollection<string>();

        }

        // Select Data ListView Drag & Drop
        public override void PropertyChangedSelectDataList()
        {

            // License file read....

        }

        public void CallOpenManual(string pdf = null)
        {
            System.Diagnostics.Debug.WriteLine("call open manual.");
            if (string.IsNullOrEmpty(pdf))
                pdf = Path.Combine(
                                    System.AppDomain.CurrentDomain.BaseDirectory,
                                    "data",
                                    "WHMetagenome.pdf");
            else
                pdf = Path.Combine(
                                    System.AppDomain.CurrentDomain.BaseDirectory,
                                    "data",
                                    pdf);

            if (File.Exists(pdf))
                Approbate.OpenApp(pdf);
            else
                Approbate.OpenUrl("https://www.w-fusion.co.jp/metagenome-kin");
        }

        public void CallOpenContact()
        {
            System.Diagnostics.Debug.WriteLine("call open url.");
            Approbate.OpenUrl("https://www.w-fusion.co.jp/contact");
        }


        public bool IsLicenceActivate = false;
        public void CallSelectLicense(OpeningFileSelectionMessage msg)
        {
            System.Diagnostics.Debug.WriteLine("call open lisence.");
            if (msg.Response != null && msg.Response.Length == 1)
            {
                _licenseFile = msg.Response.Single();
            }

            if (File.Exists(_licenseFile))
            {
                // 
                System.Diagnostics.Debug.WriteLine("found " + _licenseFile);
                CallAcceptLicense();
            }

            // File null (close window) =>  IsActivateLicence = false;
        }

        public void CallAcceptLicense()
        {
            if (File.Exists(this._licenseFile))
            {
                var message = string.Empty;
                WfComponent.Utils.FileUtils.FileCopy(
                                            _licenseFile,   // user select file pass
                                            Approbate.DefaultLicenceFilePath,
                                            ref message);

                System.Threading.Thread.Sleep(1000);
                if (string.IsNullOrEmpty(message))
                {
                    // copy success.
                    if (EnvInfo.IsLicenceInvalid())// license invalid
                    {
                        ViewClose(); // 閉じる
                        return; // IsActivateLicence = false;
                    }
                    else
                    {   // 
                        ShowInfoDialog("license is activated.", "license file accept.");
                        ShotgunMetagenome.Properties.Settings.Default.demo_mode = string.Empty;
                        ShotgunMetagenome.Properties.Settings.Default.Save();
                        IsLicenceActivate = true;
                    }
                }
                else
                {
                    ShowErrorDialog("license file copy error");
                }
            }
            // File null (close window) =>  IsActivateLicence = false;
            ViewClose(); // 閉じる
        }

        private string _nicAddress = "your address :  " + Utils.EnvInfo.firstAddress;
        public string NicAddress
        {
            get { return _nicAddress; }
            set { RaisePropertyChangedIfSet(ref _nicAddress, value); }
        }


        private string _licenseFile;
        public string LicenseFile
        {
            get { return _licenseFile; }
            set { RaisePropertyChangedIfSet(ref _licenseFile, value); }
        }


    }
}
