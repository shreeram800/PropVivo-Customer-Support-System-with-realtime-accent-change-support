public class VoiceModulation
{
     public int Id { get; set; }

    public int CallId { get; set; }
    public Call Call { get; set; }

    public string FromAccent { get; set; }  // "Indian"
    public string ToAccent { get; set; }    // "American"
    public DateTime Timestamp { get; set; }     // Real-time or post-processed
}
