# Exception Handling Pattern

## Mục đích

Pattern này cung cấp một cơ chế xử lý exception thống nhất và tập trung cho toàn bộ ứng dụng ASP.NET Core Web API. Thay vì xử lý exception rải rác ở nhiều nơi, pattern này cho phép:

- **Tập trung xử lý**: Tất cả exceptions được bắt và xử lý tại một middleware duy nhất
- **Chuẩn hóa response**: Mọi lỗi đều trả về cùng một format JSON nhất quán
- **Phân loại rõ ràng**: Các exception được phân loại theo mục đích sử dụng (Validation, NotFound, Unauthorized, Forbidden)
- **Quốc tế hóa (i18n)**: Hỗ trợ message key để dễ dàng đa ngôn ngữ
- **Logging tự động**: Tự động log các exceptions với mức độ phù hợp
- **Clean code**: Controller code gọn gàng, chỉ cần throw exception khi có lỗi

## Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────┐
│                    HTTP Request                              │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              ExceptionMiddleware                             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  try {                                                 │ │
│  │      await _next(context);  // Gọi pipeline tiếp theo │ │
│  │  }                                                     │ │
│  │  catch (Exception ex) {                               │ │
│  │      HandleExceptionAsync(context, ex);               │ │
│  │  }                                                     │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    Controllers / Services                    │
│  - Validation error → throw ValidationException             │
│  - Not found → throw NotFoundException                       │
│  - Unauthorized → throw UnauthorizedException                │
│  - Forbidden → throw ForbiddenException                      │
│  - Other errors → throw ProjectException                     │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼ (Exception được throw)
┌─────────────────────────────────────────────────────────────┐
│         ExceptionMiddleware.HandleExceptionAsync             │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  switch (exception)                                    │ │
│  │  {                                                     │ │
│  │      case ValidationException:                        │ │
│  │          → 400 + validation errors                    │ │
│  │      case ProjectException:                           │ │
│  │          → Custom status code + error details         │ │
│  │      default:                                         │ │
│  │          → 500 Internal Server Error                  │ │
│  │  }                                                     │ │
│  └────────────────────────────────────────────────────────┘ │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              JSON Response to Client                         │
│  {                                                           │
│    "success": false,                                         │
│    "messageKey": "NotFound",                                 │
│    "message": "Tai nguyen khong ton tai.",                   │
│    "statusCode": 404,                                        │
│    "errorCode": "NOT_FOUND",                                 │
│    "timestamp": "2025-11-13T10:30:00Z"                       │
│  }                                                           │
└─────────────────────────────────────────────────────────────┘
```

## Cấu trúc thư mục

```
aRefactor/
├── Configuration/
│   └── ExceptionMiddleware.cs          # Middleware bắt và xử lý exceptions
├── Domain/
│   ├── Exception/
│   │   ├── ProjectException.cs         # Base exception class
│   │   ├── ValidationException.cs      # Cho validation errors (400)
│   │   ├── NotFoundException.cs        # Cho resource not found (404)
│   │   ├── UnauthorizedException.cs    # Cho authentication errors (401)
│   │   ├── ForbiddenException.cs       # Cho authorization errors (403)
│   │   └── Response.cs                 # Enum chứa các message keys
│   └── Type/
│       └── Response.cs                 # Response models (generic & non-generic)
```

## Các thành phần chính

### 1. Exception Classes Hierarchy

```
System.Exception
    └── ProjectException (Base)
        ├── ValidationException
        ├── NotFoundException
        ├── UnauthorizedException
        └── ForbiddenException
```

### 2. Response Models

- **`Response`**: Response không có data
- **`Response<T>`**: Response generic có data kiểu T
- **`Response` enum**: Message keys cho i18n

## Chi tiết các tài liệu

1. [**Architecture.md**](./architecture.md) - Kiến trúc chi tiết của pattern
2. [**Exceptions.md**](./exceptions.md) - Hướng dẫn sử dụng các exception classes
3. [**Middleware.md**](./middleware.md) - Cấu hình và hoạt động của ExceptionMiddleware
4. [**Response-Models.md**](./response-models.md) - Cấu trúc và sử dụng Response models
5. [**Examples.md**](./examples.md) - Các ví dụ sử dụng thực tế
6. [**Best-Practices.md**](./best-practices.md) - Best practices và anti-patterns
7. [**Testing.md**](./testing.md) - Hướng dẫn viết unit tests
8. [**Migration-Guide.md**](./migration-guide.md) - Hướng dẫn áp dụng vào dự án hiện có

## Quick Start

### Bước 1: Đăng ký Middleware trong Program.cs

```csharp
var app = builder.Build();

// Đăng ký ExceptionMiddleware (phải đặt ở đầu pipeline)
app.UseMiddleware<ExceptionMiddleware>();

// Các middleware khác...
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### Bước 2: Sử dụng trong Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);

        // Không cần try-catch, throw exception trực tiếp
        if (product == null)
            throw new NotFoundException("Product", id);

        return Ok(Response<Product>.SuccessResponse(product));
    }
}
```

### Bước 3: Response từ API

**Success Response:**

```json
{
  "success": true,
  "messageKey": "Success",
  "message": "Thanh cong.",
  "errorCode": null,
  "statusCode": 200,
  "timestamp": "2025-11-13T10:30:00Z",
  "data": {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Product Name"
  }
}
```

**Error Response:**

```json
{
  "success": false,
  "messageKey": "NotFound",
  "message": "Product voi ID '123' khong ton tai.",
  "errorCode": "NOT_FOUND",
  "statusCode": 404,
  "timestamp": "2025-11-13T10:30:00Z"
}
```

## Lợi ích

✅ **Consistency**: Tất cả API responses có cùng format  
✅ **Maintainability**: Thay đổi exception handling logic ở một nơi  
✅ **Clean Code**: Controllers không bị làm rối bởi try-catch blocks  
✅ **Logging**: Tự động log tất cả exceptions với context đầy đủ  
✅ **i18n Ready**: Dễ dàng hỗ trợ đa ngôn ngữ qua message keys  
✅ **Type Safety**: Strongly-typed responses với generics  
✅ **Developer Experience**: Dễ sử dụng và mở rộng

## Khi nào sử dụng pattern này?

✅ **Nên sử dụng khi:**

- Xây dựng RESTful API với ASP.NET Core
- Cần chuẩn hóa error responses
- Có yêu cầu đa ngôn ngữ (i18n)
- Team có nhiều developers (cần consistency)
- Muốn tách biệt error handling khỏi business logic

❌ **Không nên sử dụng khi:**

- Ứng dụng rất nhỏ, chỉ có vài endpoints
- Cần custom error handling logic phức tạp cho từng endpoint
- Performance là mối quan tâm tối ưu nhất (middleware có overhead nhỏ)

## Tài liệu tham khảo

- [ASP.NET Core Middleware Documentation](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/)
- [Exception Handling Best Practices](https://docs.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions)
- [RESTful API Error Handling](https://www.rfc-editor.org/rfc/rfc7807)
