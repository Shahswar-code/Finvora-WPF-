using Finvora.Data;
using Finvora.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Finvora.Services
{
    /// <summary>
    /// Manages the PIN that protects the "Reset All Data" action. PINs are never
    /// stored in plain text -- only a PBKDF2 hash + a random salt per PIN, so the
    /// real PIN can't be recovered even with direct database access.
    /// </summary>
    public class SecurityService
    {
        private const int Iterations = 100_000;
        private const int HashLength = 32;

        public async Task<bool> HasPinAsync()
        {
            using var db = new FinvoraDbContext();
            return await db.AppSecurity.AnyAsync();
        }

        public async Task SetPinAsync(string pin)
        {
            using var db = new FinvoraDbContext();
            var existing = await db.AppSecurity.FirstOrDefaultAsync();

            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(pin, saltBytes, Iterations, HashAlgorithmName.SHA256, HashLength);

            var hash = System.Convert.ToBase64String(hashBytes);
            var salt = System.Convert.ToBase64String(saltBytes);

            if (existing is null)
            {
                db.AppSecurity.Add(new AppSecurity { PinHash = hash, PinSalt = salt, UpdatedAt = System.DateTime.Now });
            }
            else
            {
                existing.PinHash = hash;
                existing.PinSalt = salt;
                existing.UpdatedAt = System.DateTime.Now;
            }

            await db.SaveChangesAsync();
        }

        public async Task<bool> VerifyPinAsync(string pin)
        {
            using var db = new FinvoraDbContext();
            var existing = await db.AppSecurity.FirstOrDefaultAsync();
            if (existing is null) return false;

            var saltBytes = System.Convert.FromBase64String(existing.PinSalt);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(pin, saltBytes, Iterations, HashAlgorithmName.SHA256, HashLength);
            var storedHashBytes = System.Convert.FromBase64String(existing.PinHash);

            return CryptographicOperations.FixedTimeEquals(hashBytes, storedHashBytes);
        }
    }
}  