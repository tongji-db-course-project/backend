using backend.Data;
using Microsoft.EntityFrameworkCore;
using backend; // 按你的命名空间调整

var builder = WebApplication.CreateBuilder(args);

// 1. 注册 DbContext，连接串从 appsettings.json 读
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb")));

// 2. 控制器
builder.Services.AddControllers();

// 3. Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();