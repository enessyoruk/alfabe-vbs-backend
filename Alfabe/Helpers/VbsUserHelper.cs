using Alfabe.Data;
using Alfabe.Models;
using Microsoft.EntityFrameworkCore;

namespace Alfabe.Helpers
{
    public static class VbsUserHelper
    {
        public static async Task CreateOrApproveVbsUser(
            AlfabeContext db,
            string fullName,
            string phone,
            string email,
            string userType,
            string passwordHash)
        {
            email = (email ?? "").Trim().ToLower();
            phone = (phone ?? "").Trim();

            var existing = await db.VbsUsers
                .FirstOrDefaultAsync(x => x.Email == email);

            if (existing != null)
            {
                existing.FullName = fullName;
                existing.PhoneNumber = phone;
                existing.UserType = userType;
                existing.PasswordHash = passwordHash;

                existing.IsApproved = true;
                existing.ApprovedAt = DateTime.UtcNow;

                db.VbsUsers.Update(existing);
                await db.SaveChangesAsync();
                return;
            }

            var now = DateTime.UtcNow;

            var v = new VbsUser
            {
                FullName = fullName,
                PhoneNumber = phone,
                Email = email,
                PasswordHash = passwordHash,
                UserType = userType,
                IsApproved = true,
                CreatedAt = now,
                ApprovedAt = now
            };

            db.VbsUsers.Add(v);
            await db.SaveChangesAsync();
        }
    }
}
