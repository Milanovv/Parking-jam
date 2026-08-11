using NUnit.Framework;

public class PatternLockRoundTests
{
    [Test]
    public void Generate_ProducesSequenceOfRequestedLength()
    {
        var round = new PatternLockRound(4, 4);
        round.Generate(new System.Random(1));

        Assert.AreEqual(4, round.Sequence.Count);
        Assert.AreEqual(0, round.Step);
        Assert.IsFalse(round.IsComplete);
    }

    [Test]
    public void Submit_AllCorrectTaps_CompletesTheRound()
    {
        var round = new PatternLockRound(4, 3);
        round.Generate(new System.Random(2));

        Assert.AreEqual(PatternTapResult.Correct, round.Submit(round.Sequence[0]));
        Assert.AreEqual(PatternTapResult.Correct, round.Submit(round.Sequence[1]));
        Assert.AreEqual(PatternTapResult.Complete, round.Submit(round.Sequence[2]));
        Assert.IsTrue(round.IsComplete);
        Assert.AreEqual(3, round.Step);
    }

    [Test]
    public void Submit_WrongButton_FailsWithoutAdvancing()
    {
        var round = new PatternLockRound(4, 3);
        round.Generate(new System.Random(3));

        int wrong = (round.Sequence[0] + 1) % 4;
        Assert.AreEqual(PatternTapResult.Wrong, round.Submit(wrong));
        Assert.AreEqual(0, round.Step);
        Assert.IsFalse(round.IsComplete);
        Assert.AreEqual(PatternTapResult.Correct, round.Submit(round.Sequence[0]));
    }

    [Test]
    public void Regenerate_ReplacesTheSequence()
    {
        var round = new PatternLockRound(4, 4);
        round.Generate(new System.Random(4));
        var first = new int[round.Sequence.Count];
        for (int i = 0; i < round.Sequence.Count; i++)
            first[i] = round.Sequence[i];

        round.Generate(new System.Random(5));
        int differences = 0;
        for (int i = 0; i < first.Length; i++)
        {
            if (first[i] != round.Sequence[i]) differences++;
        }

        Assert.Greater(differences, 0, "A fresh sequence should differ from the previous one");
        Assert.AreEqual(0, round.Step);
    }

    [Test]
    public void Submit_AfterCompletion_StaysComplete()
    {
        var round = new PatternLockRound(2, 2);
        round.Generate(new System.Random(6));

        round.Submit(round.Sequence[0]);
        round.Submit(round.Sequence[1]);

        Assert.AreEqual(PatternTapResult.Complete, round.Submit(round.Sequence[0]));
    }
}