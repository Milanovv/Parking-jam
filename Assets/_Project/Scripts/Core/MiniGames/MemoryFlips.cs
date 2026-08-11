using System.Collections.Generic;
using UnityEngine;

public enum CardState
{
    FaceDown,
    FaceUp,
    Matched
}

public enum MemoryFlipResult
{
    Revealed,
    Matched,
    Mismatched,
    IgnoredDuplicate,
    IgnoredLocked,
    Lost
}

public class MemoryFlips
{
    public int Width { get; }
    public int Height { get; }
    public int PairCount { get; }
    public int? MoveLimit { get; }
    public IReadOnlyList<int> Layout => _layout;
    public int MovesUsed { get; private set; }
    public bool InputLocked { get; private set; }
    public int PendingCard => _pendingCard;
    public bool IsWon => _matchedCount >= PairCount;

    private readonly int[] _layout;
    private readonly CardState[] _states;
    private int _matchedCount;
    private int _pendingCard = -1;
    private int _secondCard = -1;

    public MemoryFlips(int pairCount, int width, int height, int? moveLimit, System.Random rng)
    {
        PairCount = pairCount;
        MoveLimit = moveLimit;
        Width = width;
        Height = height;

        int cardCount = width * height;
        _layout = new int[cardCount];
        var cards = new List<int>();
        for (int pair = 0; pair < pairCount; pair++)
        {
            cards.Add(pair);
            cards.Add(pair);
        }
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
        for (int i = 0; i < cards.Count; i++)
            _layout[i] = cards[i];

        _states = new CardState[cards.Count];
        for (int i = 0; i < _states.Length; i++)
            _states[i] = CardState.FaceDown;
    }

    public CardState StateOf(int index) => _states[index];
    public int PairOf(int index) => _layout[index];

    public MemoryFlipResult Flip(int index)
    {
        if (InputLocked) return MemoryFlipResult.IgnoredLocked;
        if (index < 0 || index >= _layout.Length) return MemoryFlipResult.IgnoredLocked;
        if (_states[index] == CardState.Matched) return MemoryFlipResult.IgnoredLocked;

        if (_pendingCard >= 0)
        {
            if (index == _pendingCard) return MemoryFlipResult.IgnoredDuplicate;
            if (_states[_pendingCard] == CardState.Matched || _states[index] == CardState.FaceUp) return MemoryFlipResult.IgnoredLocked;

            _states[index] = CardState.FaceUp;
            _secondCard = index;
            MovesUsed++;

            if (_layout[index] == _layout[_pendingCard])
            {
                _states[_pendingCard] = CardState.Matched;
                _states[index] = CardState.Matched;
                _matchedCount += 2;
                _pendingCard = -1;
                _secondCard = -1;
                return MemoryFlipResult.Matched;
            }

            if (MoveLimit.HasValue && MovesUsed >= MoveLimit.Value)
            {
                _states[_pendingCard] = CardState.FaceDown;
                _states[index] = CardState.FaceDown;
                _pendingCard = -1;
                _secondCard = -1;
                return MemoryFlipResult.Lost;
            }

            InputLocked = true;
            return MemoryFlipResult.Mismatched;
        }

        _states[index] = CardState.FaceUp;
        _pendingCard = index;
        return MemoryFlipResult.Revealed;
    }

    public void ResolveMismatch()
    {
        if (_pendingCard < 0) return;
        _states[_pendingCard] = CardState.FaceDown;
        _states[_secondCard] = CardState.FaceDown;
        _pendingCard = -1;
        _secondCard = -1;
        InputLocked = false;
    }
}