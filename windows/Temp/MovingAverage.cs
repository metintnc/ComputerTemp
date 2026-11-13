namespace Temp;

public sealed class MovingAverage
{
    private readonly Queue<float> values = new();
    private readonly int windowSize;
    private float sum;

    public MovingAverage(int windowSize = 5)
    {
        if (windowSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windowSize),
                "Pencere boyutu sıfırdan büyük olmalıdır.");
        }

        this.windowSize = windowSize;
    }

    public int Count => values.Count;

    public float Add(float value)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "Hareketli ortalamaya sonlu bir değer eklenmelidir.");
        }

        values.Enqueue(value);
        sum += value;

        if (values.Count > windowSize)
        {
            sum -= values.Dequeue();
        }

        return sum / values.Count;
    }

    public void Clear()
    {
        values.Clear();
        sum = 0;
    }
}
