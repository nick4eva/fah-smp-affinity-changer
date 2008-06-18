using System;
using System.ComponentModel;
using System.Configuration.Install;

namespace FahSmpAffinityChanger
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}