using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Pos.Infrastructure.Security;

public static class JwtSigningKey
{
    public static SymmetricSecurityKey Create(string key) => new(Encoding.UTF8.GetBytes(key));
}
