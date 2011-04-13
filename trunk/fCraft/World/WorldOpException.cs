using System;

namespace fCraft {
    public sealed class WorldOpException : Exception {

        public WorldOpExceptionCode ErrorCode { get; private set; }

        public WorldOpException( string worldName, WorldOpExceptionCode errorCode )
            : base( GetMessage( worldName, errorCode ) ) {
            ErrorCode = errorCode;
        }

        public WorldOpException( string worldName, WorldOpExceptionCode errorCode, string message )
            : base( message ) {
            ErrorCode = errorCode;
        }

        public WorldOpException( string worldName, WorldOpExceptionCode errorCode, Exception innerException )
            : base( GetMessage( worldName, errorCode ), innerException ) {
            ErrorCode = errorCode;
        }

        public WorldOpException( string worldName, WorldOpExceptionCode errorCode, string message, Exception innerException )
            : base( message, innerException ) {
            ErrorCode = errorCode;
        }

        public static string GetMessage( string worldName, WorldOpExceptionCode code ) {

            if( worldName != null ) {
                switch( code ) {
                    case WorldOpExceptionCode.CannotDoThatToMainWorld:
                        return "This operation cannot be done on the main world (" +
                               worldName + "). Assign a new main world and try again.";

                    case WorldOpExceptionCode.DuplicateWorldName:
                        return "A world with this name (\"" + worldName + "\") already exists.";

                    case WorldOpExceptionCode.InvalidWorldName:
                        return "World name \"" + worldName + "\" is invalid. " +
                               "Expected an alphanumeric name between 1 and 16 characters long.";

                    case WorldOpExceptionCode.MapLoadError:
                        return "Failed to load the map file for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.MapMoveError:
                        return "Failed to rename/move the map file for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.MapNotFound:
                        return "Could not find the map file for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.MapPathError:
                        return "Map file path is not valid for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.MapSaveError:
                        return "Failed to save the map file for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.NoChangeNeeded:
                        return "No change needed for world \"" + worldName + "\".";

                    case WorldOpExceptionCode.PluginDenied:
                        return "Operation for world \"" + worldName + "\" was cancelled by a plugin.";

                    case WorldOpExceptionCode.SecurityError:
                        return "You are not allowed to do this operation to world \"" + worldName + "\".";

                    case WorldOpExceptionCode.WorldNotFound:
                        return "No world found with the name \"" + worldName + "\".";

                    default:
                        return "Unexpected error occured while working on world \"" + worldName + "\"";
                }
            } else {
                switch( code ) {
                    case WorldOpExceptionCode.CannotDoThatToMainWorld:
                        return "This operation cannot be done on the main world. " +
                               "Assign a new main world and try again.";

                    case WorldOpExceptionCode.DuplicateWorldName:
                        return "A world with this name already exists.";

                    case WorldOpExceptionCode.InvalidWorldName:
                        return "Given world name is invalid. " +
                               "Expected an alphanumeric name between 1 and 16 characters long.";

                    case WorldOpExceptionCode.MapLoadError:
                        return "Failed to load the map file.";

                    case WorldOpExceptionCode.MapMoveError:
                        return "Failed to rename/move the map file.";

                    case WorldOpExceptionCode.MapNotFound:
                        return "Could not find the map file.";

                    case WorldOpExceptionCode.MapPathError:
                        return "Map file path is not valid.";

                    case WorldOpExceptionCode.MapSaveError:
                        return "Failed to save the map file.";

                    case WorldOpExceptionCode.NoChangeNeeded:
                        return "No change needed.";

                    case WorldOpExceptionCode.PluginDenied:
                        return "Operation cancelled by a plugin.";

                    case WorldOpExceptionCode.SecurityError:
                        return "You are not allowed to do this operation.";

                    case WorldOpExceptionCode.WorldNotFound:
                        return "Specified world was not found.";

                    default:
                        return "Unexpected error occured.";
                }
            }
        }
    }


    public enum WorldOpExceptionCode {
        NoChangeNeeded,

        InvalidWorldName,
        WorldNotFound,
        DuplicateWorldName,

        SecurityError,
        CannotDoThatToMainWorld,

        MapNotFound,
        MapPathError,
        MapLoadError,
        MapSaveError,
        MapMoveError,

        PluginDenied
    }
}