using System.Collections.Generic;
using UnityEngine;

public enum PatternTapResult
{
    Correct,
    Wrong,
    Complete
}

public class PatternLockRound
{
    public int ButtonCount { get; }
    public int SequenceLength { get; }
    public IReadOnlyList<int> Sequence => _sequence;
    public int Step { get; private set; }
    public bool IsComplete => Step >= _sequence.Count;

    private readonly List<int> _sequence = new List<int>();

    public PatternLockRound(int buttonCount, int sequenceLength)
    {
        ButtonCount = buttonCount;
        SequenceLength = sequenceLength;
    }

    public void Generate(System.Random rng)
    {
        _sequence.Clear();
        for (int i = 0; i < SequenceLength; i++)
            _sequence.Add(rng.Next(ButtonCount));
        Step = 0;
    }

    public PatternTapResult Submit(int buttonIndex)
    {
        if (IsComplete) return PatternTapResult.Complete;
        if (buttonIndex != _sequence[Step]) return PatternTapResult.Wrong;

        Step++;
        return Step >= _sequence.Count ? PatternTapResult.Complete : PatternTapResult.Correct;
    }
}