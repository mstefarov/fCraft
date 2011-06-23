using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    public interface IPlugin {
        string Name { get; }
        string Description { get; }
        Uri Website { get; }
        Version Version { get; }

        bool Load();
    }
}
