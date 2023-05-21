using System;
using System.Collections.Generic;

namespace Bitwarden.AutoType.Desktop.Helpers;

public class StateChangedEventArgs<T> : EventArgs where T : struct, Enum
{
    public T OldState { get; }
    public T NewState { get; }

    public StateChangedEventArgs(T oldState, T newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

public class StateController<T> where T : struct, Enum
{
    private T _state;

    public event EventHandler<StateChangedEventArgs<T>>? StateChanged;

    public T GetState()
    {
        return _state;
    }

    public void SetState(T value)
    {
        if (!EqualityComparer<T>.Default.Equals(_state, value))
        {
            T oldState = _state;
            _state = value;
            OnStateChanged(oldState, value);
        }
    }

    protected virtual void OnStateChanged(T oldState, T newState)
    {
        StateChanged?.Invoke(this, new StateChangedEventArgs<T>(oldState, newState));
    }
}