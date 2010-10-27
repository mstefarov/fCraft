using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;


namespace fCraftWinService {
    [RunInstallerAttribute( true )]
    public class fCraftWinServiceInstaller : Installer {
        private ServiceInstaller serviceInstaller;
        private ServiceProcessInstaller serviceProcessInstaller;

        public fCraftWinServiceInstaller() {
            serviceProcessInstaller = new ServiceProcessInstaller();
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller = new ServiceInstaller();
            serviceInstaller.ServiceName = fCraftWinService.Name;
            serviceInstaller.DisplayName = fCraftWinService.Description;
            serviceInstaller.StartType = ServiceStartMode.Manual;
            Installers.Add( this.serviceInstaller );
            Installers.Add( this.serviceProcessInstaller );
        }
    }
}