using System.Collections.Generic;
using NUnit.Framework;

public class MemoryFlipsTests
{
    private static MemoryFlips NewGame(int pairs, int width, int height, int? limit, int seed)
    {
        return new MemoryFlips(pairs, width, height, limit, new System.Random(seed));
    }

    [Test]
    public void Layout_ContainsEveryPairExactlyTwice()
    {
        var game = NewGame(3, 3, 2, null, 1);
        var counts = new Dictionary<int, int>();
        foreach (int pair in game.Layout)
        {
            counts.TryGetValue(pair, out int count);
            counts[pair] = count + 1;
        }

        Assert.AreEqual(3, counts.Count);
        foreach (var pair in counts)
            Assert.AreEqual(2, pair.Value);
    }

    [Test]
    public void Flip_FirstCard_RevealsPendingCard()
    {
        var game = NewGame(3, 3, 2, null, 1);

        Assert.AreEqual(MemoryFlipResult.Revealed, game.Flip(0));
        Assert.AreEqual(CardState.FaceUp, game.StateOf(0));
        Assert.AreEqual(0, game.PendingCard);
        Assert.AreEqual(0, game.MovesUsed);
    }

    [Test]
    public void Flip_SameCardTwice_IsIgnored()
    {
        var game = NewGame(3, 3, 2, null, 1);
        game.Flip(0);

        Assert.AreEqual(MemoryFlipResult.IgnoredDuplicate, game.Flip(0));
        Assert.AreEqual(0, game.MovesUsed, "A duplicate tap is not a match attempt");
    }

    [Test]
    public void Flip_MatchingPair_MatchesBothCards()
    {
        var game = NewGame(3, 3, 2, null, 1);
        int pair = game.PairOf(0);
        int twin = -1;
        for (int i = 1; i < game.Layout.Count; i++)
        {
            if (game.PairOf(i) == pair)
            {
                twin = i;
                break;
            }
        }

        game.Flip(0);
        Assert.AreEqual(MemoryFlipResult.Matched, game.Flip(twin));
        Assert.AreEqual(CardState.Matched, game.StateOf(0));
        Assert.AreEqual(CardState.Matched, game.StateOf(twin));
        Assert.AreEqual(1, game.MovesUsed);
        Assert.AreEqual(-1, game.PendingCard);
    }

    [Test]
    public void Flip_NonMatchingPair_LocksInputUntilResolved()
    {
        var game = NewGame(3, 3, 2, null, 1);
        int a = 0;
        int b = -1;
        for (int i = 1; i < game.Layout.Count; i++)
        {
            if (game.PairOf(i) != game.PairOf(a))
            {
                b = i;
                break;
            }
        }

        game.Flip(a);
        Assert.AreEqual(MemoryFlipResult.Mismatched, game.Flip(b));
        Assert.IsTrue(game.InputLocked);
        Assert.AreEqual(MemoryFlipResult.IgnoredLocked, game.Flip(2));

        game.ResolveMismatch();
        Assert.IsFalse(game.InputLocked);
        Assert.AreEqual(CardState.FaceDown, game.StateOf(a));
        Assert.AreEqual(CardState.FaceDown, game.StateOf(b));
        Assert.AreEqual(1, game.MovesUsed);
    }

    [Test]
    public void MoveLimit_ExhaustedOnNonMatchingPair_Loses()
    {
        var game = NewGame(3, 3, 2, 2, 2);

        PlayMismatch(game, out _);
        Assert.AreEqual(1, game.MovesUsed);
        Assert.IsTrue(game.InputLocked);
        game.ResolveMismatch();

        Assert.AreEqual(MemoryFlipResult.Lost, SecondMismatch(game));
        Assert.AreEqual(2, game.MovesUsed);
    }

    private static void PlayMismatch(MemoryFlips game, out int secondCard)
    {
        int a = 0;
        int b = -1;
        for (int i = 1; i < game.Layout.Count; i++)
        {
            if (game.PairOf(i) != game.PairOf(a))
            {
                b = i;
                break;
            }
        }

        game.Flip(a);
        secondCard = b;
        game.Flip(b);
    }

    private static MemoryFlipResult SecondMismatch(MemoryFlips game)
    {
        game.Flip(0);
        int b = -1;
        for (int i = 1; i < game.Layout.Count; i++)
        {
            if (game.PairOf(i) != game.PairOf(0) && game.StateOf(i) == CardState.FaceDown)
            {
                b = i;
                break;
            }
        }
        return game.Flip(b);
    }

    [Test]
    public void MoveLimit_MatchedOnLastAllowedMove_Wins()
    {
        var game = NewGame(1, 1, 2, 1, 3);

        game.Flip(0);
        Assert.AreEqual(MemoryFlipResult.Matched, game.Flip(1));
        Assert.AreEqual(1, game.MovesUsed);
        Assert.IsTrue(game.IsWon);
    }

    [Test]
    public void MatchingAllPairs_WinsTheGame()
    {
        var game = NewGame(2, 2, 2, null, 4);
        var flipped = new HashSet<int>();

        while (!game.IsWon)
        {
            int pick = -1;
            for (int i = 0; i < game.Layout.Count; i++)
            {
                if (game.StateOf(i) == CardState.FaceDown && !flipped.Contains(i))
                {
                    flipped.Add(i);
                    game.Flip(i);
                    pick = i;
                    break;
                }
            }
            if (pick < 0) break;

            int twin = -1;
            for (int i = 0; i < game.Layout.Count; i++)
            {
                if (i != pick && game.PairOf(i) == game.PairOf(pick))
                {
                    twin = i;
                    break;
                }
            }
            if (twin < 0) break;
            game.Flip(twin);
        }

        Assert.IsTrue(game.IsWon);
    }

    [Test]
    public void Flip_MatchedCard_IsIgnoredData()
    {
        var game = NewGame(2, 2, 2, null, 5);
        int pair = game.PairOf(0);
        int twin = -1;
        for (int i = 1; i < game.Layout.Count; i++)
        {
            if (game.PairOf(i) == pair)
            {
                twin = i;
                break;
            }
        }

        game.Flip(0);
        game.Flip(twin);

        Assert.AreEqual(MemoryFlipResult.IgnoredLocked, game.Flip(0));
        Assert.AreEqual(CardState.Matched, game.StateOf(0));
    }
}