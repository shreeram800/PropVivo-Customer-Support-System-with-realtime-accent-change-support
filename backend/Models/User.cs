public class User
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Location { get; set; }
    public DateTime CreatedAt { get; set; }

    public string Address { get; set; } // Optional

    public ICollection<Call>? Calls { get; set; }
}
