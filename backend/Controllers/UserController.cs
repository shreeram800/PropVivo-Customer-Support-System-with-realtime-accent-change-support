using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _repo;

    public UsersController(IUserRepository repo)
    {
        _repo = repo;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] User user)
    {
        await _repo.AddAsync(user);
        await _repo.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet("phone/{number}")]
    public async Task<IActionResult> GetByPhoneNumber(string number)
    {
        var user = await _repo.GetByPhoneNumberAsync(number);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _repo.GetAllAsync();
        return Ok(users);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
    {
        var existingUser = await _repo.GetByIdAsync(id);
        if (existingUser == null) return NotFound();

        existingUser.FullName = updatedUser.FullName;
        existingUser.PhoneNumber = updatedUser.PhoneNumber;
        existingUser.Email = updatedUser.Email;
        existingUser.Address = updatedUser.Address;

        await _repo.UpdateAsync(existingUser);
        await _repo.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _repo.GetByIdAsync(id);
        if (user == null) return NotFound();

        await _repo.DeleteAsync(user);
        await _repo.SaveChangesAsync();
        return NoContent();
    }
}
