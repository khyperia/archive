namespace StellarStack
{
    interface IImage
    {
        float this[float x, float y] { get; }
    }

    interface IIntImage
    {
        float this[int x, int y] { get; }
        int Width { get; }
        int Height { get; }
    }
}