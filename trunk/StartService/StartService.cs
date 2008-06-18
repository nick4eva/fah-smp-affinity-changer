using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Collections;


namespace StartService
{
    [RunInstaller(true)]
    public partial class StartService : Installer
    {
        public StartService()
        {
            InitializeComponent();
        }

        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            Process.Start("net", "start fahsmpaffinitychanger");
        }
    }
}
