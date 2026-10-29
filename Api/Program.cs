using Api.Features.Admissions;
using Api.Features.Courses;
using Api.Features.Products;
using Api.Features.Students;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Admissions.CreateAdmission;
using Application.Features.Admissions.GetAdmissionById;
using Application.Features.Admissions.GetAllAdmissions;
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
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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

        // API
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Vertical Slice API";
                document.Info.Description = "A simple API demonstrating Vertical Slice Architecture";
                return Task.CompletedTask;
            });
        });

        var app = builder.Build();

        // Seed data
        await app.Services.MigrateAndSeedAsync();
       
            
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        // Root endpoint
        app.MapGet("/", () => "Welcome to the Vertical Slice API! Go to /scalar/v1 for API documentation.");

        // Map endpoints
        app.MapStudentEndpoints();
        app.MapCourseEndpoints();
        app.MapAdmissionEndpoints();
        app.MapProductEndpoints();
        app.Run();
    }
}