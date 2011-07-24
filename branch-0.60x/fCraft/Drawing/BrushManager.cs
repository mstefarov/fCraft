// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public static class BrushManager {
        static readonly Dictionary<string, IBrushFactory> BrushFactories = new Dictionary<string, IBrushFactory>();

        internal static void Init() {
            RegisterBrush( NormalBrushFactory.Instance );
            RegisterBrush( CheckeredBrushFactory.Instance );
            RegisterBrush( RandomBrushFactory.Instance );
            RegisterBrush( RainbowBrush.Instance );
            RegisterBrush( CloudyBrushFactory.Instance );
        }

        public static void RegisterBrush( IBrushFactory factory ) {
            if( factory == null ) throw new ArgumentNullException( "factory" );
            BrushFactories.Add( factory.Name.ToLower(), factory );
        }

        public static IBrushFactory GetBrushFactory( string brushName ) {
            if( brushName == null ) throw new ArgumentNullException( "brushName" );
            IBrushFactory factory;
            if( BrushFactories.TryGetValue( brushName.ToLower(), out factory ) ) {
                return factory;
            } else {
                return null;
            }
        }
    }
}
