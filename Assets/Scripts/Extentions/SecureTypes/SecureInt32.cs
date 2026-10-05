using System;
using Random = UnityEngine.Random;

namespace Assets.Scripts.Extentions.SecureTypes
{
    public struct SecureInt32
    {
        private readonly Int32 _offset;
        private readonly Int32 _value;
        
        public SecureInt32(Int32 value = 0)
        {
            _offset = Random.Range(Int32.MinValue, Int32.MaxValue);
            _value = value + _offset;
        }

        #region Conversion operators overload

        public static implicit operator Int32(SecureInt32 secureValue)
        {
            return secureValue._value - secureValue._offset;
        }

        public static implicit operator SecureInt32(Int32 value)
        {
            return new SecureInt32(value);
        }

        #endregion

        #region Arithmetic operators overload

        public static SecureInt32 operator +(SecureInt32 valueA, SecureInt32 valueB)
        {
            return new SecureInt32((Int32)valueA + (Int32)valueB);
        }

        public static SecureInt32 operator -(SecureInt32 valueA, SecureInt32 valueB)
        {
            return new SecureInt32((Int32)valueA - (Int32)valueB);
        }

        public static SecureInt32 operator *(SecureInt32 valueA, SecureInt32 valueB)
        {
            return new SecureInt32((Int32)valueA * (Int32)valueB);
        }

        public static SecureInt32 operator /(SecureInt32 valueA, SecureInt32 valueB)
        {
            return new SecureInt32((Int32)valueA / (Int32)valueB);
        }

        public static SecureInt32 operator %(SecureInt32 valueA, SecureInt32 valueB)
        {
            return new SecureInt32((Int32)valueA % (Int32)valueB);
        }

        public static SecureInt32 operator ++(SecureInt32 value)
        {
            return new SecureInt32((Int32)value + 1);
        }

        public static SecureInt32 operator --(SecureInt32 value)
        {
            return new SecureInt32((Int32)value - 1);
        }

        #endregion

        #region Comparison operators overload

        public static Boolean operator <(SecureInt32 valueA, SecureInt32 valueB)
        {
            return (Int32)valueA < (Int32)valueB;
        }

        public static Boolean operator >(SecureInt32 valueA, SecureInt32 valueB)
        {
            return (Int32)valueA > (Int32)valueB;
        }

        public static Boolean operator <=(SecureInt32 valueA, SecureInt32 valueB)
        {
            return (Int32)valueA <= (Int32)valueB;
        }

        public static Boolean operator >=(SecureInt32 valueA, SecureInt32 valueB)
        {
            return (Int32)valueA >= (Int32)valueB;
        }

        public static Boolean operator ==(SecureInt32 valueA, SecureInt32 valueB)
        {
            return (Int32)valueA == (Int32)valueB;
        }

        public static Boolean operator !=(SecureInt32 valueA, SecureInt32 valueB)
        {
            return (Int32)valueA != (Int32)valueB;
        }

        public Boolean Equals(SecureInt32 value)
        {
            return value == this;
        }

        public override Boolean Equals(Object obj)
        {
            return obj is SecureInt32 && (SecureInt32)obj == this;
        }

        public override Int32 GetHashCode()
        {
            unchecked
            {
                return (_offset * 397) ^ _value;
            }
        }

        #endregion

        public override String ToString()
        {
            return ((Int32) this).ToString();
        }
    }
}