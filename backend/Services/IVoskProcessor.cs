namespace RealtimeAccentTransformer.Interfaces
{
    public interface IVoskProcessor
    {
        Task<string> TranscribeAsync(Stream pcmAudioStream);
        Stream ConvertUlawToPcm(Stream ulawAudioStream);
    }
}