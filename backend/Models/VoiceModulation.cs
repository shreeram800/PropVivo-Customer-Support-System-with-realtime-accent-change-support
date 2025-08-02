using System;
using System.ComponentModel.DataAnnotations;

public class VoiceModulation
{
    [Key]
    public int Id { get; set; }

    // Foreign Key to Call session
    [Required]
    public int CallId { get; set; }
    public virtual Call Call { get; set; }

    // Original and Target Accents
    [Required]
    public string FromAccent { get; set; } = "Indian";   // default
    [Required]
    public string ToAccent { get; set; } = "American";   // default

    // Original transcript (as spoken)
    [Required]
    public string Transcript { get; set; }

    // Path or blob for synthesized audio (can be optional if streamed)
    public string? SynthesizedAudioPath { get; set; }

    // Real-time or Post-Processed tag
    public bool IsRealTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
