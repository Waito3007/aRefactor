# Ví Dụ Sử Dụng Exception Handling Pattern

## Mục lục

1. [Service Layer Examples](#service-layer-examples)
2. [Controller Examples](#controller-examples)
3. [Repository Examples](#repository-examples)
4. [Validation Examples](#validation-examples)
5. [Complex Scenarios](#complex-scenarios)
6. [Testing Examples](#testing-examples)

---

## Service Layer Examples

### Example 1: Pattern Service - CRUD Operations

```csharp
public class PatternService : IPatternService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PatternService> _logger;

    public PatternService(AppDbContext context, ILogger<PatternService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Lấy pattern theo ID
    public async Task<Pattern> GetPatternByIdAsync(Guid id)
    {
        var pattern = await _context.Patterns
            .Include(p => p.Category)
            .Include(p => p.Implementations)
            .Include(p => p.RefactorExamples)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pattern == null)
        {
            _logger.LogWarning("Pattern with ID {PatternId} not found", id);
            throw new NotFoundException(
                Response.PatternNotFound,
                $"Pattern with ID {id} not found"
            );
        }

        return pattern;
    }

    // GET: Lấy pattern theo slug
    public async Task<Pattern> GetPatternBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException(
                Response.ValidationFailed,
                "Slug cannot be empty",
                new Dictionary<string, string[]>
                {
                    { "Slug", new[] { "Slug is required" } }
                }
            );
        }

        var pattern = await _context.Patterns
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug);

        if (pattern == null)
        {
            throw new NotFoundException(
                Response.PatternNotFound,
                $"Pattern with slug '{slug}' not found"
            );
        }

        return pattern;
    }

    // POST: Tạo pattern mới
    public async Task<Pattern> CreatePatternAsync(CreatePatternDto dto)
    {
        // Validate DTO
        await ValidateCreatePatternDto(dto);

        // Check if slug already exists
        var existingPattern = await _context.Patterns
            .FirstOrDefaultAsync(p => p.Slug == dto.Slug);

        if (existingPattern != null)
        {
            throw new ValidationException(
                Response.ValidationFailed,
                "Pattern with this slug already exists",
                new Dictionary<string, string[]>
                {
                    { "Slug", new[] { $"Slug '{dto.Slug}' is already in use" } }
                }
            );
        }

        // Check if category exists
        var category = await _context.Categories
            .FindAsync(dto.CategoryId);

        if (category == null)
        {
            throw new NotFoundException(
                Response.CategoryNotFound,
                $"Category with ID {dto.CategoryId} not found"
            );
        }

        // Create pattern
        var pattern = new Pattern
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Slug,
            Summary = dto.Summary,
            Problem = dto.Problem,
            Solution = dto.Solution,
            CategoryId = dto.CategoryId
        };

        _context.Patterns.Add(pattern);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pattern {PatternName} created successfully with ID {PatternId}",
            pattern.Name, pattern.Id);

        return pattern;
    }

    // PUT: Update pattern
    public async Task<Pattern> UpdatePatternAsync(Guid id, UpdatePatternDto dto)
    {
        var pattern = await GetPatternByIdAsync(id); // Throws if not found

        // Validate DTO
        await ValidateUpdatePatternDto(dto);

        // Check if new slug conflicts with another pattern
        if (dto.Slug != pattern.Slug)
        {
            var existingPattern = await _context.Patterns
                .FirstOrDefaultAsync(p => p.Slug == dto.Slug && p.Id != id);

            if (existingPattern != null)
            {
                throw new ValidationException(
                    Response.ValidationFailed,
                    "Pattern with this slug already exists",
                    new Dictionary<string, string[]>
                    {
                        { "Slug", new[] { $"Slug '{dto.Slug}' is already in use" } }
                    }
                );
            }
        }

        // Update fields
        pattern.Name = dto.Name;
        pattern.Slug = dto.Slug;
        pattern.Summary = dto.Summary;
        pattern.Problem = dto.Problem;
        pattern.Solution = dto.Solution;

        if (dto.CategoryId.HasValue && dto.CategoryId != pattern.CategoryId)
        {
            var category = await _context.Categories.FindAsync(dto.CategoryId.Value);
            if (category == null)
            {
                throw new NotFoundException(
                    Response.CategoryNotFound,
                    $"Category with ID {dto.CategoryId} not found"
                );
            }
            pattern.CategoryId = dto.CategoryId.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pattern {PatternName} updated successfully", pattern.Name);

        return pattern;
    }

    // DELETE: Xóa pattern
    public async Task DeletePatternAsync(Guid id)
    {
        var pattern = await GetPatternByIdAsync(id); // Throws if not found

        // Check if pattern has implementations
        var hasImplementations = await _context.Implementations
            .AnyAsync(i => i.PatternId == id);

        if (hasImplementations)
        {
            throw new ValidationException(
                Response.ValidationFailed,
                "Cannot delete pattern with existing implementations",
                new Dictionary<string, string[]>
                {
                    { "Pattern", new[] { "Please delete all implementations first" } }
                }
            );
        }

        _context.Patterns.Remove(pattern);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Pattern {PatternName} deleted successfully", pattern.Name);
    }

    // Helper: Validate CreatePatternDto
    private async Task ValidateCreatePatternDto(CreatePatternDto dto)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.AddError("Name", "Name is required");
        else if (dto.Name.Length > 200)
            errors.AddError("Name", "Name must not exceed 200 characters");

        if (string.IsNullOrWhiteSpace(dto.Slug))
            errors.AddError("Slug", "Slug is required");
        else if (dto.Slug.Length > 200)
            errors.AddError("Slug", "Slug must not exceed 200 characters");
        else if (!IsValidSlug(dto.Slug))
            errors.AddError("Slug", "Slug must contain only lowercase letters, numbers, and hyphens");

        if (string.IsNullOrWhiteSpace(dto.Summary))
            errors.AddError("Summary", "Summary is required");

        if (string.IsNullOrWhiteSpace(dto.Problem))
            errors.AddError("Problem", "Problem is required");

        if (string.IsNullOrWhiteSpace(dto.Solution))
            errors.AddError("Solution", "Solution is required");

        if (dto.CategoryId == Guid.Empty)
            errors.AddError("CategoryId", "CategoryId is required");

        if (errors.Any())
        {
            throw new ValidationException(
                Response.ValidationFailed,
                "Validation failed",
                errors.ToDictionary(k => k.Key, v => v.Value.ToArray())
            );
        }
    }

    private bool IsValidSlug(string slug)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$");
    }
}

// Extension method for cleaner validation code
public static class DictionaryExtensions
{
    public static void AddError(this Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.ContainsKey(key))
            errors[key] = new List<string>();

        errors[key].Add(message);
    }
}
```

---

## Controller Examples

### Example 2: Pattern Controller - RESTful API

```csharp
[ApiController]
[Route("api/[controller]")]
public class PatternsController : ControllerBase
{
    private readonly IPatternService _patternService;
    private readonly ILogger<PatternsController> _logger;

    public PatternsController(
        IPatternService patternService,
        ILogger<PatternsController> logger)
    {
        _patternService = patternService;
        _logger = logger;
    }

    // GET: api/patterns
    [HttpGet]
    [ProducesResponseType(typeof(Response<List<Pattern>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPatterns()
    {
        var patterns = await _patternService.GetAllPatternsAsync();

        return Ok(Response<List<Pattern>>.SuccessResponse(
            patterns,
            messageKey: Response.Success,
            message: "All patterns retrieved successfully"
        ));
    }

    // GET: api/patterns/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Response<Pattern>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPattern(Guid id)
    {
        // Service sẽ throw NotFoundException nếu không tìm thấy
        // ExceptionMiddleware sẽ catch và trả về 404
        var pattern = await _patternService.GetPatternByIdAsync(id);

        return Ok(Response<Pattern>.SuccessResponse(
            pattern,
            messageKey: Response.Success,
            message: "Pattern retrieved successfully"
        ));
    }

    // GET: api/patterns/by-slug/{slug}
    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(typeof(Response<Pattern>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<Dictionary<string, string[]>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPatternBySlug(string slug)
    {
        var pattern = await _patternService.GetPatternBySlugAsync(slug);

        return Ok(Response<Pattern>.SuccessResponse(pattern));
    }

    // GET: api/patterns/category/{categoryId}
    [HttpGet("category/{categoryId}")]
    [ProducesResponseType(typeof(Response<List<Pattern>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatternsByCategory(Guid categoryId)
    {
        var patterns = await _patternService.GetPatternsByCategoryAsync(categoryId);

        return Ok(Response<List<Pattern>>.SuccessResponse(
            patterns,
            messageKey: Response.Success,
            message: $"Retrieved {patterns.Count} patterns"
        ));
    }

    // POST: api/patterns
    [HttpPost]
    [ProducesResponseType(typeof(Response<Pattern>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<Dictionary<string, string[]>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePattern([FromBody] CreatePatternDto dto)
    {
        var pattern = await _patternService.CreatePatternAsync(dto);

        return CreatedAtAction(
            nameof(GetPattern),
            new { id = pattern.Id },
            Response<Pattern>.SuccessResponse(
                pattern,
                messageKey: Response.PatternCreated,
                message: $"Pattern '{pattern.Name}' created successfully"
            )
        );
    }

    // PUT: api/patterns/{id}
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Response<Pattern>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<Dictionary<string, string[]>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePattern(Guid id, [FromBody] UpdatePatternDto dto)
    {
        var pattern = await _patternService.UpdatePatternAsync(id, dto);

        return Ok(Response<Pattern>.SuccessResponse(
            pattern,
            messageKey: Response.PatternUpdated,
            message: $"Pattern '{pattern.Name}' updated successfully"
        ));
    }

    // DELETE: api/patterns/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<Dictionary<string, string[]>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePattern(Guid id)
    {
        await _patternService.DeletePatternAsync(id);

        return Ok(Response<object>.SuccessResponse(
            null,
            messageKey: Response.PatternDeleted,
            message: "Pattern deleted successfully"
        ));
    }

    // GET: api/patterns/{id}/implementations
    [HttpGet("{id}/implementations")]
    [ProducesResponseType(typeof(Response<List<Implementation>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPatternImplementations(Guid id)
    {
        var implementations = await _patternService.GetImplementationsAsync(id);

        return Ok(Response<List<Implementation>>.SuccessResponse(
            implementations,
            message: $"Retrieved {implementations.Count} implementations"
        ));
    }
}
```

---

## Repository Examples

### Example 3: Generic Repository với Exception Handling

```csharp
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<Repository<T>> _logger;

    public Repository(AppDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    public async Task<T> GetByIdAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);

        if (entity == null)
        {
            var entityName = typeof(T).Name;
            _logger.LogWarning("{EntityName} with ID {Id} not found", entityName, id);

            throw new NotFoundException(
                $"{entityName.ToLower()}.not_found",
                $"{entityName} with ID {id} not found"
            );
        }

        return entity;
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        try
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{EntityName} added successfully", typeof(T).Name);

            return entity;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while adding {EntityName}", typeof(T).Name);

            throw new ProjectException(
                Response.BadRequest,
                "Failed to add entity to database",
                HttpStatusCode.BadRequest,
                "DATABASE_ERROR"
            );
        }
    }

    public async Task<T> UpdateAsync(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{EntityName} updated successfully", typeof(T).Name);

            return entity;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error while updating {EntityName}", typeof(T).Name);

            throw new ProjectException(
                Response.BadRequest,
                "Entity was modified by another user",
                HttpStatusCode.Conflict,
                "CONCURRENCY_ERROR"
            );
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating {EntityName}", typeof(T).Name);

            throw new ProjectException(
                Response.BadRequest,
                "Failed to update entity in database",
                HttpStatusCode.BadRequest,
                "DATABASE_ERROR"
            );
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id); // Throws if not found

        try
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{EntityName} with ID {Id} deleted successfully",
                typeof(T).Name, id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while deleting {EntityName}", typeof(T).Name);

            throw new ProjectException(
                Response.BadRequest,
                "Failed to delete entity from database",
                HttpStatusCode.BadRequest,
                "DATABASE_ERROR"
            );
        }
    }
}
```

---

## Validation Examples

### Example 4: FluentValidation Integration

```csharp
// Install: FluentValidation.AspNetCore

public class CreatePatternDtoValidator : AbstractValidator<CreatePatternDto>
{
    private readonly AppDbContext _context;

    public CreatePatternDtoValidator(AppDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MaximumLength(200).WithMessage("Slug must not exceed 200 characters")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Slug must contain only lowercase letters, numbers, and hyphens")
            .MustAsync(BeUniqueSlug).WithMessage("Slug is already in use");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Summary is required");

        RuleFor(x => x.Problem)
            .NotEmpty().WithMessage("Problem is required");

        RuleFor(x => x.Solution)
            .NotEmpty().WithMessage("Solution is required");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required")
            .MustAsync(CategoryExists).WithMessage("Category not found");
    }

    private async Task<bool> BeUniqueSlug(string slug, CancellationToken cancellationToken)
    {
        return !await _context.Patterns.AnyAsync(p => p.Slug == slug, cancellationToken);
    }

    private async Task<bool> CategoryExists(Guid categoryId, CancellationToken cancellationToken)
    {
        return await _context.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken);
    }
}

// Program.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePatternDtoValidator>();

// Custom validation filter
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value.Errors.Any())
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            throw new ValidationException(
                Response.ValidationFailed,
                "Validation failed",
                errors
            );
        }

        await next();
    }
}

// Register filter
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
```

---

## Complex Scenarios

### Example 5: Authorization với Custom Exceptions

```csharp
public class PatternAuthorizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _context;

    public async Task<bool> CanUserModifyPattern(Guid patternId)
    {
        var userId = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedException(
                Response.Unauthorized,
                "User is not authenticated"
            );
        }

        var pattern = await _context.Patterns.FindAsync(patternId);
        if (pattern == null)
        {
            throw new NotFoundException(
                Response.PatternNotFound,
                $"Pattern with ID {patternId} not found"
            );
        }

        // Check if user is admin or owner
        var isAdmin = _httpContextAccessor.HttpContext.User
            .IsInRole("Admin");

        var isOwner = pattern.CreatedBy == userId;

        if (!isAdmin && !isOwner)
        {
            throw new ForbiddenException(
                Response.PermissionDenied,
                "You don't have permission to modify this pattern"
            );
        }

        return true;
    }
}

// Usage in Service
public async Task<Pattern> UpdatePatternAsync(Guid id, UpdatePatternDto dto)
{
    // Check authorization
    await _authService.CanUserModifyPattern(id);

    // Continue with update...
    var pattern = await GetPatternByIdAsync(id);
    // ...
}
```

### Example 6: Transaction với Error Handling

```csharp
public class ImplementationService
{
    public async Task<Implementation> CreateImplementationWithFilesAsync(
        CreateImplementationDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate pattern exists
            var pattern = await _context.Patterns.FindAsync(dto.PatternId);
            if (pattern == null)
            {
                throw new NotFoundException(
                    Response.PatternNotFound,
                    $"Pattern with ID {dto.PatternId} not found"
                );
            }

            // Create implementation
            var implementation = new Implementation
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description,
                PatternId = dto.PatternId
            };

            _context.Implementations.Add(implementation);
            await _context.SaveChangesAsync();

            // Create files
            foreach (var fileDto in dto.Files)
            {
                var file = new ImplementationFile
                {
                    Id = Guid.NewGuid(),
                    FilePath = fileDto.FilePath,
                    CodeBlock = fileDto.CodeBlock,
                    Language = fileDto.Language,
                    Notes = fileDto.Notes,
                    ImplementationId = implementation.Id
                };

                _context.ImplementationFiles.Add(file);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Implementation '{Title}' created with {FileCount} files",
                implementation.Title,
                dto.Files.Count
            );

            return implementation;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Transaction rolled back due to error");
            throw; // Re-throw to be handled by middleware
        }
    }
}
```

---

## Testing Examples

### Example 7: Unit Tests

```csharp
public class PatternServiceTests
{
    private readonly Mock<AppDbContext> _mockContext;
    private readonly Mock<ILogger<PatternService>> _mockLogger;
    private readonly PatternService _service;

    public PatternServiceTests()
    {
        _mockContext = new Mock<AppDbContext>();
        _mockLogger = new Mock<ILogger<PatternService>>();
        _service = new PatternService(_mockContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPatternByIdAsync_PatternNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var patternId = Guid.NewGuid();
        _mockContext.Setup(x => x.Patterns.FindAsync(patternId))
            .ReturnsAsync((Pattern)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetPatternByIdAsync(patternId)
        );

        Assert.Equal(Response.PatternNotFound, exception.MessageKey);
        Assert.Contains(patternId.ToString(), exception.Message);
    }

    [Fact]
    public async Task CreatePatternAsync_DuplicateSlug_ThrowsValidationException()
    {
        // Arrange
        var dto = new CreatePatternDto
        {
            Name = "Test Pattern",
            Slug = "test-pattern",
            CategoryId = Guid.NewGuid()
        };

        var existingPattern = new Pattern { Slug = "test-pattern" };

        _mockContext.Setup(x => x.Patterns.FirstOrDefaultAsync(
            It.IsAny<Expression<Func<Pattern, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPattern);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreatePatternAsync(dto)
        );

        Assert.Equal(Response.ValidationFailed, exception.MessageKey);
        Assert.Contains("Slug", exception.Errors.Keys);
    }
}
```

### Example 8: Integration Tests

```csharp
public class PatternsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public PatternsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPattern_NonExistentId_Returns404()
    {
        // Arrange
        var patternId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/patterns/{patternId}");
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Response<object>>(content);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(result.Success);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Contains("not found", result.Message.ToLower());
    }

    [Fact]
    public async Task CreatePattern_InvalidData_Returns400WithValidationErrors()
    {
        // Arrange
        var dto = new CreatePatternDto
        {
            Name = "", // Invalid: empty name
            Slug = "INVALID SLUG", // Invalid: uppercase and spaces
            CategoryId = Guid.Empty // Invalid: empty guid
        };

        var content = new StringContent(
            JsonSerializer.Serialize(dto),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/patterns", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Response<Dictionary<string, string[]>>>(
            responseContent
        );

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(result.Success);
        Assert.Equal("VALIDATION_ERROR", result.ErrorCode);
        Assert.Contains("Name", result.Data.Keys);
        Assert.Contains("Slug", result.Data.Keys);
        Assert.Contains("CategoryId", result.Data.Keys);
    }
}
```

---

## Best Practices Summary

### ✅ DO:

- Throw specific exceptions (NotFoundException, ValidationException)
- Include meaningful error messages
- Use message keys for i18n
- Log before throwing exceptions
- Let middleware handle exception responses
- Use transactions for multi-step operations
- Validate input early in the service layer

### ❌ DON'T:

- Catch exceptions in controllers
- Throw generic Exception
- Return error responses manually
- Ignore validation
- Swallow exceptions silently
- Use try-catch for flow control

---

Tài liệu này cung cấp ví dụ thực tế cho mọi tình huống phổ biến. Tham khảo các file khác để hiểu sâu hơn về từng thành phần!
