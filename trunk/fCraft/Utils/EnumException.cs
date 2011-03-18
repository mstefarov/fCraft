using System;

namespace fCraft {
    class EnumException<T> : Exception
        where T : struct, IComparable, IFormattable, IConvertible {

        public T ErrorCode { get; private set; }

        public EnumException( T _errorCode ) {
            ErrorCode = _errorCode;
        }

        public EnumException( T _errorCode, string _message )
            : base( _message ) {
            ErrorCode = _errorCode;
        }

        public EnumException( T _errorCode, string _message, Exception _innerException )
            : base( _message, _innerException ) {
            ErrorCode = _errorCode;
        }

    }
}