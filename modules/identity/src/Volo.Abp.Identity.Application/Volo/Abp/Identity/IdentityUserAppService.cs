using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Security.Encryption;

namespace Volo.Abp.Identity
{
    public class IdentityUserAppService : IdentityAppServiceBase, IIdentityUserAppService
    {
        private readonly IdentityUserManager _userManager;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IStringEncryptionService _encryptionService;
        private readonly IdentityUserStore _identityUserStore;
        private readonly IUserEmailer _userEmailer;

        public IdentityUserAppService(
            IdentityUserManager userManager,
            IIdentityUserRepository userRepository, IUserEmailer userEmailer, IStringEncryptionService encryptionService, IdentityUserStore identityUserStore)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _userEmailer = userEmailer;
            _encryptionService = encryptionService;
            _identityUserStore = identityUserStore;
        }

        //TODO: [Authorize(IdentityPermissions.Users.Default)] should go the IdentityUserAppService class.
        [Authorize(IdentityPermissions.Users.Default)]
        public virtual async Task<IdentityUserDto> GetAsync(Guid id)
        {
            return ObjectMapper.Map<IdentityUser, IdentityUserDto>(
                await _userManager.GetByIdAsync(id)
            );
        }

        [Authorize(IdentityPermissions.Users.Default)]
        public virtual async Task<PagedResultDto<IdentityUserDto>> GetListAsync(GetIdentityUsersInput input)
        {
            var count = await _userRepository.GetCountAsync(input.Filter);
            var list = await _userRepository.GetListAsync(input.Sorting, input.MaxResultCount, input.SkipCount, input.Filter);

            return new PagedResultDto<IdentityUserDto>(
                count,
                ObjectMapper.Map<List<IdentityUser>, List<IdentityUserDto>>(list)
            );
        }

        [Authorize(IdentityPermissions.Users.Default)]
        public virtual async Task<ListResultDto<IdentityRoleDto>> GetRolesAsync(Guid id)
        {
            var roles = await _userRepository.GetRolesAsync(id);
            return new ListResultDto<IdentityRoleDto>(
                ObjectMapper.Map<List<IdentityRole>, List<IdentityRoleDto>>(roles)
            );
        }

        [Authorize(IdentityPermissions.Users.Create)]
        public virtual async Task<IdentityUserDto> CreateAsync(IdentityUserCreateDto input)
        {
            var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.Email, CurrentTenant.Id);

            (await _userManager.CreateAsync(user, input.Password)).CheckErrors();
            await UpdateUserByInput(user, input);

            await CurrentUnitOfWork.SaveChangesAsync();

            //Send activation email
            if (input.SendActivationEmail)
            {
                user.SetNewEmailConfirmationCode();
                await _userEmailer.SendEmailActivationLinkAsync(user, input.Password);
            }

            return ObjectMapper.Map<IdentityUser, IdentityUserDto>(user);
        }

        [Authorize(IdentityPermissions.Users.Update)]
        public virtual async Task<IdentityUserDto> UpdateAsync(Guid id, IdentityUserUpdateDto input)
        {
            var user = await _userManager.GetByIdAsync(id);
            user.ConcurrencyStamp = input.ConcurrencyStamp;

            (await _userManager.SetUserNameAsync(user, input.UserName)).CheckErrors();
            await UpdateUserByInput(user, input);
            (await _userManager.UpdateAsync(user)).CheckErrors();

            if (!input.Password.IsNullOrEmpty())
            {
                (await _userManager.RemovePasswordAsync(user)).CheckErrors();
                (await _userManager.AddPasswordAsync(user, input.Password)).CheckErrors();
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            return ObjectMapper.Map<IdentityUser, IdentityUserDto>(user);
        }

        [Authorize(IdentityPermissions.Users.Delete)]
        public virtual async Task DeleteAsync(Guid id)
        {
            if (CurrentUser.Id == id)
            {
                throw new BusinessException(code: IdentityErrorCodes.UserSelfDeletion);
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return;
            }

            (await _userManager.DeleteAsync(user)).CheckErrors();
        }

        [Authorize(IdentityPermissions.Users.Update)]
        public virtual async Task UpdateRolesAsync(Guid id, IdentityUserUpdateRolesDto input)
        {
            var user = await _userManager.GetByIdAsync(id);
            (await _userManager.SetRolesAsync(user, input.RoleNames)).CheckErrors();
            await _userRepository.UpdateAsync(user);
        }

        [Authorize(IdentityPermissions.Users.Default)]
        public virtual async Task<IdentityUserDto> FindByUsernameAsync(string username)
        {
            return ObjectMapper.Map<IdentityUser, IdentityUserDto>(
                await _userManager.FindByNameAsync(username)
            );
        }

        [Authorize(IdentityPermissions.Users.Default)]
        public virtual async Task<IdentityUserDto> FindByEmailAsync(string email)
        {
            return ObjectMapper.Map<IdentityUser, IdentityUserDto>(
                await _userManager.FindByEmailAsync(email)
            );
        }

        public async Task EmailConfirmation(EmailConfirmationInput model)
        {
            var userId = _encryptionService.Decrypt(model.UserId);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || user.EmailConfirmationCode.IsNullOrEmpty() ||user.EmailConfirmationCode != model.ConfirmationCode)
            {
                throw new UserFriendlyException("邮箱验证已失效", "请您务必通过点击邮件中的重置链接进入本页面.如果已经这样做了,还存在问题,请重试一遍邮箱验证操作!");
            }

            await _identityUserStore.SetEmailConfirmedAsync(user, true);
        }

        private async Task UpdateUserByInput(IdentityUser user, IdentityUserCreateOrUpdateDtoBase input)
        {
            if (!string.Equals(user.Email, input.Email, StringComparison.InvariantCultureIgnoreCase))
            {
                (await _userManager.SetEmailAsync(user, input.Email)).CheckErrors();
            }

            if (!string.Equals(user.PhoneNumber, input.PhoneNumber, StringComparison.InvariantCultureIgnoreCase))
            {
                (await _userManager.SetPhoneNumberAsync(user, input.PhoneNumber)).CheckErrors();
            }

            (await _userManager.SetTwoFactorEnabledAsync(user, input.TwoFactorEnabled)).CheckErrors();
            (await _userManager.SetLockoutEnabledAsync(user, input.LockoutEnabled)).CheckErrors();

            user.Name = input.Name;
            user.Surname = input.Surname;

            if (input.RoleNames != null)
            {
                (await _userManager.SetRolesAsync(user, input.RoleNames)).CheckErrors();
            }

        }
    }
}
