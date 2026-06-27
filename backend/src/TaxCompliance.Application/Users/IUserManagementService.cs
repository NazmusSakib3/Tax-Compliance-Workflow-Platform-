using TaxCompliance.Application.Common;
using TaxCompliance.Application.Users;

namespace TaxCompliance.Application.Users;

public interface IUserManagementService
{
    Task<PagedResult<UserListItemDto>> GetPagedAsync(PagedListQuery query, CancellationToken cancellationToken);
    Task<UserListItemDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserListItemDto> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken);
}
