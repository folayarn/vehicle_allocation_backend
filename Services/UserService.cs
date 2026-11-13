using Vehicle_Information_System.Models;
using Microsoft.EntityFrameworkCore;
namespace Vehicle_Information_System.Services
{
    public class UserService
    {
        private readonly ApplicationDbContext _context;
        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> getLoginUser(Guid? officerId)
        {
            var user = await _context.Users.FindAsync(officerId);

            return user;

        } 

      

        public async Task<User?> GetUserByRefreshToken(string refreshToken)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        }

        public async Task ActivityLog(string action,  Guid UserId, Guid? TargetId, string TargetType  )
        {

            var user = _context.Users.Find(UserId);

            var activity = new ActivityLog
            {
                Action = action,
                UserId = UserId,
                FullName = $"{user.Rank } {user.Svn} {user.Fullname}".ToUpper(),
                TargetId = TargetId,
                TargetType = TargetType
            };

            _context.ActivityLogs.Add(activity);

            _context.SaveChanges();

        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

       
    }
}
