using FluentValidation;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;
using Pos.Application.Common.Exceptions;
using Pos.Application.Common.Models;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Users;

public sealed class UserService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator) : IUserService
{
    public Task<PagedResult<UserDto>> ListAsync(UserQuery query, CancellationToken ct) =>
        users.ListAsync(query.Search, query, UserMappings.Projection, ct);

    public async Task<UserDto> GetAsync(int id, CancellationToken ct) =>
        (await GetEntityAsync(id, ct)).ToDto();

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        await createValidator.EnsureValidAsync(request, ct);

        var username = User.NormalizeUsername(request.Username);
        if (await users.UsernameExistsAsync(username, ct))
            throw new ConflictException($"Username '{username}' is already taken.");

        var user = new User(username, request.FullName, request.Role, passwordHasher.Hash(request.Password));
        users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct)
    {
        await updateValidator.EnsureValidAsync(request, ct);
        var user = await GetEntityAsync(id, ct);

        if (user.Id == currentUser.UserId && (!request.IsActive || request.Role != user.Role))
            throw new ConflictException("You cannot deactivate yourself or change your own role.");

        user.Update(request.FullName, request.Role, request.IsActive);
        if (!string.IsNullOrEmpty(request.Password))
            user.ChangePasswordHash(passwordHasher.Hash(request.Password));

        await unitOfWork.SaveChangesAsync(ct);
        return user.ToDto();
    }

    public async Task DeactivateAsync(int id, CancellationToken ct)
    {
        if (id == currentUser.UserId)
            throw new ConflictException("You cannot deactivate your own account.");

        var user = await GetEntityAsync(id, ct);
        user.Deactivate();
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<User> GetEntityAsync(int id, CancellationToken ct) =>
        await users.GetByIdAsync(id, ct) ?? throw NotFoundException.For("User", id);
}
