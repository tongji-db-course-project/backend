# RetailManage Backend

商品零售管理系统后端，技术栈为 ASP.NET Core 8、EF Core 8 和 Oracle。

## 本地启动

1. 启动本地 Oracle 容器：`docker start oracle-db`。
2. 确认 `appsettings.json` 中的 `ConnectionStrings:OracleDb` 可连接。
3. 在仓库目录执行 `dotnet restore`。
4. 执行 `dotnet run --launch-profile http`。
5. 打开 `http://localhost:5113/swagger/index.html`。

本地 API 根地址为 `http://localhost:5113/api`。真实连接串和 JWT 密钥放在已忽略的 `appsettings.Development.json`，不得提交到仓库。

测试账号为 `admin`，密码为 `123456`。

## 开发流程

1. 从集成分支 `dev` 创建个人功能分支，例如 `feature/zcx-product-category-api`。
2. 先在 Apifox 确认 API 契约，再实现 Controller、Service 和 DTO。
3. 本地执行 `dotnet format`、`dotnet build` 和 Swagger 自测。
4. 涉及 API 或数据库结构变化时，同步更新 Apifox/OpenAPI 或数据库脚本，并通知相关成员。
5. 前后端联调通过后发起合并请求，经评审后合并到 `dev`。

完整 CRUD 样板和完成清单见 `docs/开发样板说明.md`。

## 提交前检查

```powershell
dotnet format --verify-no-changes
dotnet build --no-restore
git diff --check
```

不要提交 `.vs/`、`bin/`、`obj/`、`*.user` 或本地开发配置。
