using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using NurseryManagement.Api.Data;
using NurseryManagement.Api.Middleware;
using NurseryManagement.Api.Security;
using NurseryManagement.Api.Services;
using NurseryManagement.Api.Services.Logging;
using NurseryManagement.Api.Services.Children;
using NurseryManagement.Api.Services.Classes;
using NurseryManagement.Api.Services.Staff;
using NurseryManagement.Api.Services.Finance;
using NurseryManagement.Api.Services.Entry;
using NurseryManagement.Api.Services.Payroll;
using NurseryManagement.Api.Services.SecurityAdmin;
using NurseryManagement.Api.Services.Dashboard;
using NurseryManagement.Api.Services.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppDatabase(builder.Configuration);
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IChildrenService, ChildrenService>();
builder.Services.AddScoped<IClassesService, ClassesService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IPricePlansService, PricePlansService>();
builder.Services.AddScoped<IPaymentsService, PaymentsService>();
builder.Services.AddScoped<IExpensesService, ExpensesService>();
builder.Services.AddScoped<IEntryService, EntryService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<ISalariesService, SalariesService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IRolesService, RolesService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IAppLogService, AppLogService>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "NurseryManagement_Dev_Secret_Key_ChangeMe_32+";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NurseryManagement",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NurseryManagement",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionPolicies.All)
    {
        options.AddPolicy(permission, policy =>
            policy.Requirements.Add(new PermissionRequirement(permission)));
    }

    // Dropdown/list access for enrollment and payments without granting the price-plans menu.
    options.AddPolicy(PermissionPolicies.PricePlansList, policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("Admin") ||
            context.User.HasClaim("permission", PermissionPolicies.PricePlansRead) ||
            context.User.HasClaim("permission", PermissionPolicies.ChildrenWrite) ||
            context.User.HasClaim("permission", PermissionPolicies.PaymentsRead) ||
            context.User.HasClaim("permission", PermissionPolicies.PaymentsWrite)));
});

const string corsPolicy = "AngularDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.WithOrigins(
                "https://localhost:4200",
                "http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(corsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ActionLoggingMiddleware>();
app.MapControllers();

app.Run();
