# ExceptionMiddleware - Middleware Xử Lý Exception

## Mục đích

`ExceptionMiddleware` là middleware tập trung để xử lý tất cả các exception trong ứng dụng ASP.NET Core, đảm bảo response thống nhất và logging đầy đủ.

## Kiến trúc

```
Request → ExceptionMiddleware → Controller → Exception
                ↓
         Catch Exception
                ↓
         Map to Response
                ↓
         Log Error
                ↓
         Return JSON Response
```

## Source Code

```csharp
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        object response;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ValidationErrorResponse
                {
                    Success = false,
                    MessageKey = validationException.MessageKey,
                    Message = validationException.Message,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    ErrorCode = "VALIDATION_ERROR",
                    Data = validationException.Errors,
                    Timestamp = DateTime.UtcNow
                };
                _logger.LogWarning(exception, "Validation exception occurred: {Message}",
                    validationException.Message);
                break;

            case ProjectException projectException:
                context.Response.StatusCode = (int)projectException.StatusCode;
                response = ResponseModel.ErrorResponse(
                    projectException.MessageKey,
                    projectException.Message,
                    projectException.StatusCode,
                    projectException.ErrorCode
                );
                _logger.LogWarning(exception, "Project exception occurred: {Message}",
                    projectException.Message);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = ResponseModel.ErrorResponse(
                    Response.InternalServerError,
                    null,
                    HttpStatusCode.InternalServerError,
                    "INTERNAL_SERVER_ERROR"
                );
                _logger.LogError(exception, "Unhandled exception occurred: {Message}",
                    exception.Message);
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, jsonOptions)
        );
    }
}
```

## Cách Hoạt Động

### 1. Pattern Matching với Switch Expression

Middleware sử dụng **pattern matching** để xác định loại exception và xử lý tương ứng:

```csharp
switch (exception)
{
    case ValidationException validationException:
        // Xử lý validation errors
        break;

    case ProjectException projectException:
        // Xử lý business logic errors
        break;

    default:
        // Xử lý unexpected errors
        break;
}
```

### 2. Logging Phân Cấp

Middleware log các exception với mức độ khác nhau:

- **LogWarning**: Cho ValidationException và ProjectException (lỗi dự kiến)
- **LogError**: Cho unhandled exceptions (lỗi không dự kiến)

```csharp
// Warning - Expected errors
_logger.LogWarning(exception, "Validation exception occurred: {Message}",
    validationException.Message);

// Error - Unexpected errors
_logger.LogError(exception, "Unhandled exception occurred: {Message}",
    exception.Message);
```

### 3. Response Mapping

Mỗi loại exception được map sang response format cụ thể:

#### ValidationException → ValidationErrorResponse

```json
{
  "success": false,
  "messageKey": "validation.failed",
  "message": "Validation failed",
  "statusCode": 400,
  "errorCode": "VALIDATION_ERROR",
  "data": {
    "Email": ["Email is required"],
    "Password": ["Password must be at least 8 characters"]
  },
  "timestamp": "2025-11-13T10:30:00Z"
}
```

#### ProjectException → ErrorResponse

```json
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

#### Unhandled Exception → InternalServerError

```json
{
  "success": false,
  "messageKey": "error.internal_server_error",
  "message": "An unexpected error occurred",
  "statusCode": 500,
  "errorCode": "INTERNAL_SERVER_ERROR",
  "data": null,
  "timestamp": "2025-11-13T10:30:00Z"
}
```

## Đăng Ký Middleware

### Trong Program.cs

```csharp
var app = builder.Build();

// Đăng ký ExceptionMiddleware TRƯỚC các middleware khác
app.UseMiddleware<ExceptionMiddleware>();

// Các middleware khác
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**⚠️ Lưu ý**: ExceptionMiddleware phải được đăng ký **đầu tiên** trong pipeline để catch tất cả exceptions từ các middleware và controllers phía sau.

## Best Practices

### 1. Thứ Tự Đăng Ký Middleware

```csharp
// ✅ ĐÚNG - ExceptionMiddleware ở đầu
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ❌ SAI - ExceptionMiddleware ở sau
app.UseAuthentication();
app.UseMiddleware<ExceptionMiddleware>(); // Sẽ bỏ lỡ exceptions từ Authentication
app.UseAuthorization();
```

### 2. Không Catch Exception Trong Controller

```csharp
// ❌ SAI - Không cần try-catch trong controller
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
        return NotFound(ex.Message); // Middleware đã xử lý rồi!
    }
}

// ✅ ĐÚNG - Để middleware xử lý
[HttpGet("{id}")]
public async Task<IActionResult> GetPattern(Guid id)
{
    var pattern = await _service.GetPatternAsync(id);
    return Ok(Response<Pattern>.SuccessResponse(pattern));
}
```

### 3. Throw Exception Rõ Ràng

```csharp
// ✅ ĐÚNG - Throw custom exception với thông tin đầy đủ
if (pattern == null)
{
    throw new NotFoundException(
        Response.PatternNotFound,
        $"Pattern with ID {id} not found"
    );
}

// ❌ SAI - Throw generic exception
if (pattern == null)
{
    throw new Exception("Not found"); // Sẽ thành 500 Internal Server Error
}
```

## Xử Lý Trường Hợp Đặc Biệt

### 1. Async Exception Handling

Middleware xử lý cả synchronous và asynchronous exceptions:

```csharp
public async Task InvokeAsync(HttpContext context)
{
    try
    {
        await _next(context); // Async operation
    }
    catch (Exception ex) // Catches both sync and async exceptions
    {
        await HandleExceptionAsync(context, ex);
    }
}
```

### 2. Content-Type Header

Middleware luôn set `Content-Type` là `application/json`:

```csharp
context.Response.ContentType = "application/json";
```

### 3. JSON Serialization Options

Sử dụng `camelCase` naming convention:

```csharp
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true // Dễ đọc trong development
};
```

## Testing Middleware

### Unit Test Example

```csharp
public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var logger = new Mock<ILogger<ExceptionMiddleware>>();
        var middleware = new ExceptionMiddleware(
            next: (innerHttpContext) => throw new ValidationException(
                Response.ValidationFailed,
                "Validation failed",
                new Dictionary<string, string[]>
                {
                    { "Email", new[] { "Email is required" } }
                }
            ),
            logger: logger.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var response = JsonSerializer.Deserialize<ValidationErrorResponse>(responseBody);
        Assert.False(response.Success);
        Assert.Equal("VALIDATION_ERROR", response.ErrorCode);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_Returns404()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var logger = new Mock<ILogger<ExceptionMiddleware>>();
        var middleware = new ExceptionMiddleware(
            next: (innerHttpContext) => throw new NotFoundException(
                Response.PatternNotFound,
                "Pattern not found"
            ),
            logger: logger.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var logger = new Mock<ILogger<ExceptionMiddleware>>();
        var middleware = new ExceptionMiddleware(
            next: (innerHttpContext) => throw new InvalidOperationException(
                "Unexpected error"
            ),
            logger: logger.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);

        // Verify error was logged
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ),
            Times.Once
        );
    }
}
```

## Monitoring và Logging

### 1. Structured Logging

```csharp
_logger.LogWarning(exception,
    "Validation exception occurred: {Message} | User: {UserId}",
    validationException.Message,
    context.User?.Identity?.Name
);
```

### 2. Application Insights Integration

```csharp
private async Task HandleExceptionAsync(HttpContext context, Exception exception)
{
    // Track exception in Application Insights
    var telemetryClient = context.RequestServices
        .GetService<TelemetryClient>();

    telemetryClient?.TrackException(exception, new Dictionary<string, string>
    {
        { "UserId", context.User?.Identity?.Name },
        { "Path", context.Request.Path },
        { "Method", context.Request.Method }
    });

    // Continue with normal exception handling...
}
```

## Troubleshooting

### Vấn đề 1: Response không phải JSON

**Nguyên nhân**: Middleware khác đã set Content-Type hoặc write response trước

**Giải pháp**: Đảm bảo ExceptionMiddleware ở đầu pipeline

### Vấn đề 2: Exception không bị catch

**Nguyên nhân**: Exception xảy ra trong middleware đứng trước ExceptionMiddleware

**Giải pháp**: Di chuyển ExceptionMiddleware lên đầu tiên

### Vấn đề 3: Log thiếu thông tin

**Nguyên nhân**: Không sử dụng structured logging

**Giải pháp**: Sử dụng message template với parameters

```csharp
// ❌ SAI
_logger.LogError($"Error: {exception.Message}");

// ✅ ĐÚNG
_logger.LogError(exception, "Error occurred: {Message}", exception.Message);
```

## Kết Luận

ExceptionMiddleware là thành phần core của Exception Handling Pattern, cung cấp:

- ✅ Xử lý exception tập trung
- ✅ Response format thống nhất
- ✅ Logging có cấu trúc
- ✅ Separation of concerns
- ✅ Dễ test và maintain
