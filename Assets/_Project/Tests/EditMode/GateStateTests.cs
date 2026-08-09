using System;
using NUnit.Framework;

public class GateStateTests
{
    [Test]
    public void NewGate_IsLocked()
    {
        var gate = new GateState();

        Assert.IsTrue(gate.Locked);
    }

    [Test]
    public void Unlock_ClearsLock()
    {
        var gate = new GateState();

        gate.Unlock();

        Assert.IsFalse(gate.Locked);
    }

    [Test]
    public void Unlock_SecondCall_IsIdempotent()
    {
        var gate = new GateState();
        int signals = 0;
        gate.UnlockRequested += () => signals++;

        gate.Unlock();
        gate.Unlock();

        Assert.IsFalse(gate.Locked, "A second unlock changes nothing");
        Assert.AreEqual(1, signals, "The unlock requested event fires once for the real unlock");
    }

    [Test]
    public void RequestUnlock_RaisesUnlockRequestedSignal()
    {
        var gate = new GateState();
        int signals = 0;
        gate.UnlockRequested += () => signals++;

        gate.RequestUnlock();

        Assert.AreEqual(1, signals, "A tap raises the unlock-intent signal");
        Assert.IsTrue(gate.Locked, "The signal alone does not unlock — the bridge unlocks on completion");
    }

    [Test]
    public void RequestUnlock_AfterUnlock_StillSignalsTap()
    {
        var gate = new GateState();
        int signals = 0;
        gate.UnlockRequested += () => signals++;
        gate.Unlock();

        gate.RequestUnlock();

        Assert.AreEqual(1, signals, "Every tap signals intent, even on an open barrier");
    }
}