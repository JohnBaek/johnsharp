using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace JohnIsDev.Core.Features.Extensions;

/// <summary>
/// JwtAuthenticationExtension
/// </summary>
public static class JwtAuthenticationExtension
{
    /// <summary>
    /// AddCustomJwtAuthentication
    /// </summary>
    /// <param name="services">services</param>
    /// <param name="configuration">configuration</param>
    public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = false;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = configuration["jwt:issuer"] ,
                    ClockSkew = TimeSpan.Zero ,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true ,
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["jwt:secret"] ?? "")) ,
                };
                options.Events = new JwtBearerEvents
                {
                    // For WebSocket
                    OnMessageReceived = context =>
                    {
                        StringValues accessToken = context.Request.Query["access_token"];
                        PathString path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hub/v1")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    
                    // Does not have Authentication 
                    OnChallenge = async context =>
                    {
                        // Suppress Default Response 
                        context.HandleResponse();
                
                        // Manipulate Response Code 
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        Response result = new Response(EnumResponseResult.Error,
                            "", "CommonDoesNotHaveAuthentication");
                
                        // JsonSerializerOptions 설정
                        var jsonSerializerOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        };
                        
                        await context.Response.WriteAsync(JsonSerializer.Serialize(result,jsonSerializerOptions));
                    },
                    // Does not have role or claims
                    OnForbidden = async context =>
                    {
                        // Manipulate Response Code 
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        context.Response.ContentType = MediaTypeNames.Application.Json;
                        Response result = new Response(EnumResponseResult.Error,
                            "", "CommonAcceptNotAllowed");
                    
                        // JsonSerializerOptions 설정
                        var serializerOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = true
                        };

                        // 결과를 직렬화할 때 옵션 사용
                        await context.Response.WriteAsync(JsonSerializer.Serialize(result, serializerOptions));
                    } ,
                    // // Authentication is Success
                    // OnTokenValidated = async context => 
                    // {
                    //     // Get Token 
                    //     string jwtEncoded = context.HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                    //     JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();
                    //     JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(jwtEncoded);
                    //     
                    //     // Try read
                    //     string? userId = jwtToken.Claims.FirstOrDefault(i => i.Type == ClaimTypes.Sid)?.Value ?? "";
                    //     string? loginId = jwtToken.Claims.FirstOrDefault(i => i.Type == "unique_name")?.Value ?? "";
                    //     List<Claim> claims = jwtToken.Claims.Where(i => i.Type == "role").ToList();
                    // }
                };
            });
    }
}