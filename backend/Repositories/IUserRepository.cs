public interface IUserRepository
{
    Task<User> GetByIdAsync(int id);
    Task<User> GetByPhoneNumberAsync(string phoneNumber);
    Task<IEnumerable<User>> GetAllAsync();
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    void Add(User user); // Synchronous method to add a user
    Task SaveChangesAsync();
}
