# معماری CrmSuite

## سبک معماری

Modular Monolith با مرز ماژول‌های صریح. هر ماژول چهار Assembly دارد و فقط از
طریق Application Contract خودش به بیرون سرویس می‌دهد. در فاز اول یک DbContext و
یک دیتابیس مشترک داریم؛ مرز جدول‌ها با پیشوند/نام‌گذاری snake_case و
Configurator مستقل هر ماژول حفظ می‌شود.

```text
CrmSuite.Api (Composition Root)
   ├── Module.Api ───────────────┐
   │      ├── Module.Application ├── Module.Domain
   │      └── Module.Infrastructure
   │                 └── BuildingBlocks.Infrastructure
   └── BuildingBlocks.Application ── BuildingBlocks.Domain
```

قواعد:

1. Domain به EF Core، ASP.NET Core یا کتابخانه خارجی وابسته نیست.
2. Application به Infrastructure وابسته نیست؛ Command/Query و Validatorها اینجا هستند.
3. Infrastructure پیاده‌سازی Repository، Adapterها و EF Mappingها را دارد.
4. Api فقط Controllerهای نازک و Model Binding را دارد؛ منطق در Handler است.
5. ماژول‌ها نباید Domain یا Infrastructure یکدیگر را مستقیم Reference کنند.
6. ارتباط بین ماژول‌ها در فاز اول از طریق Application Contract یا Domain Event است.

## جریان درخواست

```text
HTTP → ExceptionHandlingMiddleware → Authentication → Authorization
     → Controller (thin) → MediatR Command/Query
     → ValidationBehavior → Handler → Domain → DbContext
     → ApiResponse<T> (success/data/message/errors/traceId)
```

- خطاهای دامنه با `DomainException` و خطاهای اعتبارسنجی با `ValidationException`
  به پاسخ استاندارد تبدیل می‌شوند.
- `traceId` از `Activity.Current` یا `HttpContext.TraceIdentifier` گرفته می‌شود و
  در Serilog ثبت می‌شود.
- Command/Queryها در ماژول Application هر دامنه تعریف می‌شوند؛ DTO و Validator
  همراه همان Request هستند.

## ماژول‌ها و مسئولیت‌ها

| ماژول | مسئولیت فاز اول |
|---|---|
| Foundation | AuditLog، Sequence شماره‌گذاری، Jalali helper، تنظیمات مشترک |
| Identity | User/Role/Permission/Department، قفل حساب، Refresh Token |
| Customers | حقیقی/حقوقی، OTP، Reseller، گروه، Tag، مالک مشتری |
| Catalog | گروه سرویس، سرویس، واحد، قیمت نسخه‌دار، Add-on، آیتم دیتاسنتر |
| Orders | سفارش، فروش، وضعیت پرداخت، الزامات سرویس |
| Billing | فاکتور، مالیات/بیمه، Template، PDF، قرارداد |
| Payments | SEP، پرداخت دستی، رسید، تطبیق، Idempotency |
| Wallet | کیف پول مشتری و پورسانت، شارژ/برداشت |
| Commission | قانون، Entry، Schedule، تقسیم چندنفره |
| Tickets | تیکت پورتال/کارمند، دپارتمان، ارجاع، اولویت، اعلان |
| Forms | فرم JSON Schema نسخه‌دار و Submission |
| Notifications | Adapter پیامک/Bale و اعلان درون‌سایتی/SignalR |
| Reports | گزارش مالی، پرداخت مشتری، بدهکاران، پورسانت (Dapper) |
| Settings | شرکت، مالیات، درگاه، دپارتمان، پورسانت، Template |

## تصمیم‌های پایه

- **یک DbContext مشترک**: برای فاز اول سادگی Transaction و Migration مهم‌تر از
  استقلال کامل دیتابیس هر ماژول است. هر ماژول `IModuleModelConfigurator` خود را
  دارد.
- **Permission-based Authorization**: نقش‌ها فقط بسته‌ای از Permissionها هستند.
  PolicyProvider هر Permission را در زمان اجرا به Policy تبدیل می‌کند.
- **JWT کوتاه‌عمر + Refresh Token چرخشی**: Refresh Token در Identity ذخیره و
  Revoke می‌شود. (پیاده‌سازی در ماژول Identity)
- **Idempotency**: برای پرداخت و پورسانت، کلید Idempotency در DB با Unique Index
  ذخیره می‌شود؛ اجرای دوباره همان نتیجه را برمی‌گرداند.
- **Audit**: فقط تغییرات مالی و دسترسی‌ها در AuditLog ثبت می‌شوند؛ جدول آن در
  Foundation تعریف خواهد شد.
- **زمان**: همه Timestampها UTC؛ تبدیل شمسی فقط در DTO/API.
- **فایل‌ها**: Abstraction محلی با امکان جایگزینی MinIO در آینده.
- **گزارش‌ها**: Dapper و Queryهای خواندنی، جدا از EF برای کارایی.

## نقش‌ها

`SuperAdmin`, `SystemAdmin`, `SalesExpert`, `SalesManager`, `FinanceManager`,
`FinanceExpert`, `Support`, `DataCenterSupport`, `ContractsExpert`, `Installer`,
`Customer`, `Reseller`.

نگاشت اولیه در `RolePermissionMap.cs` است و در ماژول Identity به جداول RBAC Seed
می‌شود. `SuperAdmin` تنها نقشی است که Wildcard دارد.

## مسیر تکامل

1. Foundation: Audit، Sequence، Date helpers، Settings.
2. Identity: JWT واقعی، Refresh Token، RBAC، قفل حساب.
3. Customers → Catalog → Orders → Billing → Payments.
4. Wallet → Commission (با تست‌های محاسبه و Idempotency).
5. Tickets → Forms → Notifications → Reports → Settings.
6. Frontend هر ماژول بعد از تثبیت API همان ماژول.
