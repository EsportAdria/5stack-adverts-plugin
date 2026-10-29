namespace SwiftlyS2.Adverts.Logic;

/// <summary>
/// Picks the next message index of one group: in order with wrap-around, or
/// at random without showing the same message twice in a row.
/// </summary>
public class AdvertRotation
{
    private readonly bool _random;
    private readonly int _count;
    private readonly Random _rng;
    private int _next;
    private int _last = -1;

    public AdvertRotation(string order, int count, Random rng)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        _random = order == "random";
        _count = count;
        _rng = rng;
    }

    /// <summary>Index the next sequential tick will use.</summary>
    public int Peek => _next;

    public int Next()
    {
        int index;
        if (_random)
        {
            if (_count == 1)
            {
                index = 0;
            }
            else
            {
                // draw from the other count - 1 messages
                index = _rng.Next(_count - 1);
                if (_last >= 0 && index >= _last)
                    index++;
            }
        }
        else
        {
            index = _next;
            _next = (_next + 1) % _count;
        }

        _last = index;
        return index;
    }
}
