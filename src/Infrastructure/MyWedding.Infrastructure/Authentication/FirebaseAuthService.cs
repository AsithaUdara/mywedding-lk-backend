// File: src/Infrastructure/MyWedding.Infrastructure/Authentication/FirebaseAuthService.cs
using FirebaseAdmin.Auth;
using MyWedding.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyWedding.Infrastructure.Authentication
{
    public class FirebaseAuthService : IFirebaseAuthService
    {
        public async Task SetUserRoleAsync(string userId, string role)
        {
            var claims = new Dictionary<string, object>
            {
                { "role", role }
            };

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(userId, claims);
        }
    }
}
