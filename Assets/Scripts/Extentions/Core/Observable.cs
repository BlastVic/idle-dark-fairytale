using System;
using Assets.Scripts.Extentions.SecureTypes;
using UnityEngine;

namespace Assets.Scripts.Extentions
{
    #region Observable

    public class Observable<T>
    {
        public event Action<T, T> Changing;
        public event Action<T> Changed;

        private T _value;

        public Observable()
        {
            _value = default(T);
        }

        public Observable(T value)
        {
            _value = value;
        }

        public T Value
        {
            get { return _value; }
            set
            {
                if (AreValuesEquals(_value, value))
                    return;

                Changing.SafeInvoke(_value, value);
                _value = value;
                Changed.SafeInvoke(_value);
            }
        }

        public static implicit operator T(Observable<T> observable)
        {
            return observable._value;
        }

        protected virtual Boolean AreValuesEquals(T a, T b)
        {
            return Equals(a, b);
        }
    }

    public sealed class ObservableSecureInt32 : Observable<SecureInt32>
    {
        public ObservableSecureInt32() : base() { }

        public ObservableSecureInt32(SecureInt32 value) : base(value) { }

        protected override Boolean AreValuesEquals(SecureInt32 a, SecureInt32 b)
        {
            return a == b;
        }

        public static implicit operator Int32(ObservableSecureInt32 observable)
        {
            return observable.Value;
        }
    }

    public sealed class ObservableSingle : Observable<Single>
    {
        public ObservableSingle() : base() { }

        public ObservableSingle(Single value) : base(value) { }

        protected override Boolean AreValuesEquals(Single a, Single b)
        {
            return a.IsEqual(b);
        }
    }

    public sealed class ObservableVector2 : Observable<Vector2>
    {
        public ObservableVector2() : base() { }
        public ObservableVector2(Vector2 value) : base(value) { }

        protected override Boolean AreValuesEquals(Vector2 a, Vector2 b)
        {
            return a == b;
        }
    }

    public sealed class ObservableVector3 : Observable<Vector3>
    {
        public ObservableVector3() : base() { }
        public ObservableVector3(Vector3 value) : base(value) { }

        protected override Boolean AreValuesEquals(Vector3 a, Vector3 b)
        {
            return a == b;
        }
    }

    public sealed class ObservableQuaternion : Observable<Quaternion>
    {
        public ObservableQuaternion() : base() { }
        public ObservableQuaternion(Quaternion value) : base(value) { }

        protected override Boolean AreValuesEquals(Quaternion a, Quaternion b)
        {
            return a == b;
        }
    }

    #endregion

    public class Observable<TOwner, TValue>
    {
        public event Action<TOwner, TValue, TValue> Changing;

        public event Action<TOwner, TValue> Changed;

        private readonly TOwner _owner;
        private TValue _value;

        public Observable(TOwner owner)
        {
            _owner = owner;
            _value = default(TValue);
        }

        public Observable(TOwner owner, TValue value)
        {
            _owner = owner;
            _value = value;
        }

        public TOwner Owner { get { return _owner; } }

        public TValue Value
        {
            get { return _value; }
            set
            {
                if (AreValuesEqual(_value, value))
                    return;

                Changing.SafeInvoke(_owner, _value, value);
                _value = value;
                Changed.SafeInvoke(_owner, _value);
            }
        }

        public static implicit operator TValue(Observable<TOwner, TValue> observable)
        {
            return observable._value;
        }

        protected virtual Boolean AreValuesEqual(TValue a, TValue b)
        {
            return Equals(a, b);
        }

        public void ChangeValueIgnoringCallback(TValue value, Action<TOwner, TValue> callback)
        {
            Changed -= callback;
            Value = value;
            Changed += callback;
        }
    }
}