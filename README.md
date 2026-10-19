# Insurance Claims API – ניהול תביעות ביטוח

שרת ASP.NET Core (.NET 10) לניהול תביעות ביטוח מול PostgreSQL. שמאים לוקחים תביעות לטיפול, והמערכת מבטיחה שכל תביעה תגיע **לשמאי אחד בלבד**, גם כששני שמאים לוחצים "קח לטיפול" באותו רגע.

הלקוח (React) נמצא ב-Repository נפרד: `insurance-claims-client`.

---

## תיאור המערכת

| ישות | תיאור |
|------|-------|
| `InsuranceType` | סוג ביטוח (רכב, דירה, בריאות). שמאים מתמחים בסוגי ביטוח. |
| `Policy` | פוליסה של מבוטח, עם תאריכי תוקף וסוג ביטוח. |
| `Claim` | תביעה על פוליסה. סטטוס: `Open` → `InProgress` → `Approved` / `Rejected`. |
| `Employee` | עובד: `Manager` או `Adjuster` (שמאי). קשר many-to-many לסוגי הביטוח (תחומי התמחות). |

**כללים עסקיים** (נאכפים בשכבת ה-Service, לא ב-Controller):

- תביעה נפתחת רק על פוליסה שבתוקף כרגע.
- שמאי יכול לקחת לטיפול רק תביעה מתחום ההתמחות שלו.
- רק העובד המטפל בתביעה יכול לעדכן את הסטטוס שלה (אישור, דחייה, או שחרור חזרה ל-`Open`).
- הרשמה עצמית יוצרת תמיד שמאי. תפקיד מנהל ניתן רק ע"י מנהל.

## ארכיטקטורה

```
InsuranceClaims.API      →  Controllers, Middleware, JWT, Swagger, NLog, Program.cs
   │  (מפנה ל-Service, Data, Core)
InsuranceClaims.Service  →  לוגיקה עסקית, AutoMapper Profiles, BCrypt
   │  (מפנה ל-Core בלבד)
InsuranceClaims.Data     →  DbContext, Fluent API, Seed, Migrations, Repositories
   │  (מפנה ל-Core בלבד, חבילת Npgsql)
InsuranceClaims.Core     →  ישויות, Enums, DTOs, Interfaces, OperationResult
      (ללא חבילות NuGet חיצוניות)
InsuranceClaims.Tests    →  xUnit + Moq
```

- **Core** לא מכיר את EF Core. ה-`[ConcurrencyCheck]` על `Claim.Version` היא Data Annotation מובנית ב-.NET.
- **Controllers** מזריקים רק Services, לעולם לא `DbContext`, ולא מקבלים או מחזירים ישויות, רק DTOs.
- כל פעולה עסקית מחזירה `OperationResult` (Success / NotFound / Conflict / Unauthorized / Forbidden / ValidationFailed), וה-Controller מתרגם אותו לסטטוס HTTP.
- ה-Repositories אסינכרוניים ומקבלים `CancellationToken`. הם משתמשים ב-`AsNoTracking` בקריאה, ב-`Include`/`ThenInclude` לנתונים קשורים (בלי N+1), וסינון, מיון ו-`Skip`/`Take` מתורגמים ל-SQL לפני ה-`ToListAsync`.

## התביעה כמשאב מוגבל – טיפול בתחרות

תביעה פתוחה היא משאב שרק עובד אחד יכול לקבל. שני שמאים מאותו תחום רואים את אותה רשימה ויכולים ללחוץ "קח לטיפול" בו-זמנית. בלי הגנה, שניהם יקראו "Open", שניהם ישמרו, והאחרון ידרוס את הראשון בשקט.

הפתרון הוא **Optimistic Concurrency**:

1. ל-`Claim` יש שדה `Version` מסוג `Guid` עם `[ConcurrencyCheck]`.
2. `AppDbContext.SaveChangesAsync` נדרס, ונותן `Version` חדש לכל תביעה שנוספה או השתנתה. כך אף Service לא צריך לזכור לעשות זאת.
3. EF Core מוסיף ל-`UPDATE` את התנאי `WHERE "Id" = @id AND "Version" = @originalVersion`.
   - השמירה הראשונה מצליחה ומחליפה את ה-Version.
   - השמירה השנייה לא מוצאת שורה מתאימה (0 rows), ו-EF זורק `DbUpdateConcurrencyException`.
4. `UnitOfWork` (שכבת Data) תופס **רק** `DbUpdateConcurrencyException` ומתרגם אותה ל-`ConcurrencyConflictException` של Core. כך ה-Service לא תלוי ב-EF.
5. `ClaimService.TakeAsync` טוען את התביעה, בודק סטטוס, התמחות וגרסה, ושומר, **באותה יחידת עבודה** (אותו `DbContext` ואותה ישות במעקב). התנגשות מחזירה `Conflict`, ונרשם לוג ברמת Warning.
6. ה-Controller מחזיר **409** עם ההודעה: **"התביעה נלקחה לטיפול על ידי עובד אחר"**.

"לקיחה לטיפול" מוגדרת ב-REST כעדכון של המטפל בתביעה: `PUT /api/claims/{id}/handler` עם `{ "version": "..." }`. הגרסה שהלקוח קרא נשלחת, כך שגם לחיצה על נתונים ישנים מחזירה 409.

## הרצה מקומית

### דרישות

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- **PostgreSQL** (גרסה 14 ומעלה) שרץ מקומית, עם משתמש וסיסמה

### 1. Connection String ומפתח JWT ב-User Secrets

הסודות **לא** נשמרים ב-`appsettings.json`. מתוך תיקיית ה-Repository:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=insurance_claims;Username=postgres;Password=YOUR_PASSWORD" --project src/InsuranceClaims.API
```

```bash
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_A_LONG_RANDOM_SECRET_OF_AT_LEAST_32_CHARS" --project src/InsuranceClaims.API
```

אפשר ליצור מפתח אקראי ב-PowerShell:

```powershell
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
```

### 2. הפעלת ה-Migrations (יוצר את בסיס הנתונים ואת ה-Seed)

```bash
dotnet tool restore
```

```bash
dotnet ef database update --project src/InsuranceClaims.Data --startup-project src/InsuranceClaims.API
```

יש שתי Migrations: `Init` (הטבלאות וה-Seed) ו-`AddClaimVersion` (עמודת ה-Version של התביעה).

### 3. הרצת השרת

```bash
dotnet run --project src/InsuranceClaims.API --launch-profile http
```

- API: `http://localhost:5080`
- Swagger: `http://localhost:5080/swagger` (בסביבת Development). מתחברים ב-`POST /api/auth/login`, מעתיקים את ה-`token`, לוחצים **Authorize** ומדביקים אותו.

## משתמשי דמו (מה-Seed)

| תפקיד | שם משתמש | סיסמה | תחומי התמחות |
|-------|----------|-------|--------------|
| מנהל | `manager` | `Manager123!` | – |
| שמאי | `adjuster1` | `Adjuster123!` | רכב, דירה |
| שמאי | `adjuster2` | `Adjuster123!` | רכב, בריאות |

שני השמאים מתמחים ב**ביטוח רכב**, כדי שאפשר יהיה להדגים תחרות על אותן תביעות. ה-Seed כולל 3 סוגי ביטוח, 5 פוליסות (אחת מהן שפג תוקפה) ו-12 תביעות פתוחות. כל התאריכים ב-UTC.

**הדגמת התחרות:** מתחברים כ-`adjuster1` בדפדפן אחד וכ-`adjuster2` בדפדפן אחר (או בחלון פרטי), פותחים את רשימת התביעות הפתוחות, ולוחצים "קח לטיפול" על אותה תביעת רכב בשניהם. הראשון מצליח, והשני מקבל 409.

## Endpoints

| Method | Route | הרשאה | תשובות |
|--------|-------|-------|--------|
| POST | `/api/auth/register` | אנונימי | 201 + Location, 400, 409 |
| POST | `/api/auth/login` | אנונימי | 200, 400, 401 |
| GET | `/api/insurance-types` | אנונימי | 200 |
| GET | `/api/insurance-types/{id}` | אנונימי | 200, 404 |
| POST / PUT / DELETE | `/api/insurance-types[/{id}]` | Manager | 201 / 204, 400, 401, 403, 404, 409 |
| GET | `/api/policies?page&pageSize&insuranceTypeId&search` | מחובר | 200, 401 |
| GET | `/api/policies/{id}` | מחובר | 200, 404 |
| POST / PUT / DELETE | `/api/policies[/{id}]` | Manager | 201 / 204, 400, 401, 403, 404, 409 |
| GET | `/api/claims?page&pageSize&status&insuranceTypeId&handlerId&sortBy&descending` | מחובר | 200, 400, 401 |
| GET | `/api/claims/{id}` | מחובר | 200, 404 |
| POST | `/api/claims` | מחובר | 201 + Location, 400 (פוליסה לא בתוקף) |
| PUT | `/api/claims/{id}` | Manager | 200, 400, 404, 409 |
| PUT | `/api/claims/{id}/handler` | Adjuster | 200, 403 (מחוץ להתמחות), 404, **409** |
| PUT | `/api/claims/{id}/status` | Adjuster (המטפל בלבד) | 200, 400, 403, 404, 409 |
| DELETE | `/api/claims/{id}` | Manager | 204, 404 |
| GET | `/api/employees?page&pageSize` | Manager | 200 |
| GET | `/api/employees/{id}` | Manager או העובד עצמו | 200, 403, 404 |
| PUT / DELETE | `/api/employees/{id}` | Manager | 200 / 204, 400, 404, 409 |

כל שגיאה מוחזרת במבנה JSON אחיד:

```json
{ "status": 409, "message": "התביעה נלקחה לטיפול על ידי עובד אחר", "correlationId": "4a70b4627adf4d87...", "errors": null }
```

## Middleware, לוגים ואבטחה

סדר ה-Pipeline:

1. `ExceptionHandlingMiddleware`: החיצוני ביותר. תופס כל חריגה, רושם אותה ברמת Error, ומחזיר 500 בפורמט האחיד, בלי פרטים פנימיים.
2. `CorrelationIdMiddleware`: לוקח את `X-Correlation-Id` מהבקשה (אם תקין) או יוצר חדש. המזהה מוחזר בכותרת התשובה ונכנס ל-Scope של הלוג.
3. `RequestLoggingMiddleware`: שורת Information לכל בקשה (method, path, status, משך).
4. `UseAuthentication` לפני `UseAuthorization`.

**NLog** (`nlog.config`): הקוד עובד רק עם `ILogger<T>`. הלוגים נכתבים לקונסול ולקובץ `logs/app-{date}.log` ליד קובץ ההרצה. כל שורה כוללת את ה-CorrelationId. רמת Debug פעילה רק ב-Development. התנגשות על תביעה נרשמת ברמת Warning. גוף הבקשה, Query String וכותרות לא נרשמים, ולכן סיסמאות, Tokens, ה-Connection String ותוכן ההרשמה לא מגיעים ללוג.

**JWT**: ה-Token כולל את מזהה העובד (`sub`), שם, שם משתמש ותפקיד (`role`). ההגדרות נטענות דרך `IOptions<JwtSettings>` ונבדקות בעליית השרת (מפתח של 32 בתים לפחות). הסיסמה נשמרת כ-BCrypt hash, ושום DTO של תשובה לא מכיל סיסמה.

## Lifetimes של שירותים

- `DbContext`, ה-Repositories, ה-`UnitOfWork` וה-Services רשומים כ-**Scoped**: מופע אחד לכל בקשת HTTP. כל ה-Repositories בבקשה חולקים את אותו `DbContext`, ולכן זו יחידת עבודה אחת. `DbContext` אינו thread-safe, וכך הוא לעולם לא משותף בין בקשות.
- רכיבים חסרי מצב (`BCryptPasswordHasher`, `JwtTokenGenerator`, `TimeProvider`) רשומים כ-**Singleton**.
- **למה Singleton שמחזיק Scoped הוא באג** ("captive dependency"): ה-Singleton חי כל זמן שהאפליקציה רצה, ולכן הוא היה מחזיק לנצח את ה-`DbContext` של הבקשה הראשונה. התוצאה: אותו `DbContext` משותף בין בקשות מקבילות (לא thread-safe), נתונים ישנים נשארים במעקב, ושימוש ב-`DbContext` אחרי שעבר Dispose. `Program.cs` מפעיל `ValidateScopes` ו-`ValidateOnBuild`, כך שטעות כזו מפילה את השרת כבר בעלייה.

## בדיקות

```bash
dotnet test
```

- `ClaimServiceTests` (Moq): לקיחה תקינה, לקיחה שנדחית כי התביעה כבר בטיפול, שמאי מחוץ לתחום ההתמחות, הפסד במרוץ בשמירה, גרסה ישנה, עדכון סטטוס ע"י מי שאינו המטפל, ופתיחת תביעה על פוליסה שפג תוקפה.
- `MappingTests`: תקינות כל ה-AutoMapper Profiles, ובדיקה שאין סיסמה באף DTO של תשובה.
- `ClaimConcurrencyTests`: **בדיקת התחרות מול PostgreSQL אמיתי**. שני `DbContext` נפרדים קוראים את אותה תביעה ושניהם משנים אותה. הראשון נשמר, והשני מקבל `DbUpdateConcurrencyException`.

בדיקת התחרות רצה רק מול **בסיס נתונים נפרד לבדיקות**. הבדיקה מוחקת ויוצרת אותו מחדש, ולכן היא מסרבת לרוץ אם שם בסיס הנתונים לא מכיל `test`. בלי משתנה הסביבה הבדיקה מדולגת (Skipped):

```powershell
$env:INSURANCE_CLAIMS_TEST_DB = "Host=localhost;Port=5432;Database=insurance_claims_test;Username=postgres;Password=YOUR_PASSWORD"
dotnet test
```

## הערות

- **AutoMapper** (גרסה 16) דורש מפתח רישיון לשימוש מסחרי. רישיון Community ניתן בחינם, וללא מפתח השרת רק כותב אזהרה ללוג. אם יש מפתח, שומרים אותו ב-User Secrets: `dotnet user-secrets set "AutoMapper:LicenseKey" "..." --project src/InsuranceClaims.API`.
- כדי ליצור Migration חדשה: `dotnet ef migrations add <Name> --project src/InsuranceClaims.Data --startup-project src/InsuranceClaims.API --output-dir Migrations`.
