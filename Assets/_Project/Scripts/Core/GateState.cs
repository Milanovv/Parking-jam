using System;

public class GateState
{
    public bool Locked { get; private set; } = true;

    public event Action UnlockRequested;

    public void RequestUnlock()
    {
        if (Locked) UnlockRequested?.Invoke();
    }

    public void Unlock()
    {
        if (!Locked) return;
        Locked = false;
        UnlockRequested?.Invoke();
    }
}