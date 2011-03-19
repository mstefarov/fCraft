using System;

namespace fCraft {
    class EnumException<T> : Exception
        where T : struct, IComparable, IFormattable, IConvertible {

        public T ErrorCode { get; private set; }

        public EnumException( T errorCode ) {
            ErrorCode = errorCode;
        }

        public EnumException( T errorCode, string message )
            : base( message ) {
            ErrorCode = errorCode;
        }

        public EnumException( T errorCode, string message, Exception innerException )
            : base( message, innerException ) {
            ErrorCode = errorCode;
        }

    }
}