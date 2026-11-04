using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Zuhid.Auth.User;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class UserController(UserRepository userRepository, UserMapper userMapper) : ControllerBase
{
    [HttpGet]
    public async Task<List<UserModel>> Get()
    {
        return await userRepository.Get();
    }

    [HttpPost]
    public async Task Add([FromBody] UserModel request)
    {
        var (_, errors) = await userRepository.Add(userMapper.Map(request));
        errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
    }

    [HttpPut("{id:guid}")]
    public async Task Update(Guid id, [FromBody] UserModel request)
    {
        var errors = await userRepository.Update(userMapper.Map(request, id));
        errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
    }

    [HttpPost("{id:guid}/Activate")]
    public async Task Activate(Guid id)
    {
        var errors = await userRepository.SetActive(id, true);
        errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
    }

    [HttpPost("{id:guid}/Deactivate")]
    public async Task Deactivate(Guid id)
    {
        var errors = await userRepository.SetActive(id, false);
        errors?.ForEach(e => ModelState.AddModelError(e.Key, e.Value));
    }
}

