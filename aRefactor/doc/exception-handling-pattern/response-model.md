# Response Model - Chuẩn Hóa API Response

## Mục đích

Response Model cung cấp cấu trúc thống nhất cho tất cả API responses, bao gồm cả success và error cases.

## Cấu trúc Response

### Generic Response Class

```csharp
namespace aRefactor.Domain.Type;

public class Response<T>
{
    public bool Success { get; set; }
    public string MessageKey { get; set; }
    public string Message { get; set; }
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; }
    public T Data { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Các Properties

| Property     | Type       | Mô tả                                             |
| ------------ | ---------- | ------------------------------------------------- |
| `Success`    | `bool`     | `true` nếu request thành công, `false` nếu có lỗi |
| `MessageKey` | `string`   | Key để i18n (internationalization)                |
| `Message`    | `string`   | Message mặc định (tiếng Anh)                      |
| `StatusCode` | `int`      | HTTP status code (200, 404, 500, ...)             |
| `ErrorCode`  | `string`   | Mã lỗi cụ thể cho client xử lý                    |
| `Data`       | `T`        | Dữ liệu trả về (generic type)                     |
| `Timestamp`  | `DateTime` | Thời điểm response được tạo                       |

## Factory Methods

### 1. SuccessResponse - Response Thành Công

```csharp
public static class Response
{
    public static Response<T> SuccessResponse<T>(
        T data,
        string messageKey = "success",
        string message = "Operation completed successfully")
    {
        return new Response<T>
        {
            Success = true,
            MessageKey = messageKey,
            Message = message,
            StatusCode = (int)HttpStatusCode.OK,
            ErrorCode = null,
            Data = data,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

**Ví dụ sử dụng:**

```csharp
// Trả về single object
var pattern = await _service.GetPatternAsync(id);
return Ok(Response<Pattern>.SuccessResponse(
    pattern,
    messageKey: "pattern.retrieved",
    message: "Pattern retrieved successfully"
));

// Response JSON:
{
  "success": true,
  "messageKey": "pattern.retrieved",
  "message": "Pattern retrieved successfully",
  "statusCode": 200,
  "errorCode": null,
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Strategy Pattern",
    "slug": "strategy-pattern",
    "summary": "Define a family of algorithms..."
  },
  "timestamp": "2025-11-13T10:30:00Z"
}
```

```csharp
// Trả về list
var patterns = await _service.GetAllPatternsAsync();
return Ok(Response<List<Pattern>>.SuccessResponse(
    patterns,
    messageKey: "patterns.retrieved",
    message: "All patterns retrieved successfully"
));

// Response JSON:
{
  "success": true,
  "messageKey": "patterns.retrieved",
  "message": "All patterns retrieved successfully",
  "statusCode": 200,
  "errorCode": null,
  "data": [
    { "id": "...", "name": "Strategy Pattern", ... },
    { "id": "...", "name": "Repository Pattern", ... }
  ],
  "timestamp": "2025-11-13T10:30:00Z"
}
```

```csharp
// Created response (201)
var newPattern = await _service.CreatePatternAsync(dto);
return Created(
    $"/api/patterns/{newPattern.Id}",
    Response<Pattern>.SuccessResponse(
        newPattern,
        messageKey: "pattern.created",
        message: "Pattern created successfully"
    )
);
```

### 2. ErrorResponse - Response Lỗi

```csharp
public static class Response
{
    public static Response<object> ErrorResponse(
        string messageKey,
        string message,
        HttpStatusCode statusCode,
        string errorCode)
    {
        return new Response<object>
        {
            Success = false,
            MessageKey = messageKey,
            Message = message,
            StatusCode = (int)statusCode,
            ErrorCode = errorCode,
            Data = null,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

**Ví dụ sử dụng:**

```csharp
// 404 Not Found
throw new NotFoundException(
    Response.PatternNotFound,
    "Pattern not found"
);

// Response JSON:
{
  "success": false,
  "messageKey": "pattern.not_found",
  "message": "Pattern not found",
  "statusCode": 404,
  "errorCode": "NOT_FOUND",
  "data": null,
  "timestamp": "2025-11-13T10:30:00Z"
}
```

```csharp
// 403 Forbidden
throw new ForbiddenException(
    Response.PermissionDenied,
    "You don't have permission to delete this pattern"
);

// Response JSON:
{
  "success": false,
  "messageKey": "error.permission_denied",
  "message": "You don't have permission to delete this pattern",
  "statusCode": 403,
  "errorCode": "FORBIDDEN",
  "data": null,
  "timestamp": "2025-11-13T10:30:00Z"
}
```

### 3. ValidationErrorResponse - Response Lỗi Validation

```csharp
// Special type for validation errors
public class Response<Dictionary<string, string[]>> // ValidationErrorResponse
{
    public bool Success { get; set; }
    public string MessageKey { get; set; }
    public string Message { get; set; }
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; }
    public Dictionary<string, string[]> Data { get; set; } // Field errors
    public DateTime Timestamp { get; set; }
}
```

**Ví dụ sử dụng:**

```csharp
var errors = new Dictionary<string, string[]>
{
    { "Name", new[] { "Name is required", "Name must be between 3 and 100 characters" } },
    { "Slug", new[] { "Slug is required", "Slug must be unique" } }
};

throw new ValidationException(
    Response.ValidationFailed,
    "Validation failed",
    errors
);

// Response JSON:
{
  "success": false,
  "messageKey": "validation.failed",
  "message": "Validation failed",
  "statusCode": 400,
  "errorCode": "VALIDATION_ERROR",
  "data": {
    "name": [
      "Name is required",
      "Name must be between 3 and 100 characters"
    ],
    "slug": [
      "Slug is required",
      "Slug must be unique"
    ]
  },
  "timestamp": "2025-11-13T10:30:00Z"
}
```

## Message Keys - Chuẩn Hóa Message Keys

Sử dụng class `Response` để định nghĩa các message keys chuẩn:

```csharp
namespace aRefactor.Domain.Exception;

public static class Response
{
    // Success messages
    public const string Success = "success";
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";

    // Pattern messages
    public const string PatternNotFound = "pattern.not_found";
    public const string PatternCreated = "pattern.created";
    public const string PatternUpdated = "pattern.updated";
    public const string PatternDeleted = "pattern.deleted";
    public const string PatternAlreadyExists = "pattern.already_exists";

    // Category messages
    public const string CategoryNotFound = "category.not_found";
    public const string CategoryCreated = "category.created";

    // Error messages
    public const string ValidationFailed = "validation.failed";
    public const string Unauthorized = "error.unauthorized";
    public const string PermissionDenied = "error.permission_denied";
    public const string InternalServerError = "error.internal_server_error";
    public const string BadRequest = "error.bad_request";
}
```

**Lợi ích:**

- ✅ Type-safe (compile-time checking)
- ✅ Không bị typo
- ✅ Dễ refactor
- ✅ IntelliSense support

## Sử Dụng Trong Controller

### Pattern 1: Success Response với Data

```csharp
[HttpGet]
public async Task<IActionResult> GetAllPatterns()
{
    var patterns = await _patternService.GetAllPatternsAsync();

    return Ok(Response<List<Pattern>>.SuccessResponse(
        patterns,
        messageKey: Response.Success,
        message: "All patterns retrieved successfully"
    ));
}
```

### Pattern 2: Created Response

```csharp
[HttpPost]
public async Task<IActionResult> CreatePattern([FromBody] CreatePatternDto dto)
{
    var pattern = await _patternService.CreatePatternAsync(dto);

    return CreatedAtAction(
        nameof(GetPattern),
        new { id = pattern.Id },
        Response<Pattern>.SuccessResponse(
            pattern,
            messageKey: Response.PatternCreated,
            message: "Pattern created successfully"
        )
    );
}
```

### Pattern 3: No Content Response

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> DeletePattern(Guid id)
{
    await _patternService.DeletePatternAsync(id);

    return Ok(Response<object>.SuccessResponse(
        null,
        messageKey: Response.PatternDeleted,
        message: "Pattern deleted successfully"
    ));
}
```

### Pattern 4: Custom Success Message

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdatePattern(Guid id, [FromBody] UpdatePatternDto dto)
{
    var pattern = await _patternService.UpdatePatternAsync(id, dto);

    return Ok(Response<Pattern>.SuccessResponse(
        pattern,
        messageKey: Response.PatternUpdated,
        message: $"Pattern '{pattern.Name}' updated successfully"
    ));
}
```

## Internationalization (i18n) Support

### 1. Tạo Resource Files

Tạo file `Resources/Messages.en.json`:

```json
{
  "success": "Operation completed successfully",
  "pattern.not_found": "Pattern not found",
  "pattern.created": "Pattern created successfully",
  "validation.failed": "Validation failed"
}
```

Tạo file `Resources/Messages.vi.json`:

```json
{
  "success": "Thao tác thành công",
  "pattern.not_found": "Không tìm thấy pattern",
  "pattern.created": "Tạo pattern thành công",
  "validation.failed": "Validation thất bại"
}
```

### 2. Sử Dụng i18n Service

```csharp
public class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations;

    public string Translate(string messageKey, string language = "en")
    {
        if (_translations.TryGetValue(language, out var translations) &&
            translations.TryGetValue(messageKey, out var message))
        {
            return message;
        }

        return messageKey; // Fallback to key
    }
}
```

### 3. Trong Controller

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetPattern(Guid id)
{
    var pattern = await _service.GetPatternAsync(id);

    var language = Request.Headers["Accept-Language"].ToString().Split(',')[0];

    return Ok(Response<Pattern>.SuccessResponse(
        pattern,
        messageKey: Response.PatternRetrieved,
        message: _localization.Translate(Response.PatternRetrieved, language)
    ));
}
```

## Client-Side Handling

### TypeScript/JavaScript

```typescript
interface ApiResponse<T> {
  success: boolean;
  messageKey: string;
  message: string;
  statusCode: number;
  errorCode: string | null;
  data: T | null;
  timestamp: string;
}

// Success case
async function getPattern(id: string): Promise<Pattern> {
  const response = await fetch(`/api/patterns/${id}`);
  const result: ApiResponse<Pattern> = await response.json();

  if (result.success) {
    return result.data!;
  } else {
    throw new Error(result.message);
  }
}

// Error handling
async function createPattern(dto: CreatePatternDto): Promise<Pattern> {
  try {
    const response = await fetch("/api/patterns", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(dto),
    });

    const result: ApiResponse<Pattern> = await response.json();

    if (result.success) {
      showSuccess(result.message);
      return result.data!;
    } else {
      if (result.errorCode === "VALIDATION_ERROR") {
        // Handle validation errors
        const errors = result.data as Record<string, string[]>;
        showValidationErrors(errors);
      } else {
        showError(result.message);
      }
      throw new Error(result.message);
    }
  } catch (error) {
    showError("Network error");
    throw error;
  }
}

// Validation error handling
function showValidationErrors(errors: Record<string, string[]>) {
  Object.entries(errors).forEach(([field, messages]) => {
    const inputElement = document.querySelector(`[name="${field}"]`);
    messages.forEach((message) => {
      showFieldError(inputElement, message);
    });
  });
}
```

### React Hook Example

```typescript
function useApi<T>() {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const request = async (url: string, options?: RequestInit) => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(url, options);
      const result: ApiResponse<T> = await response.json();

      if (result.success) {
        setData(result.data);
        return result.data;
      } else {
        setError(result.message);
        throw new Error(result.message);
      }
    } catch (err) {
      setError((err as Error).message);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  return { data, loading, error, request };
}

// Usage
function PatternDetail({ id }: { id: string }) {
  const { data: pattern, loading, error, request } = useApi<Pattern>();

  useEffect(() => {
    request(`/api/patterns/${id}`);
  }, [id]);

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return <div>{pattern?.name}</div>;
}
```

## Best Practices

### 1. Luôn Dùng Factory Methods

```csharp
// ✅ ĐÚNG
return Ok(Response<Pattern>.SuccessResponse(pattern));

// ❌ SAI
return Ok(new Response<Pattern>
{
    Success = true,
    Data = pattern,
    Timestamp = DateTime.UtcNow
    // Thiếu các fields khác
});
```

### 2. Consistent Message Keys

```csharp
// ✅ ĐÚNG - Dùng constants
throw new NotFoundException(Response.PatternNotFound, "Pattern not found");

// ❌ SAI - Hard-coded string
throw new NotFoundException("pattern_not_found", "Pattern not found");
```

### 3. Meaningful Error Codes

```csharp
// ✅ ĐÚNG - Error code rõ ràng
ErrorCode = "PATTERN_SLUG_DUPLICATE"

// ❌ SAI - Error code chung chung
ErrorCode = "ERROR"
```

### 4. Include Timestamp

Timestamp giúp:

- Debug timing issues
- Track request latency
- Audit logs

```csharp
Timestamp = DateTime.UtcNow // Luôn dùng UTC
```

## Testing Response Model

```csharp
[Fact]
public void SuccessResponse_ShouldHaveCorrectStructure()
{
    // Arrange
    var pattern = new Pattern { Id = Guid.NewGuid(), Name = "Strategy" };

    // Act
    var response = Response<Pattern>.SuccessResponse(pattern);

    // Assert
    Assert.True(response.Success);
    Assert.Equal(200, response.StatusCode);
    Assert.Null(response.ErrorCode);
    Assert.NotNull(response.Data);
    Assert.Equal(pattern.Name, response.Data.Name);
}

[Fact]
public void ErrorResponse_ShouldHaveCorrectStructure()
{
    // Act
    var response = Response.ErrorResponse(
        Response.PatternNotFound,
        "Pattern not found",
        HttpStatusCode.NotFound,
        "NOT_FOUND"
    );

    // Assert
    Assert.False(response.Success);
    Assert.Equal(404, response.StatusCode);
    Assert.Equal("NOT_FOUND", response.ErrorCode);
    Assert.Null(response.Data);
}
```

## Kết Luận

Response Model cung cấp:

- ✅ Cấu trúc thống nhất cho tất cả responses
- ✅ Type-safe với generic types
- ✅ Hỗ trợ i18n
- ✅ Dễ dàng xử lý ở client-side
- ✅ Consistent error handling
