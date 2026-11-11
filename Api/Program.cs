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

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Scalar.AspNetCore;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // DATABASE
        builder.Services.AddDbContext<IAppDbContext, AppDbContext>(options =>
            options.UseSqlite(
                builder.Configuration.GetConnectionString("Database")));

        // VALIDATORS
        builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentValidator>();

        // HANDLERS
        builder.Services.AddScoped<
            ICommandHandler<CreateStudentCommand, Result<CreateStudentResponse>>,
            CreateStudentCommandHandler>();

        builder.Services.AddScoped<
            IQueryHandler<GetAllStudentsQuery, Result<GetAllStudentsResponse>>,
            GetAllStudentsQueryHandler>();

        builder.Services.AddScoped<
            IQueryHandler<GetStudentByIdQuery, Result<GetStudentByIdResponse>>,
            GetStudentByIdQueryHandler>();

        builder.Services.AddScoped<
            ICommandHandler<CreateCourseCommand, Result<CreateCourseResponse>>,
            CreateCourseCommandHandler>();

        builder.Services.AddScoped<
            IQueryHandler<GetAllCoursesQuery, Result<GetAllCoursesResponse>>,
            GetAllCoursesQueryHandler>();

        builder.Services.AddScoped<
            IQueryHandler<GetCourseByIdQuery, Result<GetCourseByIdResponse>>,
            GetCourseByIdQueryHandler>();

        builder.Services.AddScoped<
            ICommandHandler<CreateAdmissionCommand, Result<CreateAdmissionResponse>>,
            CreateAdmissionCommandHandler>();

        builder.Services.AddScoped<
            IQueryHandler<GetAdmissionByIdQuery, Result<GetAdmissionByIdResponse>>,
            GetAdmissionByIdQueryHandler>();

        builder.Services.AddScoped<
            IQueryHandler<GetAllAdmissionsQuery, Result<GetAllAdmissionsResponse>>,
            GetAllAdmissionsQueryHandler>();

        builder.Services.AddScoped<
            ICommandHandler<UpdateCourseCommand, Result<UpdateCourseResponse>>,
            UpdateCourseCommandHandler>();

        builder.Services.AddScoped<
            ICommandHandler<DeleteCourseCommand, Result>,
            DeleteCourseCommandHandler>();

        builder.Services.AddScoped<
            ICommandHandler<CreateProductCommand, Result<CreateProductResponse>>,
            CreateProductHandler>();

        builder.Services.AddScoped<
            IQueryHandler<GetProductByIdQuery, Result<GetProductByIdResponse>>,
            GetProductByIdHandler>();

        builder.Services.AddScoped<
            IQueryHandler<GetAllProductsQuery, Result<List<GetProductByIdResponse>>>,
            GetAllProductsHandler>();

        builder.Services.AddScoped<
            ICommandHandler<UpdateProductCommand, Result<GetProductByIdResponse>>,
            UpdateProductHandler>();

        builder.Services.AddScoped<
            ICommandHandler<DeleteProductCommand, Result<Guid>>,
            DeleteProductHandler>();

        // AUTHENTICATION SERVICES
        builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

        builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.SigningKey),
                "Jwt:SigningKey is required.")
            .Validate(
                o => Encoding.UTF8.GetByteCount(o.SigningKey ?? "") >= 32,
                "Jwt:SigningKey must be at least 32 characters.")
            .ValidateOnStart();

        builder.Services.AddScoped<ITokenService, JwtTokenService>();

        builder.Services.AddScoped<
            ICommandHandler<RegisterCommand, Result<RegisterResponse>>,
            RegisterCommandHandler>();

        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

        builder.Services.AddScoped<
            ICommandHandler<LoginCommand, Result<LoginResponse>>,
            LoginCommandHandler>();

        // PART C: REVOKED TOKEN CLEANUP SERVICE
        builder.Services.AddHostedService<
            Infrastructure.BackgroundJobs.RevokedTokenCleanupService>();

        // API AND OPENAPI
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();

                document.Components.SecuritySchemes ??=
                    new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes["Bearer"] =
                    new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description =
                            "Paste the access token from /api/auth/login"
                    };

                document.Security ??= [];

                document.Security.Add(
                    new OpenApiSecurityRequirement
                    {
                        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                    });

                return Task.CompletedTask;
            });
        });

        // JWT VALIDATION
        var jwtSection =
            builder.Configuration.GetSection(JwtOptions.SectionName);

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // TOKEN VALIDATION PARAMETERS
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],

                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                jwtSection["SigningKey"]!)),

                        ClockSkew = TimeSpan.FromSeconds(30)
                    };

                // PART C: CHECK REVOKED TOKENS AND SECURITY STAMP
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var dbContext =
                            context.HttpContext.RequestServices
                                .GetRequiredService<IAppDbContext>();

                        var jti = context.Principal?
                            .FindFirstValue(JwtRegisteredClaimNames.Jti);

                        var sub = context.Principal?
                            .FindFirstValue(JwtRegisteredClaimNames.Sub);

                        var iatRaw = context.Principal?
                            .FindFirstValue(JwtRegisteredClaimNames.Iat);

                        // CHECK REVOKED TOKEN
                        if (jti is not null &&
                            await dbContext.RevokedTokens
                                .AnyAsync(r => r.Jti == jti))
                        {
                            context.Fail("Token has been revoked.");
                            return;
                        }

                        // CHECK USER ID AND ISSUED-AT CLAIM
                        if (sub is null ||
                            !Guid.TryParse(sub, out var userId) ||
                            iatRaw is null ||
                            !long.TryParse(iatRaw, out var iatUnix))
                        {
                            context.Fail("Invalid token claims.");
                            return;
                        }

                        var issuedAtUtc =
                            DateTimeOffset
                                .FromUnixTimeSeconds(iatUnix)
                                .UtcDateTime;

                        // FIND USER
                        var user = await dbContext.Users
                            .FirstOrDefaultAsync(u => u.Id == userId);

                        if (user is null)
                        {
                            context.Fail("User not found.");
                            return;
                        }

                        // CHECK LOGOUT ALL / SECURITY STAMP
                        if (user.TokensValidFromUtc is { } validFrom &&
                            issuedAtUtc < validFrom)
                        {
                            context.Fail(
                                "Token was issued before the user's last sign-out-everywhere.");

                            return;
                        }
                    }
                };
            });

        builder.Services.AddAuthorization();

        // BUILD APPLICATION
        var app = builder.Build();

        // SEED DATA AND MIGRATIONS
        await app.Services.MigrateAndSeedAsync();

        // DEVELOPMENT DOCUMENTATION
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        // MIDDLEWARE
        app.UseAuthentication();
        app.UseAuthorization();

        // ROOT ENDPOINT
        app.MapGet(
            "/",
            () => "Welcome to the Vertical Slice API! Go to /scalar/v1 for API documentation.");

        // MAP ENDPOINTS
        app.MapStudentEndpoints();
        app.MapCourseEndpoints();
        app.MapAdmissionEndpoints();
        app.MapProductEndpoints();
        app.MapAuthEndpoints();

        app.Run();
    }
}

