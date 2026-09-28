using MaintenanceChronicle.Data;
using MaintenanceChronicle.Data.Entities.Account;
using MaintenanceChronicle.Infrastructure.DependencyInjection;
using MaintenanceChronicle.Utilities.Error;
using MaintenanceChronicle.Utilities.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MaintenanceChronicle.Api.Installers;

public class IdentityInstaller : IServiceInstaller
{
    public int Order => 3;
    public void Install(IServiceCollection services, IConfiguration configuration)
    {
        //Identity
        services.AddIdentity<User, Role>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;
        });

        //Configure authentication using JwtTokens
        var jwtSettings = configuration.GetRequiredSection(nameof(JwtOptions)).Get<JwtOptions>();
        if (jwtSettings == null)
        {
            throw new InternalServerException("Jwt settings was not found");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience
            };
        });
    }
}