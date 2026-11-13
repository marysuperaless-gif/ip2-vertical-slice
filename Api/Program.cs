using Api.Common.Authorization;
using Api.Features.Admissions;
using Api.Features.Auth;
using Api.Features.Courses;
using Api.Features.Products;
using Api.Features.Students;
using Application.Abstractions;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Admissions.CreateAdmission;
using Application.Features.Admissions.GetAdmissionById;
using Application.Features.Admissions.GetAllAdmissions;
using Application.Features.Auth.Login;
using Application.Features.Auth.Register;
using Application.Features.Courses.CreateCourse;
using Application.Features.Courses.DeleteCourse;
using Application.Features.Courses.GetAllCourses;
using Application.Features.Courses.GetCourseById;
using Application.Features.Courses.UpdateCourse;
using Application.Features.Products.CreateProduct;
using Application.Features.Products.DeleteProduct;
using Application.Features.Products.GetProductById;
using Application.Features.Products.UpdateProduct;
using Application.Features.Students.CreateStudent;
using Application.Features.Students.GetAllStudents;
using Application.Features.Students.GetStudentById;
using Domain.Common;
using Domain.Entities;
using FluentValidation;
using Infrastructure.Authentication;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Database
        builder.Services.AddDbContext<IAppDbContext, AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("Database")));

        // Validators
        builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentValidator>();

        // Handlers
        builder.Services.AddScoped<ICommandHandler<CreateStudentCommand, Result<CreateStudentResponse>>, CreateStudentCommandHandler>();
        builder.Services.AddScoped<IQueryHandler<GetAllStudentsQuery, Result<GetAllStudentsResponse>>, GetAllStudentsQueryHandler>();
        builder.Services.AddScoped<IQueryHandler<GetStudentByIdQuery, Result<GetStudentByIdResponse>>, GetStudentByIdQueryHandler>();
        builder.Services.AddScoped<ICommandHandler<CreateCourseCommand, Result<CreateCourseResponse>>, CreateCourseCommandHandler>();
        builder.Services.AddScoped<IQueryHandler<GetAllCoursesQuery, Result<GetAllCoursesResponse>>, GetAllCoursesQueryHandler>();
        builder.Services.AddScoped<IQueryHandler<GetCourseByIdQuery, Result<GetCourseByIdResponse>>, GetCourseByIdQueryHandler>();
        builder.Services.AddScoped<ICommandHandler<CreateAdmissionCommand, Result<CreateAdmissionResponse>>, CreateAdmissionCommandHandler>();
        builder.Services.AddScoped<IQueryHandler<GetAdmissionByIdQuery, Result<GetAdmissionByIdResponse>>, GetAdmissionByIdQueryHandler>();
        builder.Services.AddScoped<IQueryHandler<GetAllAdmissionsQuery, Result<GetAllAdmissionsResponse>>, GetAllAdmissionsQueryHandler>();
        builder.Services.AddScoped<ICommandHandler<UpdateCourseCommand, Result<UpdateCourseResponse>>, UpdateCourseCommandHandler>();
        builder.Services.AddScoped<ICommandHandler<DeleteCourseCommand, Result>, DeleteCourseCommandHandler>();
        builder.Services.AddScoped<ICommandHandler<CreateProductCommand, Result<CreateProductResponse>>, CreateProductHandler>();
        builder.Services.AddScoped<IQueryHandler<GetProductByIdQuery, Result<GetProductByIdResponse>>, GetProductByIdHandler>();
        builder.Services.AddScoped<IQueryHandler<GetAllProductsQuery, Result<List<GetProductByIdResponse>>>, GetAllProductsHandler>();
        builder.Services.AddScoped<ICommandHandler<UpdateProductCommand, Result<GetProductByIdResponse>>, UpdateProductHandler>();
        builder.Services.AddScoped<ICommandHandler<DeleteProductCommand, Result<Guid>>, DeleteProductHandler>();

        // Ownership check (BOLA fix)
        builder.Services.AddSingleton<IAuthorizationHandler, AdmissionOwnerHandler>();

        // Auth services
        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
        builder.Services.AddScoped<IPasswordHasher, Infrastructure.Authentication.PasswordHasher>();
        builder.Services.AddScoped<ITokenService, JwtTokenService>();
        builder.Services.AddScoped<ICommandHandler<RegisterCommand, Result<RegisterResponse>>, RegisterCommandHandler>();
        builder.Services.AddScoped<ICommandHandler<LoginCommand, Result<LoginResponse>>, LoginCommandHandler>();

        // Cleanup background service
        builder.Services.AddHostedService<Infrastructure.BackgroundJobs.RevokedTokenCleanupService>();

        // JWT options
        builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey),
                "Jwt:SigningKey is required.")
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey ?? "") >= 32,
                "Jwt:SigningKey must be at least 32 characters.")
            .ValidateOnStart();

        // JWT validation
        var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["SigningKey"]!)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var dbContext = context.HttpContext.RequestServices
                            .GetRequiredService<IAppDbContext>();

                        var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                        var sub = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                                  ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var iatRaw = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Iat);

                        // 1. Na-revoke ba ang token na ito? (logout)
                        if (jti is not null &&
                            await dbContext.RevokedTokens.AnyAsync(r => r.Jti == jti))
                        {
                            context.Fail("Token has been revoked.");
                            return;
                        }

                        // 2. Security stamp (logout-all)
                        if (sub is not null && Guid.TryParse(sub, out var userId)
                            && iatRaw is not null && long.TryParse(iatRaw, out var iatUnix))
                        {
                            var issuedAtUtc = DateTimeOffset.FromUnixTimeSeconds(iatUnix).UtcDateTime;
                            var user = await dbContext.Users.FindAsync([userId]);

                            if (user?.TokensValidFromUtc is { } validFrom && issuedAtUtc < validFrom)
                            {
                                context.Fail("Token was issued before the user's last sign-out-everywhere.");
                            }
                        }
                    }
                };
            });

        // Policies (Registrar-only)
        builder.Services.AddAppAuthorization();

        // API + Scalar Bearer
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Paste the access token from /api/auth/login"
                };
                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
                return Task.CompletedTask;
            });
        });

        // ========== BUILD (wala nang builder.Services pagkatapos nito) ==========
        var app = builder.Build();

        // Seed data
        await app.Services.MigrateAndSeedAsync();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        // JSON body para sa 401/403
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;

            if (response.StatusCode is StatusCodes.Status401Unauthorized
                                    or StatusCodes.Status403Forbidden)
            {
                response.ContentType = "application/problem+json";
                await response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = response.StatusCode,
                    Title = response.StatusCode == StatusCodes.Status401Unauthorized
                        ? "Unauthorized" : "Forbidden",
                    Detail = response.StatusCode == StatusCodes.Status401Unauthorized
                        ? "Authentication is required to access this resource."
                        : "You do not have permission to perform this action."
                });
            }
        });

        // Middleware: EKSAKTONG PAGKAKASUNOD
        app.UseAuthentication();   // who are you?
        app.UseAuthorization();    // are you allowed?

        // Root endpoint
        app.MapGet("/", () => "Welcome to the Vertical Slice API! Go to /scalar/v1 for API documentation.");

        // Endpoints
        app.MapStudentEndpoints();
        app.MapCourseEndpoints();
        app.MapAdmissionEndpoints();
        app.MapProductEndpoints();
        app.MapAuthEndpoints();

        app.Run();
    }
}