public class SystemErrorLog
{
    public int Id { get; set; }
    public string Component { get; set; }     // e.g., "CallController", "TwilioIntegration"
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public DateTime Timestamp { get; set; }
}
