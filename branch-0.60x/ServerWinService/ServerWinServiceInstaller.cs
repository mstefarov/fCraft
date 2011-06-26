// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace fCraft.ServerWinService {
    [RunInstaller( true )]
    public class ServerWinServiceInstaller : Installer {
        public ServerWinServiceInstaller() {
            ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller {
                Username = null,
                Password = null,
                Account = ServiceAccount.LocalSystem
            };
            ServiceInstaller serviceInstaller = new ServiceInstaller {
                ServiceName = ServerWinService.Name,
                DisplayName = ServerWinService.Description,
                StartType = ServiceStartMode.Manual
            };
            Installers.Add( serviceInstaller );
            Installers.Add( serviceProcessInstaller );
        }
    }
}