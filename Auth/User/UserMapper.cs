using Zuhid.Auth.Entities;

namespace Zuhid.Auth.User;

public class UserMapper
{
    public UserEntity Map(UserModel request, Guid id = default) => new()
    {
        Id = id,
        FirstName = request.FirstName,
        LastName = request.LastName,
        UserName = request.Email,
        Email = request.Email,
        PhoneNumber = request.PhoneNumber
    };
}
