public class Call
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }
    public User user { get; set; }

    public DateTime CallStartTime { get; set; }
    public DateTime? CallEndTime { get; set; }

    public string Status { get; set; } // "Missed", "Connected", "Ended"
    public bool IsVoiceModulationUsed { get; set; }

    public string AgentName { get; set; } // Optional

    public string CallRecordingUrl { get; set; }
}
