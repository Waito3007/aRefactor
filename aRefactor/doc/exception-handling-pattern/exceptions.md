# Exception Classes - Hướng dẫn sử dụng chi tiết

## Exception Hierarchy

```
System.Exception
    └── ProjectException (Base)
        ├── ValidationException
        ├── NotFoundException
        ├── UnauthorizedException
        └── ForbiddenException
```

## 1. ProjectException (Base Class)

### Mô tả

Base class cho tất cả custom exceptions trong project. Chứa các properties chung và có thể được sử dụng trực tiếp cho các lỗi custom.

### Properties

| Property     | Type             | Mô tả                                   |
| ------------ | ---------------- | --------------------------------------- |
| `Message`    | `string`         | Error message (từ System.Exception)     |
| `StatusCode` | `HttpStatusCode` | HTTP status code trả về                 |
| `ErrorCode`  | `string`         | Mã lỗi để client phân biệt các loại lỗi |
| `MessageKey` | `Response` enum  | Key để tra cứu message đa ngôn ngữ      |

### Constructors

```csharp
// Constructor 1: Basic
public ProjectException(
    string message,
    HttpStatusCode statusCode = HttpStatusCode.BadRequest,
    string errorCode = null,
    Response messageKey = Response.Success)

// Constructor 2: With InnerException
public ProjectException(
    string message,
    Exception innerException,
    HttpStatusCode statusCode = HttpStatusCode.BadRequest,
    string errorCode = null,
    Response messageKey = Response.Success)
```

### Khi nào sử dụng

✅ Sử dụng khi cần custom exception với status code riêng  
✅ Sử dụng cho business logic errors không fit vào các exception con

### Ví dụ

```csharp
// Ví dụ 1: Custom business error
public async Task PublishPatternAsync(Guid patternId)
{
    var pattern = await _repository.GetByIdAsync(patternId);

    if (pattern.Implementations.Count == 0)
    {
        throw new ProjectException(
            "Không thể publish pattern khi chưa có implementation nào.",
            HttpStatusCode.BadRequest,
            "PATTERN_NO_IMPLEMENTATIONS",
            Response.ValidationError
        );
    }

    pattern.IsPublished = true;
    await _repository.UpdateAsync(pattern);
}

// Ví dụ 2: External API error
public async Task<string> FetchExternalDataAsync(string url)
{
    try
    {
        return await _httpClient.GetStringAsync(url);
    }
    catch (HttpRequestException ex)
    {
        throw new ProjectException(
            "Không thể kết nối đến dịch vụ bên ngoài.",
            ex,  // Inner exception
            HttpStatusCode.BadGateway,
            "EXTERNAL_SERVICE_ERROR",
            Response.InternalServerError
        );
    }
}
```

### Response Example

```json
{
  "success": false,
  "messageKey": "ValidationError",
  "message": "Không thể publish pattern khi chưa có implementation nào.",
  "errorCode": "PATTERN_NO_IMPLEMENTATIONS",
  "statusCode": 400,
  "timestamp": "2025-11-13T10:00:00Z"
}
```

---

## 2. ValidationException

### Mô tả

Exception cho validation errors. Thường được throw khi input data không hợp lệ hoặc vi phạm business rules. Hỗ trợ trả về multiple validation errors cho nhiều fields.

### Properties (kế thừa từ ProjectException)

| Property     | Type                           | Mô tả                                        |
| ------------ | ------------------------------ | -------------------------------------------- |
| `Errors`     | `Dictionary<string, string[]>` | Dictionary chứa validation errors theo field |
| `Message`    | `string`                       | Default: "Du lieu khong hop le."             |
| `StatusCode` | `HttpStatusCode`               | Fixed: 400 Bad Request                       |
| `ErrorCode`  | `string`                       | Fixed: "VALIDATION_ERROR"                    |
| `MessageKey` | `Response` enum                | Fixed: Response.ValidationError              |

### Constructors

```csharp
// Constructor 1: Chỉ có errors dictionary
public ValidationException(Dictionary<string, string[]> errors)

// Constructor 2: Custom message + errors
public ValidationException(
    string message = null,
    Dictionary<string, string[]> errors = null)
```

### Khi nào sử dụng

✅ Input validation fails  
✅ Business rules validation fails  
✅ Cần trả về multiple errors cho multiple fields  
✅ Form validation errors

### Ví dụ

```csharp
// Ví dụ 1: Single field validation
public async Task<Category> CreateAsync(CreateCategoryDto dto)
{
    if (await _repository.ExistsBySlugAsync(dto.Slug))
    {
        throw new ValidationException(new Dictionary<string, string[]>
        {
            ["Slug"] = new[] { "Slug đã tồn tại trong hệ thống." }
        });
    }

    // ... create logic
}

// Ví dụ 2: Multiple fields validation
public async Task<Pattern> CreatePatternAsync(CreatePatternDto dto)
{
    var errors = new Dictionary<string, string[]>();

    // Validate name
    if (string.IsNullOrWhiteSpace(dto.Name))
        errors["Name"] = new[] { "Tên pattern không được để trống." };
    else if (dto.Name.Length > 200)
        errors["Name"] = new[] { "Tên pattern không được vượt quá 200 ký tự." };

    // Validate slug
    if (string.IsNullOrWhiteSpace(dto.Slug))
        errors["Slug"] = new[] { "Slug không được để trống." };
    else if (await _repository.ExistsBySlugAsync(dto.Slug))
        errors["Slug"] = new[] { "Slug đã tồn tại." };

    // Validate category
    if (!await _categoryRepository.ExistsAsync(dto.CategoryId))
        errors["CategoryId"] = new[] { "Category không tồn tại." };

    if (errors.Any())
        throw new ValidationException(errors);

    // ... create logic
}

// Ví dụ 3: FluentValidation integration
public async Task<Category> CreateAsync(CreateCategoryDto dto)
{
    var validator = new CreateCategoryValidator();
    var validationResult = await validator.ValidateAsync(dto);

    if (!validationResult.IsValid)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        throw new ValidationException(errors);
    }

    // ... create logic
}

// Ví dụ 4: Custom validation message
public async Task UpdateAsync(Guid id, UpdateCategoryDto dto)
{
    var errors = new Dictionary<string, string[]>();

    if (dto.Name != null && dto.Name.Length < 3)
        errors["Name"] = new[] { "Tên phải có ít nhất 3 ký tự." };

    if (errors.Any())
    {
        throw new ValidationException(
            "Dữ liệu cập nhật không hợp lệ.",
            errors
        );
    }

    // ... update logic
}
```

### Response Example

```json
{
  "success": false,
  "messageKey": "ValidationError",
  "message": "Du lieu khong hop le.",
  "errorCode": "VALIDATION_ERROR",
  "statusCode": 400,
  "timestamp": "2025-11-13T10:00:00Z",
  "data": {
    "Name": ["Tên pattern không được để trống."],
    "Slug": ["Slug đã tồn tại."],
    "CategoryId": ["Category không tồn tại."]
  }
}
```

---

## 3. NotFoundException

### Mô tả

Exception khi resource không được tìm thấy trong database. Thường được throw khi query by ID không trả về kết quả.

### Properties (kế thừa từ ProjectException)

| Property     | Type             | Mô tả                                                    |
| ------------ | ---------------- | -------------------------------------------------------- |
| `Message`    | `string`         | Custom message hoặc default: "Tai nguyen khong ton tai." |
| `StatusCode` | `HttpStatusCode` | Fixed: 404 Not Found                                     |
| `ErrorCode`  | `string`         | Fixed: "NOT_FOUND"                                       |
| `MessageKey` | `Response` enum  | Fixed: Response.NotFound                                 |

### Constructors

```csharp
// Constructor 1: Default message
public NotFoundException(string message = null)

// Constructor 2: Format message với entity name và ID
public NotFoundException(string entityName, object key)
```

### Khi nào sử dụng

✅ Query by ID không trả về kết quả  
✅ Resource đã bị xóa  
✅ User request resource không tồn tại  
✅ GET, PUT, DELETE operations trên resource không tồn tại

### Ví dụ

```csharp
// Ví dụ 1: Get by ID
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    var category = await _service.GetByIdAsync(id);

    if (category == null)
        throw new NotFoundException("Category", id);

    return Ok(Response<Category>.SuccessResponse(category));
}

// Ví dụ 2: Update non-existent resource
[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, UpdateCategoryDto dto)
{
    var category = await _service.GetByIdAsync(id);

    if (category == null)
        throw new NotFoundException("Category", id);

    await _service.UpdateAsync(id, dto);
    return Ok(Response.SuccessResponse());
}

// Ví dụ 3: Delete non-existent resource
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id)
{
    var exists = await _service.ExistsAsync(id);

    if (!exists)
        throw new NotFoundException("Category", id);

    await _service.DeleteAsync(id);
    return Ok(Response.SuccessResponse());
}

// Ví dụ 4: Custom message
public async Task<Pattern> GetBySlugAsync(string slug)
{
    var pattern = await _repository.GetBySlugAsync(slug);

    if (pattern == null)
        throw new NotFoundException($"Pattern với slug '{slug}' không tồn tại.");

    return pattern;
}

// Ví dụ 5: Nested resource not found
public async Task AddImplementationAsync(Guid patternId, CreateImplementationDto dto)
{
    var pattern = await _patternRepository.GetByIdAsync(patternId);

    if (pattern == null)
        throw new NotFoundException("Pattern", patternId);

    // ... add implementation logic
}
```

### Response Examples

```json
// Constructor với entityName và key
{
  "success": false,
  "messageKey": "NotFound",
  "message": "Category voi ID '123e4567-e89b-12d3-a456-426614174000' khong ton tai.",
  "errorCode": "NOT_FOUND",
  "statusCode": 404,
  "timestamp": "2025-11-13T10:00:00Z"
}

// Constructor với custom message
{
  "success": false,
  "messageKey": "NotFound",
  "message": "Pattern với slug 'singleton' không tồn tại.",
  "errorCode": "NOT_FOUND",
  "statusCode": 404,
  "timestamp": "2025-11-13T10:00:00Z"
}
```

---

## 4. UnauthorizedException

### Mô tả

Exception khi user chưa đăng nhập hoặc token không hợp lệ. Dùng cho authentication errors.

### Properties (kế thừa từ ProjectException)

| Property     | Type             | Mô tả                                                       |
| ------------ | ---------------- | ----------------------------------------------------------- |
| `Message`    | `string`         | Custom message hoặc default: "Ban khong co quyen truy cap." |
| `StatusCode` | `HttpStatusCode` | Fixed: 401 Unauthorized                                     |
| `ErrorCode`  | `string`         | Fixed: "UNAUTHORIZED"                                       |
| `MessageKey` | `Response` enum  | Fixed: Response.Unauthorized                                |

### Constructor

```csharp
public UnauthorizedException(string message = null)
```

### Khi nào sử dụng

✅ User chưa đăng nhập  
✅ JWT token không hợp lệ  
✅ Token đã hết hạn  
✅ Authentication credentials thiếu hoặc sai

### Ví dụ

```csharp
// Ví dụ 1: Check authentication
public async Task<User> GetCurrentUserAsync()
{
    var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (userId == null)
        throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này.");

    return await _userRepository.GetByIdAsync(userId);
}

// Ví dụ 2: Validate JWT token
public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
{
    var principal = _tokenService.ValidateRefreshToken(refreshToken);

    if (principal == null)
        throw new UnauthorizedException("Refresh token không hợp lệ.");

    // ... generate new tokens
}

// Ví dụ 3: Custom authentication logic
public async Task<LoginResponse> LoginAsync(LoginDto dto)
{
    var user = await _userRepository.GetByUsernameAsync(dto.Username);

    if (user == null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        throw new UnauthorizedException("Tên đăng nhập hoặc mật khẩu không đúng.");

    // ... generate tokens
}

// Ví dụ 4: Session expired
public async Task<IActionResult> GetProtectedResource()
{
    if (!User.Identity.IsAuthenticated)
        throw new UnauthorizedException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");

    // ... return resource
}
```

### Response Example

```json
{
  "success": false,
  "messageKey": "Unauthorized",
  "message": "Ban khong co quyen truy cap.",
  "errorCode": "UNAUTHORIZED",
  "statusCode": 401,
  "timestamp": "2025-11-13T10:00:00Z"
}
```

---

## 5. ForbiddenException

### Mô tả

Exception khi user đã đăng nhập nhưng không có quyền truy cập resource. Dùng cho authorization errors.

### Properties (kế thừa từ ProjectException)

| Property     | Type             | Mô tả                                                                      |
| ------------ | ---------------- | -------------------------------------------------------------------------- |
| `Message`    | `string`         | Custom message hoặc default: "Ban khong co quyen thuc hien hanh dong nay." |
| `StatusCode` | `HttpStatusCode` | Fixed: 403 Forbidden                                                       |
| `ErrorCode`  | `string`         | Fixed: "FORBIDDEN"                                                         |
| `MessageKey` | `Response` enum  | Fixed: Response.Forbidden                                                  |

### Constructor

```csharp
public ForbiddenException(string message = null)
```

### Khi nào sử dụng

✅ User đã đăng nhập nhưng không có role/permission  
✅ User cố truy cập resource của user khác  
✅ User cố thực hiện action không được phép  
✅ Authorization policy fails

### Ví dụ

```csharp
// Ví dụ 1: Check role
public async Task DeleteCategoryAsync(Guid id)
{
    if (!User.IsInRole("Admin"))
        throw new ForbiddenException("Chỉ Admin mới có thể xóa Category.");

    await _repository.DeleteAsync(id);
}

// Ví dụ 2: Resource ownership check
public async Task UpdatePatternAsync(Guid id, UpdatePatternDto dto)
{
    var pattern = await _repository.GetByIdAsync(id);

    if (pattern == null)
        throw new NotFoundException("Pattern", id);

    var currentUserId = _currentUserService.GetUserId();

    if (pattern.CreatedBy != currentUserId && !User.IsInRole("Admin"))
        throw new ForbiddenException("Bạn chỉ có thể chỉnh sửa Pattern do bạn tạo.");

    // ... update logic
}

// Ví dụ 3: Feature access check
public async Task<IActionResult> PublishPattern(Guid id)
{
    var user = await _currentUserService.GetCurrentUserAsync();

    if (!user.HasFeature("CanPublishPatterns"))
        throw new ForbiddenException("Tài khoản của bạn không có quyền publish Pattern.");

    // ... publish logic
}

// Ví dụ 4: Permission check
public async Task AssignRoleAsync(Guid userId, string role)
{
    var currentUser = await _currentUserService.GetCurrentUserAsync();

    if (!await _permissionService.HasPermissionAsync(currentUser.Id, "ManageRoles"))
        throw new ForbiddenException("Bạn không có quyền quản lý roles.");

    // ... assign role logic
}

// Ví dụ 5: Subscription/Plan check
public async Task CreateAdvancedPatternAsync(CreatePatternDto dto)
{
    var subscription = await _subscriptionService.GetUserSubscriptionAsync();

    if (subscription.Plan == "Free" && dto.Type == "Advanced")
        throw new ForbiddenException("Gói miễn phí không hỗ trợ tạo Advanced Pattern. Vui lòng nâng cấp.");

    // ... create logic
}
```

### Response Example

```json
{
  "success": false,
  "messageKey": "Forbidden",
  "message": "Ban khong co quyen thuc hien hanh dong nay.",
  "errorCode": "FORBIDDEN",
  "statusCode": 403,
  "timestamp": "2025-11-13T10:00:00Z"
}
```

---

## So sánh 401 vs 403

|                   | 401 Unauthorized                   | 403 Forbidden                            |
| ----------------- | ---------------------------------- | ---------------------------------------- |
| **Ý nghĩa**       | User chưa xác thực                 | User đã xác thực nhưng không có quyền    |
| **Khi nào throw** | Chưa đăng nhập, token không hợp lệ | Đã đăng nhập nhưng thiếu role/permission |
| **Client action** | Redirect to login page             | Show "Access Denied" message             |
| **Có thể retry?** | Có (sau khi login)                 | Không (cần admin cấp quyền)              |

---

## Best Practices

### 1. Throw càng sớm càng tốt

```csharp
// ✅ GOOD: Throw ngay khi phát hiện lỗi
public async Task<Category> GetByIdAsync(Guid id)
{
    var category = await _repository.GetByIdAsync(id);

    if (category == null)
        throw new NotFoundException("Category", id);

    return category;
}

// ❌ BAD: Xử lý thêm logic trước khi throw
public async Task<Category> GetByIdAsync(Guid id)
{
    var category = await _repository.GetByIdAsync(id);
    var relatedData = await _repository.GetRelatedDataAsync(id); // Waste of resources

    if (category == null)
        throw new NotFoundException("Category", id);

    return category;
}
```

### 2. Message phải rõ ràng và hữu ích

```csharp
// ✅ GOOD: Specific message
throw new NotFoundException("Pattern", patternId);
// → "Pattern voi ID '123' khong ton tai."

// ❌ BAD: Generic message
throw new NotFoundException();
// → "Tai nguyen khong ton tai." (không biết resource nào)
```

### 3. Sử dụng đúng exception type

```csharp
// ✅ GOOD: Dùng NotFoundException cho resource not found
var pattern = await _repository.GetByIdAsync(id);
if (pattern == null)
    throw new NotFoundException("Pattern", id);

// ❌ BAD: Dùng ValidationException cho resource not found
var pattern = await _repository.GetByIdAsync(id);
if (pattern == null)
    throw new ValidationException(new Dictionary<string, string[]>
    {
        ["Id"] = new[] { "Pattern not found." }
    });
```

### 4. Validation errors nên nhóm lại

```csharp
// ✅ GOOD: Collect all errors, throw once
var errors = new Dictionary<string, string[]>();

if (string.IsNullOrEmpty(dto.Name))
    errors["Name"] = new[] { "Required." };

if (string.IsNullOrEmpty(dto.Slug))
    errors["Slug"] = new[] { "Required." };

if (errors.Any())
    throw new ValidationException(errors);

// ❌ BAD: Throw multiple times
if (string.IsNullOrEmpty(dto.Name))
    throw new ValidationException(new Dictionary<string, string[]>
    {
        ["Name"] = new[] { "Required." }
    });

if (string.IsNullOrEmpty(dto.Slug))  // Code này sẽ không chạy được
    throw new ValidationException(new Dictionary<string, string[]>
    {
        ["Slug"] = new[] { "Required." }
    });
```

### 5. Không catch và re-throw không cần thiết

```csharp
// ✅ GOOD: Để exception bubble up
public async Task<Category> GetByIdAsync(Guid id)
{
    var category = await _repository.GetByIdAsync(id);

    if (category == null)
        throw new NotFoundException("Category", id);

    return category;
}

// ❌ BAD: Catch và re-throw không thêm giá trị gì
public async Task<Category> GetByIdAsync(Guid id)
{
    try
    {
        var category = await _repository.GetByIdAsync(id);

        if (category == null)
            throw new NotFoundException("Category", id);

        return category;
    }
    catch (NotFoundException)
    {
        throw; // Không cần thiết
    }
}
```
