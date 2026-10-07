using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Domain.Security.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security.Tokens
{
    public class GeradorTokenJwt : IGeradorTokenJwt
    {
        private readonly string _chaveSecreta;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiracaoEmMinutos;

        public GeradorTokenJwt(IConfiguration configuration)
        {
            _chaveSecreta = configuration.GetValue<string>("Jwt:ChaveSecreta")!;
            _issuer = configuration.GetValue<string>("Jwt:Issuer")!;
            _audience = configuration.GetValue<string>("Jwt:Audience")!;
            _expiracaoEmMinutos = configuration.GetValue<int>("Jwt:ExpiracaoEmMinutos");
        }

        public string GerarToken(Clientes cliente)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
                new(ClaimTypes.Name, cliente.Nome),
                new(ClaimTypes.Email, cliente.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_chaveSecreta));
            var credenciais = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiracaoEmMinutos),
                signingCredentials: credenciais);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}