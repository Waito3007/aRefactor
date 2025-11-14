# Hướng dẫn sử dụng AutoMapping Profile

Tài liệu này mô tả cách chúng ta cấu hình và khai thác AutoMapper trong dự án `aRefactor`, giúp việc chuyển đổi giữa DTO và domain model trở nên ngắn gọn, dễ bảo trì.

## 1. Mục đích

- Giảm code lặp khi map giữa các lớp `CreateRequestPattern`, `Pattern`, `CreateResponsePattern`, …
- Tập trung toàn bộ cấu hình vào một (hoặc vài) profile để dễ mở rộng.
- Đảm bảo những quy tắc đặc thù (ví dụ: bỏ qua `Id`, chuẩn hoá chuỗi) được áp dụng nhất quán.

## 2. Đăng ký AutoMapper

```csharp
// Program.cs
builder.Services.AddAutoMapper(typeof(AutoMappingProfile));
```

- DI sẽ quét toàn bộ profile nằm cùng assembly với `AutoMappingProfile`.
- Khi cần thêm profile mới, chỉ cần tạo class kế thừa `Profile` trong dự án, không phải sửa `Program.cs`.

## 3. Cấu trúc Profile

```csharp
public class AutoMappingProfile : Profile
{
    public AutoMappingProfile()
    {
        RegisterPatternMappings();
        // TODO: RegisterCategoryMappings(); RegisterImplementationMappings();
    }

    private void RegisterPatternMappings()
    {
        CreateMap<CreateRequestPattern, Pattern>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<Pattern, CreateRequestPattern>();
    }
}
```

- Tách nhỏ từng nhóm mapping vào hàm `Register*` để file luôn gọn.
- Dễ dàng thêm cấu hình mới mà không cần tạo thêm profile riêng lẻ.

## 4. Sử dụng trong service

```csharp
public class PatternService : IPatternService
{
    private readonly IMapper _mapper;
    ...

    public async Task<CreateRequestPattern> CreatePatternAsync(CreateRequestPattern request)
    {
        var pattern = _mapper.Map<Pattern>(request);
        ...
        return _mapper.Map<CreateRequestPattern>(pattern); // khi cần map ngược
    }
}
```

- `_mapper.Map<TDestination>(source)` được inject qua DI.
- Giữ service tập trung vào business logic, không trộn lẫn thao tác copy field.

## 5. Best Practices

1. **Một profile cho một bounded context**  
   - `AutoMappingProfile` có thể chia nhỏ bằng các phương thức con như trên.  
   - Nếu dự án phát triển lớn, có thể bổ sung thêm `CategoryMappingProfile`, `ImplementationMappingProfile`, …

2. **Không map những giá trị được domain quyết định**  
   - Ví dụ `Pattern.Id` được sinh trong service, nên cấu hình `.ForMember(x => x.Id, opt => opt.Ignore())`.

3. **Tận dụng Value Resolvers/Converters khi cần logic phức tạp**  
   - Ví dụ convert markdown sang HTML, chuẩn hoá slug.

4. **Viết test cho mapping quan trọng**  
   - AutoMapper cung cấp `configuration.AssertConfigurationIsValid()` giúp phát hiện thiếu map.

## 6. Kiểm tra cấu hình

Trong các test, có thể thêm:

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<AutoMappingProfile>();
});

config.AssertConfigurationIsValid();
```

- Đảm bảo mọi destination member đều được map hoặc đánh dấu `Ignore`.

---

Bằng việc gom yếu tố mapping vào `AutoMappingProfile`, chúng ta có thể quản lý hàng loạt DTO/domain của nhiều module mà không cần tạo file hướng dẫn riêng cho từng case cụ thể. Chỉ cần bổ sung thêm hàm `Register*` tương ứng là xong.***
