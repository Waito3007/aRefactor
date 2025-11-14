using aRefactor.Configuration;
using aRefactor.Lib;
using aRefactor.Lib.Interfacde;
using aRefactor.Repository;
using aRefactor.Repository.Interface;
using aRefactor.Service;
using aRefactor.Service.Interface;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddAuthorization();
// DI
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPatternRepository, PatternRepository>();
builder.Services.AddScoped<IPatternService, PatternService>();
builder.Services.AddAutoMapper(typeof(AutoMappingProfile));

//DB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapControllers();
//app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.Run();
