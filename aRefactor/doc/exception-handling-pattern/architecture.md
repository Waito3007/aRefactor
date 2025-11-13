# Kiến trúc Exception Handling Pattern

## Tổng quan kiến trúc

Pattern này được xây dựng dựa trên **Middleware Pattern** của ASP.NET Core, kết hợp với **Custom Exception Hierarchy** và **Standardized Response Model**.

## Các layer trong kiến trúc

### 1. Presentation Layer (API Controllers)

**Trách nhiệm:**

- Nhận HTTP requests
- Validate input (basic validation)
- Gọi business logic
- **Throw exceptions** khi có lỗi
- Return standardized responses

**Không làm:**

- ❌ Không xử lý exceptions bằng try-catch
- ❌ Không tạo error responses thủ công
- ❌ Không log exceptions (middleware sẽ làm)

```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Không cần try-catch, để middleware xử lý
        var category = await _categoryService.GetByIdAsync(id);

        if (category == null)
            throw new NotFoundException("Category", id);

        return Ok(Response<Category>.SuccessResponse(category));
    }
}
```

### 2. Business Logic Layer (Services)

**Trách nhiệm:**

- Thực thi business logic
- Validate business rules
- **Throw exceptions** khi vi phạm business rules
- Return domain objects

```csharp
public class CategoryService : ICategoryService
{
    public async Task<Category> CreateAsync(CreateCategoryDto dto)
    {
        // Business validation
        if (await _repository.ExistsBySlugAsync(dto.Slug))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Slug"] = new[] { "Slug đã tồn tại trong hệ thống." }
            });
        }

        var category = _mapper.Map<Category>(dto);
        await _repository.AddAsync(category);

        return category;
    }
}
```

### 3. Exception Layer (Domain/Exception)

**Trách nhiệm:**

- Định nghĩa các exception types
- Chứa metadata (StatusCode, ErrorCode, MessageKey)
- Không chứa logic xử lý

```csharp
// Hierarchy
System.Exception
    └── ProjectException
        ├── ValidationException      // 400 Bad Request
        ├── NotFoundException         // 404 Not Found
        ├── UnauthorizedException     // 401 Unauthorized
        └── ForbiddenException        // 403 Forbidden
```

### 4. Middleware Layer (ExceptionMiddleware)

**Trách nhiệm:**

- Bắt TẤT CẢ exceptions từ pipeline
- Phân loại và xử lý từng loại exception
- Tạo standardized error response
- Log exceptions
- Set HTTP status code
- Return JSON response

```csharp
public class ExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Cho phép request đi qua pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Bắt và xử lý mọi exception
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 1. Set content type
        context.Response.ContentType = "application/json";

        // 2. Tạo response object dựa trên exception type
        object response = exception switch
        {
            ValidationException ve => CreateValidationResponse(ve),
            ProjectException pe => CreateProjectExceptionResponse(pe),
            _ => CreateInternalServerErrorResponse()
        };

        // 3. Set status code
        context.Response.StatusCode = GetStatusCode(exception);

        // 4. Log exception
        LogException(exception);

        // 5. Write JSON response
        await WriteJsonResponse(context, response);
    }
}
```

### 5. Response Layer (Domain/Type)

**Trách nhiệm:**

- Định nghĩa cấu trúc response
- Factory methods để tạo success/error responses
- Generic support cho typed data

```csharp
// Non-generic response (không có data)
public class Response
{
    public bool Success { get; set; }
    public ResponseMessage MessageKey { get; set; }
    public string Message { get; set; }
    public string? ErrorCode { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
}

// Generic response (có data)
public class Response<T> : Response
{
    public T? Data { get; set; }
}
```

## Data Flow

### Success Flow

```
1. Client Request
   ↓
2. ExceptionMiddleware (pass through)
   ↓
3. Controller → Service
   ↓
4. Service returns data
   ↓
5. Controller returns Response<T>.SuccessResponse(data)
   ↓
6. HTTP 200 + JSON Response
```

**Example:**

```http
GET /api/categories/123

Response: 200 OK
{
  "success": true,
  "messageKey": "Success",
  "message": "Thanh cong.",
  "statusCode": 200,
  "timestamp": "2025-11-13T10:00:00Z",
  "data": {
    "id": "123",
    "name": "Creational"
  }
}
```

### Error Flow - NotFoundException

```
1. Client Request
   ↓
2. ExceptionMiddleware (pass through)
   ↓
3. Controller → Service
   ↓
4. Service: data not found
   ↓
5. Service throws NotFoundException
   ↓
6. Exception bubbles up to ExceptionMiddleware
   ↓
7. Middleware catches exception
   ↓
8. Middleware creates error response
   ↓
9. HTTP 404 + JSON Response
```

**Example:**

```http
GET /api/categories/999

Response: 404 Not Found
{
  "success": false,
  "messageKey": "NotFound",
  "message": "Category voi ID '999' khong ton tai.",
  "statusCode": 404,
  "errorCode": "NOT_FOUND",
  "timestamp": "2025-11-13T10:00:00Z"
}
```

### Error Flow - ValidationException

```
1. Client sends invalid data
   ↓
2. ExceptionMiddleware (pass through)
   ↓
3. Controller → Service
   ↓
4. Service validates business rules
   ↓
5. Service throws ValidationException with error dictionary
   ↓
6. Exception bubbles up to ExceptionMiddleware
   ↓
7. Middleware catches ValidationException
   ↓
8. Middleware creates error response with validation errors
   ↓
9. HTTP 400 + JSON Response with error details
```

**Example:**

```http
POST /api/categories
{
  "name": "",
  "slug": "existing-slug"
}

Response: 400 Bad Request
{
  "success": false,
  "messageKey": "ValidationError",
  "message": "Du lieu khong hop le.",
  "statusCode": 400,
  "errorCode": "VALIDATION_ERROR",
  "timestamp": "2025-11-13T10:00:00Z",
  "data": {
    "Name": ["Ten khong duoc de trong."],
    "Slug": ["Slug da ton tai trong he thong."]
  }
}
```

### Error Flow - Unhandled Exception

```
1. Client Request
   ↓
2. ExceptionMiddleware (pass through)
   ↓
3. Controller → Service
   ↓
4. Unexpected error (e.g., DB connection lost, NullReferenceException)
   ↓
5. Exception bubbles up to ExceptionMiddleware
   ↓
6. Middleware catches generic Exception
   ↓
7. Middleware logs error with stack trace
   ↓
8. Middleware creates generic error response (no details)
   ↓
9. HTTP 500 + Generic JSON Response
```

**Example:**

```http
GET /api/categories/123

Response: 500 Internal Server Error
{
  "success": false,
  "messageKey": "InternalServerError",
  "message": "Da xay ra loi khong mong muon. Vui long thu lai sau.",
  "statusCode": 500,
  "errorCode": "INTERNAL_SERVER_ERROR",
  "timestamp": "2025-11-13T10:00:00Z"
}
```

## Middleware Pipeline Order

⚠️ **QUAN TRỌNG**: ExceptionMiddleware phải được đăng ký **ĐẦU TIÊN** trong pipeline để bắt được tất cả exceptions.

```csharp
var app = builder.Build();

// ✅ ĐÚNG: ExceptionMiddleware ở đầu
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ❌ SAI: ExceptionMiddleware ở giữa/cuối
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<ExceptionMiddleware>();  // Sẽ không bắt được lỗi từ Authentication
app.UseAuthorization();
app.MapControllers();
```

## Design Decisions

### Tại sao dùng Middleware thay vì Exception Filter?

| Middleware                                                  | Exception Filter                                |
| ----------------------------------------------------------- | ----------------------------------------------- |
| ✅ Bắt được exceptions từ TẤT CẢ middleware                 | ❌ Chỉ bắt exceptions từ controllers            |
| ✅ Có thể xử lý exceptions từ authentication, authorization | ❌ Không bắt được exceptions từ middleware khác |
| ✅ Flexibility cao hơn                                      | ❌ Limited scope                                |
| ✅ Centralized error handling                               | ❌ Cần register ở nhiều nơi                     |

### Tại sao dùng Custom Exceptions?

- **Type Safety**: Compiler kiểm tra exception types
- **Metadata**: Mỗi exception mang theo StatusCode, ErrorCode, MessageKey
- **Semantic**: Tên exception thể hiện rõ ý nghĩa (NotFoundException vs Exception)
- **Maintainability**: Dễ extend và customize
- **Testability**: Dễ test các exception scenarios

### Tại sao dùng Response<T> Generic?

- **Type Safety**: Compile-time type checking
- **Intellisense**: IDE hỗ trợ autocomplete
- **Reusability**: Một class cho nhiều data types
- **Consistency**: Cùng structure cho mọi responses

## Extensibility

### Thêm Custom Exception mới

```csharp
// 1. Tạo exception class
public class ConflictException : ProjectException
{
    public ConflictException(string message = null)
        : base(
            message ?? Response.Conflict.GetDescriptionOfEnum(),
            HttpStatusCode.Conflict,
            "CONFLICT",
            Response.Conflict)
    {
    }
}

// 2. Thêm message key vào enum
public enum Response
{
    // ... existing values

    [Description("Xung đột dữ liệu.")]
    Conflict = 6
}

// 3. Thêm case vào ExceptionMiddleware (optional nếu dùng ProjectException base)
case ConflictException conflictException:
    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
    response = ResponseModel.ErrorResponse(
        conflictException.MessageKey,
        conflictException.Message,
        conflictException.StatusCode,
        conflictException.ErrorCode
    );
    break;
```

### Thêm Custom Logging

```csharp
private async Task HandleExceptionAsync(HttpContext context, Exception exception)
{
    // ... existing code

    // Custom logging logic
    if (exception is ProjectException projectException)
    {
        _logger.LogWarning(exception,
            "Project exception: {ErrorCode} - {Message}",
            projectException.ErrorCode,
            projectException.Message);

        // Send to external logging service
        await _errorTrackingService.TrackAsync(projectException);
    }

    // ... rest of the code
}
```

### Thêm Custom Response Headers

```csharp
private async Task HandleExceptionAsync(HttpContext context, Exception exception)
{
    context.Response.ContentType = "application/json";

    // Add custom headers
    context.Response.Headers.Add("X-Error-Code", GetErrorCode(exception));
    context.Response.Headers.Add("X-Request-Id", context.TraceIdentifier);

    // ... rest of the code
}
```

## Performance Considerations

### Exception Throwing Overhead

- Throwing exceptions có cost (stack trace generation)
- **Không nên** dùng exceptions cho flow control
- **Chỉ nên** dùng cho exceptional cases (thực sự là lỗi)

```csharp
// ❌ BAD: Dùng exception cho flow control
public Category GetBySlug(string slug)
{
    try
    {
        return _categories.First(c => c.Slug == slug);
    }
    catch (InvalidOperationException)
    {
        throw new NotFoundException("Category", slug);
    }
}

// ✅ GOOD: Check trước khi throw
public Category GetBySlug(string slug)
{
    var category = _categories.FirstOrDefault(c => c.Slug == slug);

    if (category == null)
        throw new NotFoundException("Category", slug);

    return category;
}
```

### Middleware Order Impact

- Middleware được execute theo thứ tự đăng ký
- ExceptionMiddleware ở đầu → bắt được nhiều exceptions hơn
- Trade-off: Một chút overhead cho mọi requests

## Security Considerations

### Không expose sensitive information

```csharp
// ❌ BAD: Expose stack trace ra ngoài
default:
    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
    response = new Response
    {
        Success = false,
        Message = exception.ToString(), // NGUY HIỂM!
        StatusCode = 500
    };
    break;

// ✅ GOOD: Generic message cho unhandled exceptions
default:
    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
    response = ResponseModel.ErrorResponse(
        Response.InternalServerError,
        null, // Không expose chi tiết
        HttpStatusCode.InternalServerError,
        "INTERNAL_SERVER_ERROR"
    );
    _logger.LogError(exception, "Unhandled exception"); // Log ở server
    break;
```

### Validate input before processing

```csharp
// Validation exception có thể expose field names
// Đảm bảo field names không chứa sensitive info
public class LoginDto
{
    public string Username { get; set; }  // ✅ OK to expose
    public string Password { get; set; }  // ❌ Không nên expose trong validation error
}

// Validation
if (string.IsNullOrEmpty(dto.Password))
{
    throw new ValidationException(new Dictionary<string, string[]>
    {
        ["Password"] = new[] { "Password is required." }  // OK - không expose giá trị
    });
}
```
