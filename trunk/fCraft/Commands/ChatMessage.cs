/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft {
    public class ChatMessage {
        public string Message { get; set; }

        public ChatMessageType Type { get; set; }

        public Player Source { get; private set; }

        public PermissionOverride SeeingCheck { get; set; }

        public PermissionOverride IgnoredCheck { get; set; }

        public PermissionOverride SelfCheck { get; set; }




        #region Filters

        public interface IChatMessageFilter {
            FilterAction Action { get; set; }
            void Apply( ChatMessage message, HashSet<Player> players );
        }


        public class SelfFilter : IChatMessageFilter {
            public FilterAction Action { get; set; }

            public void Apply( ChatMessage message, HashSet<Player> players ) {
                switch( Action ) {
                    case FilterAction.Add:
                        players.Add( message.Source );
                        break;

                    case FilterAction.AddInverse:
                        foreach( Player p in Server.PlayerList.Where( p => p != message.Source ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.Keep:
                        players.RemoveWhere( p => p != message.Source );
                        break;

                    case FilterAction.Remove:
                        players.Remove( message.Source );
                        break;
                }
            }
        }


        public class IgnoredFilter : IChatMessageFilter {
            public FilterAction Action { get; set; }
            public void Apply( ChatMessage message, HashSet<Player> players ) {
                Player[] plist = Server.PlayerList;
                switch( Action ) {
                    case FilterAction.Add:
                        foreach( Player p in plist.Where( p => p.IsIgnoring( message.Source.Info ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.AddInverse:
                        foreach( Player p in plist.Where( p => !p.IsIgnoring( message.Source.Info ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.Keep:
                        players.RemoveWhere( p => !p.IsIgnoring( message.Source.Info ) );
                        break;

                    case FilterAction.Remove:
                        players.RemoveWhere( p => p.IsIgnoring( message.Source.Info ) );
                        break;
                }
            }
        }


        public class SeeFilter : IChatMessageFilter {
            public FilterAction Action { get; set; }
            public void Apply( ChatMessage message, HashSet<Player> players ) {
                Player[] plist = Server.PlayerList;
                switch( Action ) {
                    case FilterAction.Add:
                        foreach( Player p in plist.Where( p => p.CanSee( message.Source ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.AddInverse:
                        foreach( Player p in plist.Where( p => !p.CanSee( message.Source ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.Keep:
                        players.RemoveWhere( p => !p.CanSee( message.Source ) );
                        break;

                    case FilterAction.Remove:
                        players.RemoveWhere( p => p.CanSee( message.Source ) );
                        break;
                }
            }
        }


        public class PermissionFilter : HashSet<Permission>, IChatMessageFilter {
            public FilterAction Action { get; set; }


            public void Apply( ChatMessage message, HashSet<Player> players ) {
                Player[] plist = Server.PlayerList;
                Rank sourceRank = message.Source.Info.Rank;
                switch( Action ) {
                    case FilterAction.Add:
                        foreach( Permission perm in this ) {
                            foreach( Player p in plist.Where( p => p.Can( perm, sourceRank ) ) ) {
                                players.Add( p );
                            }
                        }
                        break;

                    case FilterAction.AddInverse:
                        foreach( Permission perm in this ) {
                            foreach( Player p in plist.Where( p => !p.Can( perm, sourceRank ) ) ) {
                                players.Add( p );
                            }
                        }
                        break;

                    case FilterAction.Keep:
                        foreach( Permission perm in this ) {
                            players.RemoveWhere( p => !p.Can( perm, sourceRank ) );
                        }
                        break;

                    case FilterAction.Remove:
                        foreach( Permission perm in this ) {
                            players.RemoveWhere( p => p.Can( perm, sourceRank ) );
                        }
                        break;
                }
            }
        }


        public class WorldFilter : HashSet<World>, IChatMessageFilter {
            public FilterAction Action { get; set; }

            public void Apply( ChatMessage message, HashSet<Player> players ) {
                Player[] plist = Server.PlayerList;
                Rank sourceRank = message.Source.Info.Rank;
                switch( Action ) {
                    case FilterAction.Add:
                        foreach( Player p in plist.Where( p => Contains( p.World ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.AddInverse:
                        foreach( Player p in plist.Where( p => !Contains( p.World ) ) ) {
                            players.Add( p);
                        }
                        break;

                    case FilterAction.Keep:
                        players.RemoveWhere( p => !Contains( p.World ) );
                        break;

                    case FilterAction.Remove:
                        players.RemoveWhere( p => !Contains( p.World ) );
                        break;
                }
            }
        }


        public class RankFilter : HashSet<Rank>, IChatMessageFilter {
            public FilterAction Action { get; set; }

            public void Apply( ChatMessage message, HashSet<Player> players ) {
                Player[] plist = Server.PlayerList;
                switch( Action ) {
                    case FilterAction.Add:
                        foreach( Player p in plist.Where( p => Contains( p.Info.Rank ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.AddInverse:
                        foreach( Player p in plist.Where( p => !Contains( p.Info.Rank ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.Keep:
                        players.RemoveWhere( p => !Contains( p.Info.Rank ) );
                        break;

                    case FilterAction.Remove:
                        players.RemoveWhere( p => !Contains( p.Info.Rank ) );
                        break;
                }
            }
        }


        public class PlayerFilter : HashSet<Player>, IChatMessageFilter {
            public FilterAction Action { get; set; }

            public void Apply( ChatMessage message, HashSet<Player> players ) {
                switch( Action ) {
                    case FilterAction.Add:
                        foreach( Player p in this ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.AddInverse:
                        foreach( Player p in Server.PlayerList.Where( p => !Contains( p ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.Keep:
                        players.RemoveWhere( p => !Contains( p ) );
                        break;

                    case FilterAction.Remove:
                        foreach( Player p in this){
                            players.Remove( p );
                        }
                        break;
                }
            }
        }


        public class CustomFilter : IChatMessageFilter {
            Func<Player, ChatMessage, bool> checkFunction;
            public CustomFilter( Func<Player, ChatMessage, bool> checkFunction ) {
                this.checkFunction = checkFunction;
            }

            public FilterAction Action { get; set; }

            public void Apply( ChatMessage message, HashSet<Player> players ) {
                Player[] plist = Server.PlayerList;
                switch( Action ) {
                    case FilterAction.Add:
                        foreach( Player p in plist.Where( p => checkFunction(p,message) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.AddInverse:
                        foreach( Player p in plist.Where( p => !checkFunction( p, message ) ) ) {
                            players.Add( p );
                        }
                        break;

                    case FilterAction.Keep:
                        players.RemoveWhere( p => !checkFunction( p, message ) );
                        break;

                    case FilterAction.Remove:
                        players.RemoveWhere( p => checkFunction( p, message ) );
                        break;
                }
            }
        }


        public enum FilterAction {
            Add,
            AddInverse,
            Keep,
            Remove
        }

        #endregion
    }



    


public enum ChatMessageType {
    Global,
    Me,
    PM,
    Rank,
    Say,
    Staff,
    World,
    IRC
}
}

#region EventArgs

namespace fCraft.Events {
    public sealed class PlayerSendingMessageEventArgs : PlayerEventArgs {
        public PlayerSendingMessageEventArgs( ChatMessage message )
            : base( message.Source ) {
            Message = message;
        }

        public ChatMessage Message { get; private set; }

        public bool Cancel { get; set; }
    }


    public sealed class PlayerSentMessageEventArgs : PlayerEventArgs {
        public PlayerSentMessageEventArgs( ChatMessage message )
            : base( message.Source ) {
            Message = message;
        }

        public ChatMessage Message { get; private set; }
    }
}

#endregion
*/