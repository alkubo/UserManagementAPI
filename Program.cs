using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using UserManagementAPI.Dtos;
using UserManagementAPI.Models;
using UserManagementAPI.Middlewares; // Make sure your middleware classes are in this namespace

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// ✅ Middleware pipeline order
app.UseMiddleware<ErrorHandlingMiddleware>();       // 1. Global error handling
app.UseMiddleware<TokenAuthenticationMiddleware>(); // 2. Token-based authentication
app.UseMiddleware<LoggingMiddleware>();             // 3. Request/response logging

// In-memory user store (demo). Wrapped with a lock for thread-safety.
var users = new List<User>
{
    new User { Id = 1, Name = "Alice Johnson", Email = "alice.johnson@techhive.com", Role = "HR Manager" },
    new User { Id = 2, Name = "Bob Smith", Email = "bob.smith@techhive.com", Role = "IT Admin" }
};
var usersLock = new object();

// Helper: DataAnnotations validation + custom rules
static (bool IsValid, Dictionary<string, string[]> Errors) Validate<T>(
    T model,
    Func<T, List<(string Field, string Message)>>? extraRules = null)
{
    var context = new ValidationContext(model!);
    var results = new List<ValidationResult>();
    var isValid = Validator.TryValidateObject(model!, context, results, validateAllProperties: true);
    var errors = results
        .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
        .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid value").ToArray());

    if (extraRules is not null)
    {
        foreach (var (Field, Message) in extraRules(model))
        {
            if (!errors.ContainsKey(Field)) errors[Field] = Array.Empty<string>();
            errors[Field] = errors[Field].Concat(new[] { Message }).ToArray();
            isValid = false;
        }
    }

    return (isValid, errors);
}

// GET: /api/users?page=1&pageSize=20
app.MapGet("/api/users", (int? page, int? pageSize) =>
{
    var p = page.GetValueOrDefault(1);
    var ps = pageSize.GetValueOrDefault(20);
    if (p < 1 || ps < 1 || ps > 200)
    {
        return Results.Problem(
            title: "Invalid pagination parameters",
            detail: "page must be >= 1 and pageSize must be between 1 and 200.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    List<User> snapshot;
    lock (usersLock)
    {
        snapshot = users.ToList();
    }

    var total = snapshot.Count;
    var items = snapshot.Skip((p - 1) * ps).Take(ps).ToList();

    return Results.Ok(new { page = p, pageSize = ps, total, items });
});

// GET: /api/users/{id}
app.MapGet("/api/users/{id:int}", (int id) =>
{
    User? user;
    lock (usersLock)
    {
        user = users.FirstOrDefault(u => u.Id == id);
    }

    if (user is null)
    {
        return Results.Problem(
            title: "User not found",
            detail: $"No user exists with id {id}.",
            statusCode: StatusCodes.Status404NotFound);
    }

    return Results.Ok(user);
});

// POST: /api/users
app.MapPost("/api/users", (UserCreateDto newUser) =>
{
    var (isValid, errors) = Validate(newUser, m =>
    {
        var extra = new List<(string Field, string Message)>();
        if (!IsValidEmail(m.Email)) extra.Add((nameof(m.Email), "Email format is invalid."));
        return extra;
    });

    if (!isValid) return Results.ValidationProblem(errors);

    lock (usersLock)
    {
        if (users.Any(u => string.Equals(u.Email, newUser.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return Results.Problem(
                title: "Duplicate email",
                detail: $"A user with email '{newUser.Email}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var nextId = users.Count == 0 ? 1 : users.Max(u => u.Id) + 1;
        var user = new User
        {
            Id = nextId,
            Name = newUser.Name.Trim(),
            Email = newUser.Email.Trim(),
            Role = newUser.Role.Trim()
        };

        users.Add(user);
        return Results.Created($"/api/users/{user.Id}", user);
    }
});

// PUT: /api/users/{id}
app.MapPut("/api/users/{id:int}", (int id, UserUpdateDto updatedUser) =>
{
    var (isValid, errors) = Validate(updatedUser, m =>
    {
        var extra = new List<(string Field, string Message)>();
        if (!string.IsNullOrWhiteSpace(m.Email) && !IsValidEmail(m.Email))
            extra.Add((nameof(m.Email), "Email format is invalid."));
        return extra;
    });

    if (!isValid) return Results.ValidationProblem(errors);

    lock (usersLock)
    {
        var existing = users.FirstOrDefault(u => u.Id == id);
        if (existing is null)
        {
            return Results.Problem(
                title: "User not found",
                detail: $"No user exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (!string.IsNullOrWhiteSpace(updatedUser.Email) &&
            !string.Equals(existing.Email, updatedUser.Email, StringComparison.OrdinalIgnoreCase) &&
            users.Any(u => string.Equals(u.Email, updatedUser.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return Results.Problem(
                title: "Duplicate email",
                detail: $"A user with email '{updatedUser.Email}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (!string.IsNullOrWhiteSpace(updatedUser.Name))
            existing.Name = updatedUser.Name.Trim();

        if (!string.IsNullOrWhiteSpace(updatedUser.Email))
            existing.Email = updatedUser.Email.Trim();

        if (!string.IsNullOrWhiteSpace(updatedUser.Role))
            existing.Role = updatedUser.Role.Trim();

        return Results.NoContent();
    }
});

// DELETE: /api/users/{id}
app.MapDelete("/api/users/{id:int}", (int id) =>
{
    lock (usersLock)
    {
        var user = users.FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            return Results.Problem(
                title: "User not found",
                detail: $"No user exists with id {id}.",
                statusCode: StatusCodes.Status404NotFound);
        }

        users.Remove(user);
        return Results.NoContent();
    }
});

app.Run();

// Utilities
static bool IsValidEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email)) return false;
    var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
}
