using SwiftlyS2.Adverts.Logic;
using Xunit;

public class AdvertRotationTests
{
    [Fact]
    public void SequentialWrapsAround()
    {
        var rotation = new AdvertRotation("sequential", 3, new Random(1));

        var picks = Enumerable.Range(0, 7).Select(_ => rotation.Next()).ToArray();

        Assert.Equal(new[] { 0, 1, 2, 0, 1, 2, 0 }, picks);
        Assert.Equal(1, rotation.Peek);
    }

    [Fact]
    public void RandomNeverRepeatsTheLastMessage()
    {
        var rotation = new AdvertRotation("random", 3, new Random(42));
        var seen = new HashSet<int>();
        var last = -1;

        for (var i = 0; i < 1000; i++)
        {
            var index = rotation.Next();
            Assert.InRange(index, 0, 2);
            Assert.NotEqual(last, index);
            seen.Add(index);
            last = index;
        }

        Assert.Equal(3, seen.Count);
    }

    [Fact]
    public void RandomWithOneMessageAlwaysPicksIt()
    {
        var rotation = new AdvertRotation("random", 1, new Random(1));

        Assert.All(Enumerable.Range(0, 5), _ => Assert.Equal(0, rotation.Next()));
    }

    [Fact]
    public void RejectsAnEmptyGroup()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AdvertRotation("sequential", 0, new Random()));
    }
}
