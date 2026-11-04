using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface IUserRoleService
{
    ServiceResult<IReadOnlyList<UserDto>> GetAll();
    ServiceResult ChangeRole(string userId, ChangeUserRoleRequest request);
}
