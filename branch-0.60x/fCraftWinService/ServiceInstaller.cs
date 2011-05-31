// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace fCraftWinService {
    [RunInstaller( true )]
    public class fCraftWinServiceInstaller : Installer {
        public fCraftWinServiceInstaller() {
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller {
                Username = null,
                Password = null,
                Account = ServiceAccount.LocalSystem
            };
            ServiceInstaller serviceInstaller = new ServiceInstaller {
                ServiceName = fCraftWinService.Name,
                DisplayName = fCraftWinService.Description,
                StartType = ServiceStartMode.Manual
            };
            Installers.Add( serviceInstaller );
            Installers.Add( serviceProcessInstaller );
        }
    }
}