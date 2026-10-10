using System.Linq.Expressions;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Users;

public static class UserMappings
{
    public static readonly Expression<Func<User, UserDto>> Projection =
        u => new UserDto(u.Id, u.Username, u.FullName, u.Role, u.IsActive, u.CreatedAt);

    private static readonly Func<User, UserDto> Compiled = Projection.Compile();

    public static UserDto ToDto(this User user) => Compiled(user);
}
