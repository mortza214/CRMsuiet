# CrmSuite — سیستم مدیریت مشتریان، فروش و پشتیبانی

CrmSuite یک سامانه ماژولار برای مدیریت چرخه کامل فروش خدمات اینترنت و دیتاسنتر است: مشتریان، سفارش‌ها، فاکتورها، پرداخت‌ها، کیف پول، پورسانت، تیکت‌ها، گزارش‌ها و پورتال مشتری.

- **Backend:** ASP.NET Core 8، EF Core 8، Dapper، MediatR، FluentValidation، JWT + Refresh Token، RBAC، Hangfire، Serilog و QuestPDF
- **Database:** MySQL 8 (سازگار با MariaDB نسخه‌های اخیر برای توسعه محلی)
- **Frontend:** React 18، TypeScript، Vite، Ant Design RTL، React Query، Zustand و Recharts
- **Portal:** ورود مشتری با OTP و رابط کاملاً جدا از پنل کارکنان

---

## وضعیت پروژه

همه ماژول‌های اصلی بک‌اند و فرانت‌اند پیاده‌سازی شده‌اند:

- [x] Foundation، Identity، Customers، Catalog
- [x] Orders، Billing، Payments
- [x] Wallet و Commission
- [x] Tickets، Forms، Notifications
- [x] Reports و Settings
- [x] Workflow Engine (تعریف، انتشار، Instance، کارتابل و Transition)
- [x] پنل کارکنان با تمام صفحات مدیریتی
- [x] پورتال مشتری با OTP
- [x] تست Unit، تست E2E مرورگری و Build نهایی

---

## پیش‌نیازها

| ابزار | نسخه پیشنهادی | توضیح |
|---|---|---|
| .NET SDK | 8.0.x | برای اجرای API و تست‌ها |
| MySQL | 8.0+ | دیتابیس اصلی؛ در محیط توسعه MariaDB 10.11 نیز تست شده است |
| Node.js | 20+ | برای Frontend |
| pnpm | 9+ | مدیریت پکیج Frontend |
| Git | اختیاری | برای نگهداری سورس |

اگر `dotnet` روی `PATH` نیست، مسیر نصب محلی را اضافه کنید:

```powershell
$env:Path = "C:\Users\<USER>\.dotnet8;$env:Path"
```

---

## اجرای سریع

### ۱. آماده‌سازی دیتابیس

```sql
CREATE DATABASE crm_suite
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_persian_ci;
```

### ۲. اجرای Backend

```powershell
cd "D:\deepseek project"

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__MySql = "Server=127.0.0.1;Port=3306;Database=crm_suite;User=root;CharSet=utf8mb4;AllowUserVariables=True;UseAffectedRows=False;"
$env:Hangfire__Enabled = "true"
$env:HealthChecks__MySql__Enabled = "true"

dotnet restore
dotnet run --project src/Hosts/CrmSuite.Api --urls "http://127.0.0.1:5080"
```

آدرس‌ها:

- API: `http://127.0.0.1:5080`
- Swagger: `http://127.0.0.1:5080/swagger`
- Health: `http://127.0.0.1:5080/health`

در اولین اجرا، جداول و داده‌های اولیه شامل Permissionها، نقش‌ها و کاربر `superadmin` ساخته می‌شوند.

### ۳. اجرای Frontend

```powershell
cd "D:\deepseek project\frontend"
pnpm install
Copy-Item .env.example .env
pnpm dev
```

آدرس پنل کارکنان:

```text
http://127.0.0.1:5173
```

آدرس پورتال مشتری:

```text
http://127.0.0.1:5173/portal/login
```

با `npm` نیز می‌توانید اجرا کنید:

```powershell
npm install
npm run dev
```

### ۴. ورود پیش‌فرض

```text
username: superadmin
password: Admin@123
```

> رمز پیش‌فرض فقط برای Development است. قبل از استقرار، رمز را تغییر دهید و `Identity:SuperAdminPassword` را از محیط امن تنظیم کنید.

---

## تنظیمات محیطی

### Backend

| کلید | نمونه | توضیح |
|---|---|---|
| `ConnectionStrings__MySql` | `Server=127.0.0.1;...` | اتصال دیتابیس |
| `ASPNETCORE_URLS` | `http://127.0.0.1:5080` | آدرس Listen |
| `Jwt__Key` | کلید حداقل ۳۲ بایتی | کلید امضای توکن |
| `Jwt__Issuer` / `Jwt__Audience` | `CrmSuite` | تنظیمات JWT |
| `Cors__AllowedOrigins__0` | `http://127.0.0.1:5173` | Origin مجاز Frontend |
| `Identity__SeedOnStartup` | `true` / `false` | فعال‌سازی Seed اولیه |
| `Identity__SuperAdminPassword` | رمز قوی | رمز اولیه SuperAdmin در Production |
| `Hangfire__Enabled` | `true` | فعال‌سازی Jobها |
| `HealthChecks__MySql__Enabled` | `true` | Health Check دیتابیس |
| `Saman__IsSandbox` | `true` / `false` | حالت Sandbox درگاه سامان |
| `Settings__EncryptionKey` | کلید AES | رمزنگاری Merchant/Terminal |
| `Otp__TtlSeconds` | `120` | مدت اعتبار OTP |
| `Sms__Ippanel__Enabled` | `false` / `true` | فعال‌سازی ارسال واقعی Pattern |
| `Sms__Ippanel__ApiKey` | API Key | فقط Environment/User Secrets |
| `Sms__Ippanel__FromNumber` | `+98...` | شماره فرستنده Pattern |
| `Sms__Ippanel__PatternCode` | `PATTERN_CODE` | کد پترن پنل IPPanel |
| `Sms__Ippanel__BaseUrl` | `https://edge.ippanel.com/v1/api/send` | آدرس کامل Endpoint |
| `Sms__Ippanel__PatternParamName` | `code` | نام متغیر پترن |

کلیدهای حساس را در User Secrets یا Environment Variable نگه دارید و در `appsettings.json` قرار ندهید.

### IPPanel Pattern

تنظیمات IPPanel از دو منبع خوانده می‌شود و اولویت زیر همیشه برقرار است:

**Environment Variable > Database (Settings) > پیش‌فرض کد**

- در Development مقدار پیش‌فرض `false` است و OTP در Serilog ثبت می‌شود.
- از مسیر **تنظیمات → اتصال پیامک** می‌توان IPPanel را فعال، تنظیم و تست کرد.
- ApiKey در دیتابیس با AES و همان `Settings:EncryptionKey` ذخیره می‌شود و در پاسخ API فقط به‌صورت `******1234` برمی‌گردد.
- تنظیمات کامل از Environment (برای CI/CD و Production):

```powershell
$env:Sms__Ippanel__Enabled = "true"
$env:Sms__Ippanel__ApiKey = "<ippanel-api-key>"
$env:Sms__Ippanel__FromNumber = "+98..."
$env:Sms__Ippanel__PatternCode = "<pattern-code>"
$env:Sms__Ippanel__PatternParamName = "code"
```

نام‌های قدیمی `Ippanel__ApiKey` و `Ippanel__FromNumber` هم برای سازگاری خوانده می‌شوند؛ اما نام‌های `Sms__Ippanel__*` اولویت دارند.

فقط Endpoint `POST https://edge.ippanel.com/v1/api/send` با `sending_type: "pattern"` و **دقیقاً یک گیرنده** استفاده می‌شود. نام کلید `params` از `PatternParamName` خوانده می‌شود و باید با متغیر پترن پنل IPPanel یکی باشد. هر درخواست OTP یک `CorrelationId` دارد؛ تکرار همان CorrelationId باعث ارسال دوباره SMS نمی‌شود.

#### Settings API

| Endpoint | Permission |
|---|---|
| `GET /api/settings/sms/ippanel` | `Settings.Sms.Read` |
| `PUT /api/settings/sms/ippanel` | `Settings.Sms.Manage` |
| `POST /api/settings/sms/ippanel/test` | `Settings.Sms.Manage` |

### Frontend

فایل `frontend/.env`:

```env
VITE_API_BASE_URL=/api
VITE_PROXY_TARGET=http://127.0.0.1:5080
```

Vite مسیرهای `/api`، `/swagger`، `/health` و `/hubs` را به Backend پروکسی می‌کند.

---

## ماژول‌ها و مسیرهای اصلی

### پنل کارکنان

| بخش | مسیر |
|---|---|
| ورود | `/login` |
| داشبورد | `/dashboard` |
| مشتریان | `/customers` |
| ثبت سرویس جدید مشتری | `/customers/:id/new-service` |
| سفارش‌ها | `/orders` |
| صدور سفارش | `/orders/create` |
| فاکتورها | `/invoices` |
| پرداخت‌ها | `/payments` |
| کیف پول | `/wallet` |
| مدیریت پورسانت | `/commissions` |
| پورسانت‌های من | `/my-commissions` |
| تیکت‌ها | `/tickets` |
| گزارش‌ها | `/reports` |
| تنظیمات | `/settings` |
| تعاریف گردش کار | `/workflow/definitions` |
| ویرایشگر گردش کار | `/workflow/definitions/:id` |
| کارتابل گردش کار | `/workflow/inbox` |
| نمونه گردش کار | `/workflow/instances/:id` |
| تعاریف فرم | `/forms/templates` |
| ویرایشگر فرم | `/forms/templates/:id` |
| پاسخ‌های فرم | `/forms/submissions` |
| کاتالوگ سرویس‌ها | `/catalog/services` |
| ویرایشگر سرویس | `/catalog/services/:id` |
| قیمت‌های تاریخ‌دار | `/catalog/prices` |
| آیتم‌های جانبی | `/catalog/addons` |

### پورتال مشتری

| بخش | مسیر |
|---|---|
| ورود OTP | `/portal/login` |
| داشبورد | `/portal/dashboard` |
| سفارش‌ها | `/portal/orders` |
| سرویس‌های من | `/portal/services` |
| فاکتورها | `/portal/invoices` |
| تیکت‌ها | `/portal/tickets` |
| کیف پول | `/portal/wallet` |
| پروفایل | `/portal/profile` |

---

## قابلیت‌ها

### مشتریان
- مشتری حقیقی و حقوقی
- گروه‌بندی، برچسب، ریسلر و زیرمجموعه
- فیلتر بدهکاران
- کاردکس و سابقه مالی

### فروش
- ثبت سفارش چندمرحله‌ای
- انتخاب سرویس از Catalog
- محاسبه قیمت در Backend
- تخفیف، تغییر وضعیت و تاریخچه
- ثبت سرویس جدید از پروفایل مشتری با فرم داینامیک AttributeSchema و نوع همکاری
- انتخاب خودکار Workflow بر اساس بستر (وایرلس با امکان‌سنجی / فیبر و کابل بدون امکان‌سنجی)
- فرم‌های FeasibilityResult، InstallationSpecs و InstallationReport از ماژول Forms و متصل به FormCode مراحل
- چرخه کامل امکان‌سنجی، بررسی فنی، تأمین تجهیزات، پیش‌فاکتور، تأیید مشتری، نصب، فاکتور نهایی و فعال‌سازی
- پورسانت Pending فقط پس از فعال‌سازی و تأیید مالی
- لیست سرویس‌های فعال مشتری، وضعیت درخواست‌ها و فاکتور دوره‌ای از Snapshot
- نمایش درخواست‌های سرویس و مرحله فعلی Workflow در پروفایل مشتری و پورتال
- نمایش درخواست‌های اخیر و مرحله آن‌ها در داشبورد فروش
- نمایش مشخصات کامل درخواست (مشتری، پهنای باند، بستر و مشخصات فنی) در صفحه Workflow
- زمان‌بندی GenerateRecurringInvoiceJob با Hangfire
- ActionKey برای هر مرحله Workflow و اجرای اکشن واقعی از کارتابل
- فرم داینامیک، پیش‌فاکتور، پرداخت، فاکتور نهایی و فعال‌سازی در کارتابل
- مرحله تصمیم مراجعه حضوری با فرم مستقل و ارجاع تسک به کارشناس فنی انتخاب‌شده
- موتور پورسانت ماهانه (۱/۱۲) با CommissionCalculator و CommissionShare
- چهار حالت تخصیص سهم: مالک، شرکت-معرفی‌شده، مشتری شرکتی و پیگیری
- کارتابل مالی برای تأیید/رد/ویرایش سهم‌ها و شارژ کیف پول
- پرداخت خودکار ۱۱ قسط باقیمانده با بررسی وضعیت سرویس
- صفحه پورسانت‌های من با نام مشتری و سرویس

### فاکتور و پرداخت
- صدور فاکتور رسمی/غیررسمی
- مالیات و بیمه از Settings
- PDF فاکتور
- پرداخت آنلاین سامان
- پرداخت دستی و تأیید مالی
- پرداخت از کیف پول

### کیف پول و پورسانت
- کیف پول مشتری و کارمند
- شارژ کیف پول مشتری
- درخواست برداشت
- قوانین پورسانت و محاسبه خودکار
- تأیید/رد پورسانت

### تیکت‌ها
- ثبت تیکت توسط کارمند و مشتری
- گفتگو، وضعیت و ارجاع
- یادداشت داخلی فقط برای کارکنان
- به‌روزرسانی دوره‌ای در Frontend

### گزارش‌ها
- خلاصه فروش، دریافتی و بدهی
- روند ماهانه
- بدهکاران و بدحساب‌ها
- کاردکس مشتری
- فروش به تفکیک دسته

### تنظیمات
- کاربران، نقش‌ها و Permissionها
- مالیات و بیمه
- درگاه سامان (رمزنگاری‌شده)
- اتصال پیامک IPPanel با ApiKey رمزنگاری‌شده، ماسک و تست ارسال
- اطلاعات شرکت
- قوانین مالیاتی

### Catalog
- PricingMode: UnitBased / Fixed / Additive و BillingPeriod
- CommissionPercent و AttributeSchemaJson برای فیلدهای داینامیک
- قیمت‌های نسخه‌دار با بسته‌شدن اتمیک قیمت قبلی
- Addon عمومی و اختصاصی با Snapshot قیمت
- اتصال چندفرمی به سرویس با الزام و ترتیب
- Snapshot کامل آیتم سفارش: قیمت، Addonها و Attributes
- Override قیمت با Permission و Reason و Audit
- بررسی فرم‌های اجباری پیش از نهایی‌سازی سفارش
- قفل ویرایش سرویس دارای سفارش فعال

### Forms
- تعریف فرم با Code پایدار و نسخه‌بندی Draft/Published/Archived
- فیلدساز کامل با نوع، الزام، گزینه‌ها، Regex و VisibleIf
- Publish با بازسازی FormField از SchemaJson
- پاسخ‌ها با EntityType/EntityId و سازگاری CustomerId/OrderId
- FormSubmissionItem برای گزارش و جستجو
- بررسی پاسخ (تأیید/رد) و اتصال FormCode به Workflow
- پشتیبانی از Schema قدیمی JSON Schema برای مصرف‌کننده‌های قبلی

### Workflow
- تعریف نسخه‌دار با State/Transition
- Publish با اعتبارسنجی Start/End/Reachability
- Instance قفل‌شده به DefinitionVersion
- Transition با Guard، Comment و IdempotencyKey
- کارتابل مبتنی بر User/Role/Department/CustomerOwner
- Audit کامل هر Transition و تاریخچه شمسی
- شروع خودکار از OrderCreated، TicketCreated و CustomerCreated
- FormCode و Hookهای قابل توسعه
- Seed آماده:
  - `WF-BANDWIDTH-FEASIBILITY`
  - `WF-BANDWIDTH-NO-FEASIBILITY`
- مسیر: Start → Feasibility → BackToSales → Purchase/Proforma → PaymentMode → Installation/Payment → Activated
- Seed دپارتمان‌های «فنی» و «خرید» برای اختصاص تسک
- کارتابل با نام مشتری و شماره سفارش/تیکت (بدون GUID خام)
- دکمه «ویرایش مستقیم (فقط برای تست)» برای برگرداندن تعریف منتشرشده به Draft بدون تغییر Version
- در Development و فقط SuperAdmin: `POST /api/system/reset-test-data` برای پاک‌سازی داده‌های تست و حفظ تعاریف و تنظیمات

### Sales API

| Endpoint | Permission |
|---|---|
| `GET /api/sales/service-requests` | `Orders.Order.Read` |
| `GET /api/sales/service-requests/{orderId}` | `Orders.Order.Read` |
| `GET /api/sales/customers/{customerId}/service-requests` | `Orders.Order.Read` |
| `POST /api/sales/service-requests` | `Orders.Order.Create` |
| `POST /api/sales/instances/{id}/feasibility-result` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/feasibility-review` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/equipment-supply` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/proforma` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/confirm-proforma` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/installation-specs` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/final-invoice` | `Orders.Order.Edit` |
| `POST /api/sales/orders/{id}/activate-customer-service` | `Orders.Order.Edit` |
| `POST /api/sales/orders/{id}/cancel-request` | `Orders.Order.Edit` |
| `POST /api/sales/services/{id}/collect` | `Orders.Order.Edit` |
| `POST /api/sales/services/{id}/adjust-bandwidth` | `Orders.Order.Edit` |
| `GET /api/portal/sales/requests` | نقش مشتری |
| `GET /api/portal/sales/services` | نقش مشتری |
| `POST /api/workflow/instances/{id}/action` | `Workflow.Instance.Execute` |
| `PUT /api/commissions/{id}/shares` | `Commission.Entry.Approve` |
| `POST /api/sales/instances/{id}/feasibility` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/decision` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/payment-mode` | `Orders.Order.Edit` |
| `POST /api/sales/instances/{id}/installation` | `Orders.Order.Edit` |
| `POST /api/sales/orders/{id}/activate` | `Orders.Order.Edit` |
| `POST /api/sales/invoices/{id}/confirm-payment` | `Orders.Order.Edit` |
| `GET /api/customers/{id}/services` | `Orders.Order.Read` |

---

## دسترسی‌ها و نقش‌ها

- `SuperAdmin`: Wildcard و دسترسی کامل
- `SystemAdmin`: مدیریت سیستم، کاربران، نقش‌ها و تنظیمات
- `SalesManager` / `SalesExpert`: مشتریان، سفارش‌ها، پرداخت‌ها و کیف پول
- `FinanceManager`: فاکتور، پرداخت، کیف پول، پورسانت و گزارش‌ها
- `Support` / `DataCenterSupport`: تیکت‌ها و پشتیبانی
- `Customer` / `Reseller`: فقط از طریق پورتال

منوی Settings فقط برای SystemAdmin/SuperAdmin و منوی Reports فقط با Permission گزارش نمایش داده می‌شود. مسیرهای حساس علاوه بر منو، در Frontend نیز با `PermissionRoute` محافظت شده‌اند.

---

## تست‌ها

### تست‌های Backend

```powershell
dotnet test tests/CrmSuite.UnitTests/CrmSuite.UnitTests.csproj
```

نتیجه فعلی: **۶۸ تست Unit موفق**.

برای Integration Tests نیاز به دیتابیس تست دارید:

```powershell
$env:CRMSUITE_TEST_MYSQL = "Server=127.0.0.1;Port=3306;Database=crm_suite;User=root;CharSet=utf8mb4;"
dotnet test tests/CrmSuite.IntegrationTests
```

### تست‌های Frontend

```powershell
cd frontend
pnpm typecheck
pnpm build
```

### تست دستی مسیر اصلی

1. با `superadmin` وارد شوید.
2. یک مشتری با موبایل معتبر بسازید.
3. یک کارشناس فروش بسازید و با او وارد شوید.
4. برای مشتری سفارش ثبت کنید و وضعیت را به «در انتظار پرداخت» تغییر دهید.
5. با مدیر مالی وارد شوید و فاکتور صادر کنید.
6. کیف پول مشتری را شارژ کنید یا پرداخت آنلاین را شروع کنید.
7. با موبایل مشتری وارد پورتال شوید و فاکتور را ببینید/پرداخت کنید.
8. برای مشتری تیکت بسازید و پاسخ دهید.
9. در Reports آمار فروش و دریافتی را بررسی کنید.

---

## Catalog API

| Endpoint | Permission |
|---|---|
| `GET /api/catalog/services` | `Catalog.Service.Read` |
| `GET /api/catalog/services/{id}` | `Catalog.Service.Read` |
| `POST/PUT /api/catalog/services` | `Catalog.Service.Manage` |
| `PUT /api/catalog/services/{id}/attribute-schema` | `Catalog.Service.Manage` |
| `POST /api/catalog/services/{id}/activate` / `deactivate` | `Catalog.Service.Manage` |
| `GET/POST /api/catalog/services/{id}/prices` | `Catalog.Price.Read` / `Catalog.Price.Manage` |
| `GET /api/catalog/services/{id}/addons` | `Catalog.AddOn.Read` |
| `GET/POST /api/catalog/services/{id}/forms` | `Catalog.Service.Read` / `Catalog.Service.Manage` |
| `DELETE /api/catalog/services/{id}/forms/{formId}` | `Catalog.Service.Manage` |
| `GET/POST/PUT /api/catalog/addons` | `Catalog.AddOn.Read` / `Catalog.AddOn.Manage` |
| `POST /api/catalog/addons/{id}/deactivate` | `Catalog.AddOn.Manage` |
| `POST /api/orders/{orderId}/items` | `Orders.Order.Create` |
| `PUT /api/orders/{orderId}/items/{itemId}` | `Orders.Order.Edit` |
| `POST /api/orders/{orderId}/items/{itemId}/override-price` | `Orders.Order.PriceOverride` |
| `POST /api/pricing/preview` | `Catalog.Price.Read` |

Migration `CatalogPricingAdvanced` فقط Additive است و قیمت، Addon، AttributeSchema و Snapshotهای OrderItem را با Backfill اضافه می‌کند.

## Forms API

| Endpoint | Permission |
|---|---|
| `GET /api/forms/templates` | `Forms.Form.Read` |
| `GET /api/forms/templates/{id}` | `Forms.Form.Read` |
| `GET /api/forms/templates/by-code/{code}` | `Forms.Form.Read` |
| `POST /api/forms/templates` | `Forms.Form.Manage` |
| `PUT /api/forms/templates/{id}` | `Forms.Form.Manage` |
| `POST /api/forms/templates/{id}/publish` | `Forms.Form.Manage` |
| `POST /api/forms/templates/{id}/archive` | `Forms.Form.Manage` |
| `POST /api/forms/submit` | `Forms.Submission.Create` |
| `GET /api/forms/submissions` | `Forms.Submission.Read` |
| `POST /api/forms/submissions/{id}/review` | `Forms.Submission.Review` |
| `POST /api/portal/forms/submit` | نقش Customer |

Migration `FormsBuilderInitial` ستون‌های قدیمی `customer_id` و `order_id` را حفظ می‌کند، داده‌های موجود را به `entity_type`/`entity_id` منتقل می‌کند و از این پس فقط مدل عمومی را می‌نویسد.

## Workflow API

| Endpoint | Permission |
|---|---|
| `GET /api/workflow/definitions` | `Workflow.Definition.Read` |
| `POST/PUT /api/workflow/definitions` | `Workflow.Definition.Manage` |
| `POST /api/workflow/definitions/{id}/publish` | `Workflow.Definition.Manage` |
| `POST /api/workflow/definitions/{id}/archive` | `Workflow.Definition.Manage` |
| `POST /api/workflow/definitions/{id}/services/{serviceId}` | `Workflow.Definition.Manage` |
| `POST/PUT/DELETE /api/workflow/definitions/{id}/states` | `Workflow.Definition.Manage` |
| `POST/DELETE /api/workflow/definitions/{id}/transitions` | `Workflow.Definition.Manage` |
| `POST /api/workflow/instances` | `Workflow.Instance.Start` |
| `GET /api/workflow/instances/{id}` | `Workflow.Instance.Read` |
| `POST /api/workflow/instances/{id}/transition` | `Workflow.Instance.Execute` |
| `POST /api/workflow/instances/{id}/cancel` | `Workflow.Instance.Cancel` |
| `GET /api/workflow/tasks/my` | `Workflow.Task.Read` |
| `POST /api/workflow/tasks/{id}/reassign` | `Workflow.Task.Reassign` |
| `GET /api/workflow/instances/by-entity` | `Workflow.Instance.Read` |

Migration جدید `WorkflowInitial` فقط Additive است و ستون‌های `workflow_definition_id` و `workflow_instance_id` را به ترتیب به Services/Orders/Tickets اضافه می‌کند.

## Migrationها

```powershell
dotnet tool restore

dotnet ef database update `
  --project src/BuildingBlocks/CrmSuite.BuildingBlocks.Infrastructure `
  --startup-project src/Hosts/CrmSuite.Api `
  --context AppDbContext
```

ساخت Migration جدید:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/BuildingBlocks/CrmSuite.BuildingBlocks.Infrastructure `
  --startup-project src/Hosts/CrmSuite.Api `
  --output-dir Persistence/Migrations `
  --context AppDbContext
```

---

## ساخت نسخه Production

### Backend

```powershell
dotnet publish src/Hosts/CrmSuite.Api/CrmSuite.Api.csproj `
  -c Release `
  -o "C:\Sites\CrmSuite\api"
```

### Frontend

```powershell
cd frontend
pnpm install --frozen-lockfile
pnpm build
```

خروجی در `frontend/dist` قرار می‌گیرد.

### استقرار پیشنهادی

- API را به‌صورت Windows Service یا پشت IIS/Nginx اجرا کنید.
- `frontend/dist` را با Nginx یا IIS به‌عنوان Static Site سرو کنید.
- مسیرهای `/api`، `/swagger`، `/health` و `/hubs` را به Backend Reverse Proxy کنید.
- برای SPA، همه مسیرهای غیرفایل را به `index.html` برگردانید.
- HTTPS را در Reverse Proxy فعال کنید.
- `Jwt:Key`، `Settings:EncryptionKey` و رمز دیتابیس را از محیط امن تزریق کنید.
- `Identity:SeedOnStartup` را در Production مدیریت کنید.
- CORS را فقط برای دامنه واقعی Frontend تنظیم کنید.

نمونه سرویس ویندوز:

```powershell
sc.exe create CrmSuite.Api binPath= "C:\Sites\CrmSuite\api\CrmSuite.Api.exe" start= auto
sc.exe start CrmSuite.Api
```

---

## امنیت و نکات Production

- رمز پیش‌فرض `Admin@123` را حتماً تغییر دهید.
- از HTTPS استفاده کنید.
- Refresh Tokenها را در دیتابیس نگه دارید و امکان ابطال آن‌ها را فعال کنید.
- Merchant/Terminal درگاه با AES رمزنگاری می‌شوند؛ کلید را گم نکنید.
- OTP در Production از طریق IPPanel Pattern ارسال می‌شود؛ در Development به‌صورت پیش‌فرض در لاگ ثبت می‌شود.
- لاگ‌های Serilog را با Retention مناسب نگه دارید.
- Backup دیتابیس و تست Restore را برنامه‌ریزی کنید.
- Permissionها را بر اساس اصل حداقل دسترسی تنظیم کنید.
- برای درگاه سامان، `Saman:IsSandbox` را در Production غیرفعال کنید.

---

## ساختار پروژه

```text
CrmSuite.sln
src/
  BuildingBlocks/
    CrmSuite.BuildingBlocks.Domain/
    CrmSuite.BuildingBlocks.Application/
    CrmSuite.BuildingBlocks.Infrastructure/
  Modules/
    Foundation/
    Identity/
    Customers/
    Catalog/
    Orders/
    Billing/
    Payments/
    Wallet/
    Tickets/
    Forms/
    Notifications/
    Reports/
    Settings/
    Workflow/
  Hosts/
    CrmSuite.Api/
tests/
  CrmSuite.UnitTests/
  CrmSuite.IntegrationTests/
frontend/
  src/
    app/
    modules/
      auth/
      dashboard/
      customers/
      orders/
      billing/
      payments/
      wallet/
      commission/
      tickets/
      reports/
      settings/
      portal/
    shared/
    styles/
docs/
```

---

## عیب‌یابی

### API بالا نمی‌آید
- پورت `5080` آزاد باشد.
- Connection String و سرویس MySQL بررسی شود.
- `http://127.0.0.1:5080/health` را باز کنید.
- لاگ‌های `src/Hosts/CrmSuite.Api/logs` را ببینید.

### Frontend به API وصل نمی‌شود
- Backend روی `5080` فعال باشد.
- `VITE_PROXY_TARGET` در `.env` درست باشد.
- CORS برای `http://127.0.0.1:5173` تنظیم شده باشد.

### OTP دریافت نمی‌شود
- شماره موبایل باید در جدول `customers` ثبت شده باشد.
- در Development کد در لاگ Serilog با عبارت `OTP for` ثبت می‌شود؛ برای ارسال واقعی از تب «اتصال پیامک» تنظیمات را کامل کنید یا `Sms__Ippanel__Enabled=true` و کلیدهای IPPanel را از Environment تنظیم کنید.
- برای تست دستی:

```sql
SELECT code
FROM customer_otps
WHERE mobile = '09xxxxxxxxx'
ORDER BY created_at DESC
LIMIT 1;
```

### تاریخ‌ها شمسی نیستند
- API باید فیلد `*Jalali` را برگرداند؛ Frontend از همان استفاده می‌کند.
- برای داده‌های قدیمی، Migration/Backfill لازم است.

### خطای ۴۰۳ در صفحه‌ای خاص
- Permission نقش را در Settings → نقش‌ها بررسی کنید.
- پس از تغییر Permission، کاربر باید مجدداً وارد شود تا توکن جدید بگیرد.

---

## تست‌های انجام‌شده در این نسخه

- تست کامل مسیر Happy Path از ساخت مشتری تا پرداخت پورتال و گزارش مالی
- تست OTP و پورتال مشتری
- تست Permission نقش SalesExpert و مخفی بودن Settings/Reports در منو
- تست عدم وجود خطای Console، Warningهای AntD/React Router و درخواست شبکه ناموفق در صفحات کارکنان و پورتال
- بررسی عدم نمایش تاریخ‌های میلادی/`NaN` در تمام صفحات
- تست TypeScript و Build نهایی Frontend
- تست‌های Unit بک‌اند (۱۱۰ تست)
- تست IPPanel Pattern با بررسی بدنه، تک‌گیرنده بودن، هدر Authorization و پاسخ خطا
- تست Idempotency OTP با CorrelationId
- تست Settings IPPanel: اولویت Environment، رمزنگاری ApiKey، ماسک، حفظ کلید قبلی و تنظیمات ناقص
- تست Integration چرخهٔ کامل Settings → Mask → Test → SmsResult
- تست انتخاب Workflow بر اساس بستر و تست فاکتور ماهانه بر پایه Snapshot
- تست فعال‌سازی سرویس و ایجاد رکورد CustomerService
- تست پورسانت Pending پس از فعال‌سازی و تست فاکتور دوره‌ای Snapshot
- تست فرمول ۱/۱۲ پورسانت و تقسیم چهار حالت سهم
- تست ActionKey/Dynamic Form و اجرای Action واقعی از کارتابل
- تست نمایش خوانای موجودیت در کارتابل و پاک‌سازی Development-only
- تست‌های Catalog شامل قیمت نسخه‌دار، Addon، Snapshot، Override و AttributeSchema
- تست‌های Integration Catalog برای Pricing Preview و فرم‌های اجباری
- تست‌های Forms Builder شامل Schema جدید، VisibleIf، Publish و Alias موجودیت
- تست E2E فرم‌ساز، ویرایشگر و پاسخ‌های فرم با Console/Network پاک
- تست‌های Workflow شامل چرخه کامل، Idempotency، Guard، Audit، کارتابل و شروع خودکار از سفارش
- تست E2E رابط Workflow (تعاریف، ویرایشگر، کارتابل و نمونه) با Console/Network پاک

---

## یادداشت‌های باقی‌مانده

- شارژ کیف پول مشتری در پورتال فعلاً از طریق پشتیبانی انجام می‌شود؛ درگاه اختصاصی شارژ کیف پول در نسخه بعدی اضافه می‌شود.
- گزارش‌ها بر مبنای دریافتی نقدی هستند و پرداخت از کیف پول به‌عنوان تراکنش داخلی دوباره در «کل دریافتی» شمرده نمی‌شود.
- برای Production حتماً MySQL 8 و Backup/HTTPS/Secrets مدیریت‌شده استفاده شود.
