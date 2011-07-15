// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft.Drawing {
    public static class BrushManager {
        static Dictionary<string, IBrush> Brushes = new Dictionary<string, IBrush>();

        public static void RegisterBrush( IBrush factory ) {
            if( factory == null ) throw new ArgumentNullException( "factory" );
            Brushes.Add( factory.Name.ToLower(), factory );
        }

        public static IBrush GetBrush( string brushName ) {
            if( brushName == null ) throw new ArgumentNullException( "brushName" );
            IBrush factory;
            if( Brushes.TryGetValue( brushName.ToLower(), out factory ) ) {
                return factory;
            } else {
                return null;
            }
        }
    }
}
