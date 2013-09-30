// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

namespace fCraft {
    /// <summary> Client Protocol Extension list.
    /// See http://wiki.vg/Classic_Protocol_Extension for details. </summary>
    public enum CpeExtension {
        /// <summary> Used to extend or restrict the distance at which client may click blocks,
        /// controlled by the server. </summary>
        ClickDistance,

        /// <summary> Used to add support for custom block types. </summary>
        CustomBlocks,

        /// <summary> Provides a way for the client to notify the server about the block type that it is
        /// currently holding, and for the server to change the currently-held block type. </summary>
        HeldBlock,

        /// <summary> Indicates that the client can render emotes in chat properly,
        /// without padding or suffixes that are required for vanilla client. </summary>
        EmoteFix,

        /// <summary> Allows the server to define "hotkeys" for certain commands. </summary>
        TextHotKey,

        /// <summary> Provides more flexibility in naming of players and loading of skins,
        /// autocompletion, and player tab-list display. Separates tracking of in-game
        /// entities (spawned player models) and names on the player list. </summary>
        ExtPlayerList,

        /// <summary> Allows server to alter some of the colors used by the client in
        /// environment rendering. </summary>
        EnvColors,

        /// <summary> Allows the server to highlight parts of a world. Applications include zoning,
        /// previewing draw command size, previewing undo commands. </summary>
        SelectionCuboid,

        /// <summary> This extension allows the server to instruct the player that certain block
        /// types are allowed/disallowed to be placed or deleted. </summary>
        BlockPermissions,

        /// <summary> Allows changing appearance of player models in supporting clients. </summary>
        ChangeModel,

        /// <summary> This extension allows the server to specify custom terrain textures,
        /// and tweak appearance of map edges. </summary>
        EnvMapAppearance
    }


    public enum EnvVariable : byte {
        SkyColor = 0,
        CloudColor = 1,
        FogColor = 2,
        Shadow = 3,
        Sunlight = 4
    }
}
