using System;

namespace fCraft {
    public interface IPlugin {
        string Name { get; }
        string Description { get; }
        Uri Website { get; }
        Version Version { get; }

        string[] LoadDependencies { get; }
        string[] RunDependencies { get; }

        bool Load();
    }
}