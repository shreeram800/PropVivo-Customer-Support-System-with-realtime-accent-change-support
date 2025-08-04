namespace RealtimeAccentTransformer.Interfaces
{
    public interface IPiperTtsService
    {
        Task<byte[]?> SynthesizeAsync(string text);
    }
}