# Dynamic Formula Engine

> מבדק פיתוח רמה ג - משרד החינוך

מערכת לחישוב דינמי של נוסחאות מתמטיות על **מיליון רשומות**.
במקום לקבע נוסחאות בקוד, המערכת קוראת אותן מה-DB בזמן ריצה.

**3 שיטות חישוב · Web API · Angular Dashboard**

🔗 **[צפייה בדשבורד](https://rachel726.github.io/DynamicFormula/)**

---

## 3 שיטות חישוב

| # | שיטה | גישה | ממוצע לנוסחה |
|---|------|------|--------------|
| 1 | SQL — Stored Procedure | `sp_executesql` + `INSERT...SELECT` | ~16.4s |
| 2 | C# .NET — Compiled Lambda | Expression Trees + `Parallel.For` | ~0.013s |
| 3 | Node.js — math.js | `math.compile()` + evaluate loop | ~0.87s |

<p dir="rtl">C# מציע חישוב בזיכרון מקביל ומהיר, SQL מספק תחזוקה יציבה, ו-Node.js נותן גמישות מהירה לפיתוח ובדיקה.</p>

---

## דוח מסכם

### רקע ומטרה

מערכת תשלומים הדורשת חישוב נוסחאות דינמיות על מיליוני רשומות, כאשר הנוסחאות משתנות בזמן ריצה ישירות ממסד הנתונים — ללא שינוי קוד.
המטרה: להשוות מספר פתרונות לחישוב נוסחאות דינמיות ולהגיע למסקנה — מהו הפתרון החכם והמהיר ביותר?

---

### נתוני הבדיקה

| פרמטר | ערך |
|--------|-----|
| רשומות נתונים | 1,000,000 (ערכים רנדומליים Float בשדות a, b, c, d) |
| נוסחאות שנבדקו | 16 — פשוטות, מורכבות ועם תנאים |
| מדידה | זמן ריצה לנוסחה על כלל המיליון רשומות (שמור ב-`t_log`) |
| אימות | תוצאות הושוו בין כל השיטות — אין סטיות |

---

### תוצאות ביצועים

| # | שיטה | ממוצע לנוסחה | סה"כ ל-16 נוסחאות | יחס לעומת C# |
|---|------|:---:|:---:|:---:|
| 🥇 | **C# .NET — Compiled Lambda** | **~0.013s** | **~0.21s** | ×1 |
| 🥈 | Node.js — math.js | ~0.87s | ~13.8s | ×67 |
| 🥉 | SQL — Stored Procedure | ~16.4s | ~262s | ×1,260 |

> הזמנים נמדדו על חומרה מקומית. **היחסים בין השיטות עקביים בכל סביבה.**

---

### ניתוח השיטות

**שיטה 1 — SQL Stored Procedure**

<p dir="rtl">כותב מיליון שורות לדיסק בכל נוסחה — כל שורה עוברת דרך transaction log. יתרון: הנתונים נשארים ב-DB, פשוט לתחזוקה, ללא תלות בשפה חיצונית. חיסרון: I/O לדיסק לכל נוסחה — זה צוואר הבקבוק.</p>

**שיטה 2 — C# .NET Compiled Lambda** ✅ **מומלצת**

<p dir="rtl">מקמפל כל נוסחה פעם אחת לפונקציה מהודרת, ואז מריץ מיליון חישובים במקביל על כל ליבות ה-CPU. ללא גישות דיסק, ללא תעבורת רשת — הכל בזיכרון. מהיר פי <strong>1,260</strong> מ-SQL.</p>

**שיטה 3 — Node.js math.js**

<p dir="rtl">מקמפל כל נוסחה מראש ומריץ מיליון חישובים בלולאה. מהיר פי 30 מ-SQL — אך JavaScript חד-תהליכי ואינו מנצל מרובה ליבות. יתרון: פיתוח מהיר וגמישות דינמית מלאה.</p>

---

### המלצה

<p dir="rtl"><strong>הפתרון המומלץ לסביבת ייצור הוא C# .NET</strong> — חישוב בזיכרון על כל ליבות ה-CPU, ללא גישות דיסק וללא תעבורת רשת.</p>

<p dir="rtl"><strong>SQL</strong> — מתאים כשפשטות ותחזוקה חשובות יותר מביצועים.</p>

<p dir="rtl"><strong>Node.js</strong> — מתאים לפיתוח מהיר ולסביבות הדורשות גמישות דינמית.</p>

---

## ארכיטקטורה

```
SQL Server (DynamicFormula DB)
  t_data · t_targil · t_results · t_log
         |
  +------+----------+
  |      |          |
 SQL    C#        Node.js      <- 3 calculation methods, each saves to t_log
  +------+----------+
         |
   C# Benchmark Runner         <- dotnet run --export-only
         | report.json
         |
   DynamicFormula.Api          <- ASP.NET Core Web API
   GET /api/performance
   GET /api/formulas
         |
   Angular Dashboard           <- dev: live API | prod: static JSON (GitHub Pages)
```

---

## מבנה הפרויקט

```
DynamicFormula/
├── sql/
│   ├── 01_schema.sql           -- DB schema + 4 tables + SQL user
│   ├── 02_seed_data.sql        -- 1M random rows
│   ├── 03_seed_formulas.sql    -- 16 formulas
│   ├── 04_sp_calculate.sql     -- Stored Procedure (method 1)
│   └── 05_verify_results.sql   -- compare results across methods
│
├── src/
│   ├── DynamicFormula.Core/        -- Models + Interfaces
│   ├── DynamicFormula.Engine/      -- Formula parser + compiler
│   ├── DynamicFormula.Data/        -- Repository (Dapper + SqlBulkCopy)
│   ├── DynamicFormula.Calculators/ -- Method 2: C# Compiled Lambda
│   ├── DynamicFormula.Benchmarks/  -- Benchmark runner + JSON export
│   └── DynamicFormula.Api/         -- Web API: GET /api/performance
│
├── node-calculator/
│   └── calculate.js            -- Method 3: Node.js + math.js
│
├── run-benchmarks.ps1          -- runs all 3 methods in sequence
│
└── dashboard/                  -- Angular 19 Dashboard → GitHub Pages
```

---

## הרצה מקומית

### שלב 1 — SQL Server (SSMS)

פתחו SSMS והריצו את הסקריפטים **בסדר הזה**:

| קובץ | תיאור |
|------|-------|
| `sql/01_schema.sql` | יצירת DB + 4 טבלאות + משתמש SQL |
| `sql/02_seed_data.sql` | מיליון רשומות רנדומליות (~20 שניות) |
| `sql/03_seed_formulas.sql` | 16 נוסחאות לבדיקה |
| `sql/04_sp_calculate.sql` | יצירת Stored Procedure לשיטה 1 |

**לפני הרצה — שני דברים לוודא:**

**1. שם השרת** — שנו את שם השרת המקומי שלכם בשני קבצים:

- `src/DynamicFormula.Benchmarks/Program.cs`
- `src/DynamicFormula.Api/appsettings.json`

ברירת מחדל: `.\SQLEXPRESS`


<p dir="rtl"><strong>2. Mixed Mode Authentication</strong> — נדרש עבור Node.js וה-API כדי לתמוך ב-SQL Server Authentication ולא רק ב-Windows Authentication:</p>

```
SSMS → לחצן ימני על השרת → Properties → Security
→ SQL Server and Windows Authentication mode
```

---

### שלב 2 — הרצת 3 שיטות החישוב

פתחו **PowerShell** מתיקיית השורש (`DynamicFormula/`) והריצו:

```powershell
.\run-benchmarks.ps1
```

הסקריפט מריץ את 3 השיטות ברצף:

| שלב | מה קורה |
|-----|---------|
| [1/3] | C# מריץ SQL Stored Procedure + C# Lambda, שומר תוצאות ל-DB |
| [2/3] | Node.js מריץ math.js על מיליון שורות, שומר תוצאות ל-DB |
| [3/3] | ייצוא report.json לדשבורד אחרי שכל 3 השיטות סיימו |

> משך הרצה מלאה: כ-10 דקות — 16 נוסחאות x מיליון שורות x 3 שיטות

---

### שלב 3 — אימות תוצאות (SSMS)

```sql
-- הריצו אחרי שכל 3 השיטות סיימו:
sql/05_verify_results.sql
```

---

### שלב 4 — Web API + Dashboard

```powershell
# טרמינל 1 — Web API
cd src/DynamicFormula.Api
dotnet run
# Swagger: http://localhost:5000/swagger

# טרמינל 2 — Angular Dashboard
cd dashboard
ng serve
# http://localhost:4200
```

---

## מבנה הנתונים

| טבלה | תיאור |
|------|-------|
| `t_data` | מיליון רשומות: data_id, a, b, c, d |
| `t_targil` | 16 נוסחאות: targil, tnai, targil_false |
| `t_results` | תוצאות החישוב לכל שיטה |
| `t_log` | זמני ריצה — בסיס להשוואה בדשבורד |

---

## צילומי מסך — בסיס הנתונים

תיקייה: [`screenshots/`](screenshots/)

---

## דרישות המבדק

| דרישה | מימוש |
|-------|-------|
| 3 תוכניות בשפות שונות | SQL, C#, Node.js |
| 4 טבלאות במבנה המוגדר | t_data, t_targil, t_results, t_log |
| מיליון רשומות רנדומליות | 02_seed_data.sql |
| נוסחאות פשוטות + מורכבות + מותנות | 16 נוסחאות |
| מדידת זמנים + שמירה ב-t_log | כל שיטה שומרת אוטומטית |
| סקריפט השוואת תוצאות | 05_verify_results.sql |
| מסך דוח | Angular Dashboard + GitHub Pages |

---

## טכנולוגיות

| שכבה | טכנולוגיה |
|------|-----------|
| שפות חישוב | SQL Server, C# 13 / .NET 9, Node.js 18+ |
| גישה לנתונים | Dapper, SqlBulkCopy |
| Web API | ASP.NET Core, Swagger |
| Formula Parser | Expression Trees + Compiled Lambda (C#) |
| Dashboard | Angular 19, TypeScript, SCSS |
| פריסה | GitHub Pages |

---

## ארכיטקטורה ועיצוב

<div dir="rtl">

### 🎯 **Dual Mode**: פיתוח וייצור בלתי תלויים

**בסביבת פיתוח** — הדשבורד מתחבר ל-**API חי** דרך Swagger, קורא נתונים אמיתיים מה-DB בזמן אמת, ותומך בשינויים מהירים וחזרות פיתוח.

**בסביבת ייצור** — הדשבורד משתמש ב-**קובץ JSON סטטי** (`report.json`) המופץ ל-GitHub Pages — פשוט, מהיר וללא דרישות שרת.

**התוצאה**: פיתוח אמיתי עם API + פריסה יציבה וקלה ללא תלות בשרת.

---

### 💡 Core Technologies

- **Expression Trees + Parallel.For (C#)** — חישוב נוסחאות בזמן ריצה ב-0.013 שניות לנוסחה (1M רשומות)
- **Angular 19 + TypeScript + SCSS** — דשבורד מקצועי עם טבלאות אינטראקטיביות, גרפים לוגריתמיים וויזואליזציה ברמה גבוהה
- **SQL Server + Node.js** — מנועי חישוב חלופיים המשמשים בדיקה חוצת-שיטה וההשוואה ביצועים
- **ASP.NET Core Web API + Swagger** — Web API עם תיעוד אוטומטי ל-develop וbenchmark בזמן אמת

</div>

