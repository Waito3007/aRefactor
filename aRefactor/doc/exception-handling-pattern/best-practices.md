# Best Practices - Exception Handling Pattern

## Nguyên Tắc Cốt Lõi

### 1. Single Responsibility Principle (SRP)

Mỗi layer chỉ làm một việc:

```csharp
// ✅ ĐÚNG - Controller chỉ điều phối
[HttpGet("{id}")]
public async Task<IActionResult> GetPattern(Guid id)
{
    var pattern = await _service.GetPatternAsync(id);
    return Ok(Response<Pattern>.SuccessResponse(pattern));
}

// ✅ ĐÚNG - Service xử lý business logic và throw exception
public async Task<Pattern> GetPatternAsync(Guid id)
{
    var pattern = await _repository.FindByIdAsync(id);

    if (pattern == null)
    {
        throw new NotFoundException(
            Response.PatternNotFound,
            $"Pattern with ID {id} not found"
        );
    }

    return pattern;
}

// ✅ ĐÚNG - Middleware xử lý exception và format response
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context);
    }
    catch (Exception ex)
    {
        await HandleExceptionAsync(context, ex);
    }
}
```

```csharp
// ❌ SAI - Controller không nên xử lý exception
[HttpGet("{id}")]
public async Task<IActionResult> GetPattern(Guid id)
{
    try
    {
        var pattern = await _service.GetPatternAsync(id);
        return Ok(pattern);
    }
    catch (NotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
}
```

---

## Layer-Specific Guidelines

### Controller Layer

#### ✅ DO:

```csharp
// 1. Chỉ gọi service và return response
[HttpPost]
public async Task<IActionResult> CreatePattern([FromBody] CreatePatternDto dto)
{
    var pattern = await _service.CreatePatternAsync(dto);

    return CreatedAtAction(
        nameof(GetPattern),
        new { id = pattern.Id },
        Response<Pattern>.SuccessResponse(pattern)
    );
}

// 2. Sử dụng ProducesResponseType để document API
[HttpGet("{id}")]
[ProducesResponseType(typeof(Response<Pattern>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetPattern(Guid id)
{
    var pattern = await _service.GetPatternAsync(id);
    return Ok(Response<Pattern>.SuccessResponse(pattern));
}

// 3. Validate input ở controller level (model binding)
[HttpPost]
public async Task<IActionResult> CreatePattern([FromBody] CreatePatternDto dto)
{
    // Model binding validation sẽ tự động chạy
    // ValidationFilter sẽ throw ValidationException nếu có lỗi

    var pattern = await _service.CreatePatternAsync(dto);
    return CreatedAtAction(nameof(GetPattern), new { id = pattern.Id },
        Response<Pattern>.SuccessResponse(pattern));
}
```

#### ❌ DON'T:

```csharp
// 1. Không try-catch trong controller
[HttpGet("{id}")]
public async Task<IActionResult> GetPattern(Guid id)
{
    try // ❌ Middleware đã xử lý rồi
    {
        var pattern = await _service.GetPatternAsync(id);
        return Ok(pattern);
    }
    catch (NotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
}

// 2. Không xử lý business logic trong controller
[HttpPost]
public async Task<IActionResult> CreatePattern([FromBody] CreatePatternDto dto)
{
    // ❌ Business logic không thuộc controller
    if (string.IsNullOrEmpty(dto.Name))
    {
        return BadRequest("Name is required");
    }

    var existingPattern = await _context.Patterns
        .FirstOrDefaultAsync(p => p.Slug == dto.Slug);

    if (existingPattern != null)
    {
        return BadRequest("Slug already exists");
    }

    // ...
}

// 3. Không return data trực tiếp
[HttpGet("{id}")]
public async Task<IActionResult> GetPattern(Guid id)
{
    var pattern = await _service.GetPatternAsync(id);
    return Ok(pattern); // ❌ Thiếu wrapper Response<T>
}
```

---

### Service Layer

#### ✅ DO:

```csharp
// 1. Throw specific exceptions
public async Task<Pattern> GetPatternAsync(Guid id)
{
    var pattern = await _repository.FindByIdAsync(id);

    if (pattern == null)
    {
        throw new NotFoundException(
            Response.PatternNotFound,
            $"Pattern with ID {id} not found"
        );
    }

    return pattern;
}

// 2. Validate business rules
public async Task<Pattern> CreatePatternAsync(CreatePatternDto dto)
{
    // Validate duplicate slug
    var existingPattern = await _repository.FindBySlugAsync(dto.Slug);

    if (existingPattern != null)
    {
        throw new ValidationException(
            Response.ValidationFailed,
            "Slug already exists",
            new Dictionary<string, string[]>
            {
                { "Slug", new[] { $"Slug '{dto.Slug}' is already in use" } }
            }
        );
    }

    // Validate category exists
    var category = await _categoryRepository.FindByIdAsync(dto.CategoryId);

    if (category == null)
    {
        throw new NotFoundException(
            Response.CategoryNotFound,
            $"Category with ID {dto.CategoryId} not found"
        );
    }

    // Create pattern
    var pattern = _mapper.Map<Pattern>(dto);
    return await _repository.AddAsync(pattern);
}

// 3. Log trước khi throw exception
public async Task DeletePatternAsync(Guid id)
{
    var pattern = await GetPatternAsync(id); // Throws if not found

    var hasImplementations = await _implementationRepository
        .HasImplementationsForPattern(id);

    if (hasImplementations)
    {
        _logger.LogWarning(
            "Cannot delete pattern {PatternId} because it has implementations",
            id
        );

        throw new ValidationException(
            Response.ValidationFailed,
            "Cannot delete pattern with implementations",
            new Dictionary<string, string[]>
            {
                { "Pattern", new[] { "Delete all implementations first" } }
            }
        );
    }

    await _repository.DeleteAsync(pattern);

    _logger.LogInformation("Pattern {PatternId} deleted successfully", id);
}

// 4. Sử dụng transactions cho multi-step operations
public async Task<Implementation> CreateImplementationWithFilesAsync(
    CreateImplementationDto dto)
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        var implementation = await CreateImplementationAsync(dto);

        foreach (var fileDto in dto.Files)
        {
            await CreateImplementationFileAsync(implementation.Id, fileDto);
        }

        await transaction.CommitAsync();

        return implementation;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw; // Re-throw to middleware
    }
}
```

#### ❌ DON'T:

```csharp
// 1. Không return null
public async Task<Pattern> GetPatternAsync(Guid id)
{
    var pattern = await _repository.FindByIdAsync(id);
    return pattern; // ❌ Return null nếu không tìm thấy
}

// 2. Không throw generic Exception
public async Task<Pattern> CreatePatternAsync(CreatePatternDto dto)
{
    if (string.IsNullOrEmpty(dto.Name))
    {
        throw new Exception("Name is required"); // ❌ Generic exception
    }
    // ...
}

// 3. Không swallow exceptions
public async Task DeletePatternAsync(Guid id)
{
    try
    {
        await _repository.DeleteAsync(id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting pattern");
        // ❌ Không throw lại, client sẽ nghĩ operation thành công
    }
}

// 4. Không validate trong repository
public async Task<Pattern> AddAsync(Pattern pattern)
{
    // ❌ Validation thuộc service layer
    if (string.IsNullOrEmpty(pattern.Name))
    {
        throw new ValidationException("Name is required");
    }

    await _context.Patterns.AddAsync(pattern);
    await _context.SaveChangesAsync();
    return pattern;
}
```

---

### Repository Layer

#### ✅ DO:

```csharp
// 1. Chỉ xử lý data access
public async Task<Pattern> FindByIdAsync(Guid id)
{
    return await _context.Patterns
        .Include(p => p.Category)
        .Include(p => p.Implementations)
        .FirstOrDefaultAsync(p => p.Id == id);
}

// 2. Return null nếu không tìm thấy (không throw exception)
public async Task<Pattern> FindBySlugAsync(string slug)
{
    return await _context.Patterns
        .FirstOrDefaultAsync(p => p.Slug == slug);
}

// 3. Throw ProjectException cho database errors
public async Task<Pattern> AddAsync(Pattern pattern)
{
    try
    {
        await _context.Patterns.AddAsync(pattern);
        await _context.SaveChangesAsync();
        return pattern;
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database error while adding pattern");

        throw new ProjectException(
            Response.BadRequest,
            "Failed to save to database",
            HttpStatusCode.InternalServerError,
            "DATABASE_ERROR"
        );
    }
}
```

#### ❌ DON'T:

```csharp
// 1. Không throw NotFoundException trong repository
public async Task<Pattern> FindByIdAsync(Guid id)
{
    var pattern = await _context.Patterns.FindAsync(id);

    if (pattern == null)
    {
        // ❌ Repository không nên throw business exceptions
        throw new NotFoundException("Pattern not found");
    }

    return pattern;
}

// 2. Không validate business rules
public async Task<Pattern> AddAsync(Pattern pattern)
{
    // ❌ Business logic không thuộc repository
    var existingPattern = await _context.Patterns
        .FirstOrDefaultAsync(p => p.Slug == pattern.Slug);

    if (existingPattern != null)
    {
        throw new ValidationException("Slug already exists");
    }

    await _context.Patterns.AddAsync(pattern);
    await _context.SaveChangesAsync();
    return pattern;
}
```

---

## Exception Handling Guidelines

### 1. Chọn Exception Đúng

```csharp
// NotFoundException - Resource không tồn tại
if (pattern == null)
{
    throw new NotFoundException(
        Response.PatternNotFound,
        $"Pattern with ID {id} not found"
    );
}

// ValidationException - Input không hợp lệ
if (string.IsNullOrEmpty(dto.Name))
{
    throw new ValidationException(
        Response.ValidationFailed,
        "Validation failed",
        new Dictionary<string, string[]>
        {
            { "Name", new[] { "Name is required" } }
        }
    );
}

// ForbiddenException - Không có quyền
if (!user.IsAdmin && pattern.CreatedBy != user.Id)
{
    throw new ForbiddenException(
        Response.PermissionDenied,
        "You don't have permission to modify this pattern"
    );
}

// UnauthorizedException - Chưa đăng nhập
if (!user.IsAuthenticated)
{
    throw new UnauthorizedException(
        Response.Unauthorized,
        "You must be logged in to perform this action"
    );
}

// ProjectException - Lỗi khác (database, external API, ...)
catch (DbUpdateException ex)
{
    throw new ProjectException(
        Response.InternalServerError,
        "Database error occurred",
        HttpStatusCode.InternalServerError,
        "DATABASE_ERROR"
    );
}
```

### 2. Message Keys Pattern

```csharp
// ✅ ĐÚNG - Sử dụng constants
public static class Response
{
    // Format: {entity}.{action}_{status}
    public const string PatternNotFound = "pattern.not_found";
    public const string PatternCreated = "pattern.created";
    public const string PatternUpdated = "pattern.updated";
    public const string PatternDeleted = "pattern.deleted";
    public const string PatternAlreadyExists = "pattern.already_exists";

    // Format: {entity}.{action}_{constraint}
    public const string PatternHasImplementations = "pattern.has_implementations";
    public const string PatternSlugDuplicate = "pattern.slug_duplicate";

    // Format: error.{type}
    public const string ValidationFailed = "validation.failed";
    public const string Unauthorized = "error.unauthorized";
    public const string PermissionDenied = "error.permission_denied";
}

// ❌ SAI - Hard-coded strings
throw new NotFoundException("pattern_not_found", "Pattern not found");
```

### 3. Error Codes Pattern

```csharp
// ✅ ĐÚNG - Error codes rõ ràng
public class NotFoundException : ProjectException
{
    public NotFoundException(string messageKey, string message)
        : base(messageKey, message, HttpStatusCode.NotFound, "NOT_FOUND")
    {
    }
}

public class ValidationException : ProjectException
{
    public ValidationException(string messageKey, string message,
        Dictionary<string, string[]> errors)
        : base(messageKey, message, HttpStatusCode.BadRequest, "VALIDATION_ERROR")
    {
        Errors = errors;
    }
}

// Custom error codes cho specific cases
throw new ProjectException(
    Response.PatternSlugDuplicate,
    "Pattern slug already exists",
    HttpStatusCode.BadRequest,
    "PATTERN_SLUG_DUPLICATE"
);
```

---

## Logging Best Practices

### 1. Structured Logging

```csharp
// ✅ ĐÚNG - Structured logging với parameters
_logger.LogWarning(
    "Pattern {PatternId} not found for user {UserId}",
    id,
    userId
);

_logger.LogInformation(
    "Pattern {PatternName} created successfully with ID {PatternId}",
    pattern.Name,
    pattern.Id
);

_logger.LogError(
    ex,
    "Failed to create pattern {PatternName}. CategoryId: {CategoryId}",
    dto.Name,
    dto.CategoryId
);

// ❌ SAI - String interpolation
_logger.LogWarning($"Pattern {id} not found for user {userId}");
_logger.LogInformation($"Pattern {pattern.Name} created successfully");
```

### 2. Log Levels

```csharp
// LogDebug - Chi tiết debug
_logger.LogDebug("Validating pattern DTO: {@Dto}", dto);

// LogInformation - Success operations
_logger.LogInformation(
    "Pattern {PatternId} updated successfully",
    pattern.Id
);

// LogWarning - Expected errors (business logic)
_logger.LogWarning(
    "Pattern {PatternId} not found",
    id
);

_logger.LogWarning(
    "Validation failed for pattern creation: {@Errors}",
    errors
);

// LogError - Unexpected errors (infrastructure)
_logger.LogError(
    ex,
    "Database error while creating pattern {PatternName}",
    dto.Name
);

// LogCritical - Application-wide failures
_logger.LogCritical(
    ex,
    "Failed to connect to database"
);
```

### 3. Log Context

```csharp
// ✅ ĐÚNG - Include context
public async Task<Pattern> UpdatePatternAsync(Guid id, UpdatePatternDto dto)
{
    _logger.LogInformation(
        "Updating pattern {PatternId} by user {UserId}",
        id,
        _currentUser.Id
    );

    try
    {
        var pattern = await GetPatternAsync(id);

        // ... update logic

        _logger.LogInformation(
            "Pattern {PatternId} updated successfully. Changed fields: {@Changes}",
            id,
            new { OldName = pattern.Name, NewName = dto.Name }
        );

        return pattern;
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Failed to update pattern {PatternId}. DTO: {@Dto}",
            id,
            dto
        );
        throw;
    }
}
```

---

## Validation Best Practices

### 1. Multi-Layer Validation

```csharp
// Layer 1: Model Binding Validation (DTO Attributes)
public class CreatePatternDto
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name must not exceed 200 characters")]
    public string Name { get; set; }

    [Required]
    [RegularExpression(@"^[a-z0-9-]+$",
        ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens")]
    public string Slug { get; set; }
}

// Layer 2: FluentValidation (Complex Rules)
public class CreatePatternDtoValidator : AbstractValidator<CreatePatternDto>
{
    public CreatePatternDtoValidator(AppDbContext context)
    {
        RuleFor(x => x.Slug)
            .MustAsync(async (slug, cancellation) =>
                !await context.Patterns.AnyAsync(p => p.Slug == slug, cancellation))
            .WithMessage("Slug is already in use");
    }
}

// Layer 3: Service Layer (Business Rules)
public async Task<Pattern> CreatePatternAsync(CreatePatternDto dto)
{
    // Validate category exists
    var category = await _categoryRepository.FindByIdAsync(dto.CategoryId);

    if (category == null)
    {
        throw new NotFoundException(
            Response.CategoryNotFound,
            $"Category with ID {dto.CategoryId} not found"
        );
    }

    // Business rule: Maximum 10 patterns per category
    var patternCount = await _repository.CountByCategoryAsync(dto.CategoryId);

    if (patternCount >= 10)
    {
        throw new ValidationException(
            Response.ValidationFailed,
            "Category has reached maximum number of patterns",
            new Dictionary<string, string[]>
            {
                { "CategoryId", new[] { "This category already has 10 patterns" } }
            }
        );
    }

    // ...
}
```

### 2. Validation Error Format

```csharp
// ✅ ĐÚNG - Dictionary với field names và messages
var errors = new Dictionary<string, string[]>
{
    { "Name", new[] { "Name is required", "Name must be between 3 and 200 characters" } },
    { "Slug", new[] { "Slug is already in use" } },
    { "CategoryId", new[] { "Category not found" } }
};

throw new ValidationException(
    Response.ValidationFailed,
    "Validation failed",
    errors
);

// Response:
{
  "success": false,
  "data": {
    "name": ["Name is required", "Name must be between 3 and 200 characters"],
    "slug": ["Slug is already in use"],
    "categoryId": ["Category not found"]
  }
}
```

---

## Performance Best Practices

### 1. Async/Await Pattern

```csharp
// ✅ ĐÚNG - Async all the way
public async Task<Pattern> GetPatternAsync(Guid id)
{
    var pattern = await _repository.FindByIdAsync(id);

    if (pattern == null)
    {
        throw new NotFoundException(
            Response.PatternNotFound,
            $"Pattern with ID {id} not found"
        );
    }

    return pattern;
}

// ❌ SAI - Blocking async operations
public Pattern GetPattern(Guid id)
{
    var pattern = _repository.FindByIdAsync(id).Result; // ❌ Deadlock risk
    // ...
}
```

### 2. Efficient Queries

```csharp
// ✅ ĐÚNG - Include related data in single query
public async Task<Pattern> GetPatternWithDetailsAsync(Guid id)
{
    var pattern = await _context.Patterns
        .Include(p => p.Category)
        .Include(p => p.Implementations)
            .ThenInclude(i => i.Files)
        .Include(p => p.RefactorExamples)
            .ThenInclude(r => r.Snippets)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (pattern == null)
    {
        throw new NotFoundException(
            Response.PatternNotFound,
            $"Pattern with ID {id} not found"
        );
    }

    return pattern;
}

// ❌ SAI - Multiple queries (N+1 problem)
public async Task<Pattern> GetPatternWithDetails(Guid id)
{
    var pattern = await _context.Patterns.FindAsync(id);
    pattern.Category = await _context.Categories.FindAsync(pattern.CategoryId);
    pattern.Implementations = await _context.Implementations
        .Where(i => i.PatternId == id).ToListAsync();
    // ...
}
```

---

## Security Best Practices

### 1. Input Sanitization

```csharp
// ✅ ĐÚNG - Validate and sanitize input
public async Task<Pattern> CreatePatternAsync(CreatePatternDto dto)
{
    // Trim whitespace
    dto.Name = dto.Name?.Trim();
    dto.Slug = dto.Slug?.Trim().ToLower();

    // Validate format
    if (!IsValidSlug(dto.Slug))
    {
        throw new ValidationException(
            Response.ValidationFailed,
            "Invalid slug format",
            new Dictionary<string, string[]>
            {
                { "Slug", new[] { "Slug must contain only lowercase letters, numbers, and hyphens" } }
            }
        );
    }

    // ...
}

private bool IsValidSlug(string slug)
{
    return Regex.IsMatch(slug, @"^[a-z0-9-]+$");
}
```

### 2. Authorization Checks

```csharp
// ✅ ĐÚNG - Check permissions
public async Task<Pattern> UpdatePatternAsync(Guid id, UpdatePatternDto dto)
{
    var pattern = await GetPatternAsync(id);

    // Check if user has permission
    if (!await _authService.CanModifyPattern(id, _currentUser.Id))
    {
        throw new ForbiddenException(
            Response.PermissionDenied,
            "You don't have permission to modify this pattern"
        );
    }

    // ...
}
```

---

## Testing Best Practices

### 1. Test Exception Scenarios

```csharp
[Fact]
public async Task GetPatternAsync_NotFound_ThrowsNotFoundException()
{
    // Arrange
    var patternId = Guid.NewGuid();
    _mockRepository
        .Setup(x => x.FindByIdAsync(patternId))
        .ReturnsAsync((Pattern)null);

    // Act & Assert
    var exception = await Assert.ThrowsAsync<NotFoundException>(
        () => _service.GetPatternAsync(patternId)
    );

    Assert.Equal(Response.PatternNotFound, exception.MessageKey);
    Assert.Contains(patternId.ToString(), exception.Message);
}

[Fact]
public async Task CreatePatternAsync_DuplicateSlug_ThrowsValidationException()
{
    // Arrange
    var dto = new CreatePatternDto { Slug = "test-pattern" };
    _mockRepository
        .Setup(x => x.FindBySlugAsync("test-pattern"))
        .ReturnsAsync(new Pattern { Slug = "test-pattern" });

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ValidationException>(
        () => _service.CreatePatternAsync(dto)
    );

    Assert.Contains("Slug", exception.Errors.Keys);
}
```

---

## Checklist

### Pre-Deployment Checklist

- [ ] ExceptionMiddleware đã đăng ký đầu tiên trong pipeline
- [ ] Tất cả controllers đều return Response<T>
- [ ] Services throw specific exceptions (không throw generic Exception)
- [ ] Repository chỉ xử lý data access (không throw business exceptions)
- [ ] Validation có ở cả DTO attributes và service layer
- [ ] Log đầy đủ với structured logging
- [ ] Message keys đã định nghĩa trong Response class
- [ ] Error codes rõ ràng và consistent
- [ ] Unit tests cover exception scenarios
- [ ] Integration tests verify correct HTTP status codes

### Code Review Checklist

- [ ] Không có try-catch trong controllers
- [ ] Exceptions có messageKey và message phù hợp
- [ ] Log trước khi throw exception
- [ ] Async/await sử dụng đúng cách
- [ ] Input đã được validate và sanitize
- [ ] Authorization checks ở đúng nơi
- [ ] Transactions cho multi-step operations
- [ ] Tests cover edge cases

---

Tuân thủ các best practices này sẽ đảm bảo code base maintainable, testable, và scalable!
