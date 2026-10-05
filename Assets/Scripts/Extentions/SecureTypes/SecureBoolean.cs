using System;

namespace Assets.Scripts.Extentions.SecureTypes
{
    public struct SecureBoolean
    {
        public static readonly SecureBoolean True = new SecureBoolean(true);
        public static readonly SecureBoolean False = new SecureBoolean(false);

        private readonly Int32 _value;

        public SecureBoolean(Boolean value)
        {
            _value = value ? 684136 : 888413;
        }

        public static implicit operator Int32(SecureBoolean secureValue)
        {
            return secureValue._value;
        }

        public static implicit operator SecureBoolean(Boolean value)
        {
            return value ? True : False;
        }
    }
}