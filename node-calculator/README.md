# 🟢 Node.js Calculator — שיטה 3

מחשבון נוסחאות דינמי הרץ על **Node.js** (צד שרת) — בדיוק כמו שהדוגמה של Python במבדק.

---

## 📋 דרישות מקדימות

- **Node.js** גרסה 18 ומעלה — [להורדה](https://nodejs.org)
- **SQL Server** עם הבסיס `DynamicFormula` שכבר הוקם

---

## 🚀 הרצה

**מומלץ:** להריץ את כל 3 השיטות יחד — מתיקיית השורש `DynamicFormula/`:
```powershell
.\run-benchmarks.ps1
```

**הרצה ידנית** מתוך התיקייה `node-calculator/`:

```bash
# התקנה חד-פעמית של תלויות
npm install

# הרצת הבנצ'מרק
node calculate.js
```

> ⚠️ `calculate.js` שומר תוצאות ל-DB בלבד — **לא מעדכן** את `report.json`.
> כדי לעדכן את הדשבורד הסטטי (GitHub Pages), יש להריץ אחר כך:
> ```powershell
> cd ..\src\DynamicFormula.Benchmarks
> dotnet run -c Release -- --export-only
> ```

---

## 🧠 איך זה עובד

1. מתחבר ל-SQL Server
2. טוען את כל 16 הנוסחאות מ-`t_targil`
3. טוען מיליון רשומות מ-`t_data` לזיכרון
4. עבור כל נוסחה:
   - **מקמפל פעם אחת** עם `math.compile()`
   - **מריץ מיליון חישובים** עם הפונקציה המקומפלת
   - מודד זמן מדויק ב-`performance.now()`
   - שומר תוצאות ב-`t_results` (Bulk Insert)
   - שומר זמן ריצה ב-`t_log`

---

## 🛠️ ספריות

- **`mssql`** — חיבור ל-SQL Server מ-Node.js
- **`msnodesqlv8`** — אימות Windows (Trusted Connection)
- **`mathjs`** — מנוע חישוב ביטויים מתמטיים עם קומפילציה

---

## 📊 דוגמת פלט

```
╔══════════════════════════════════════════════════════════════╗
║       ⚡ Dynamic Formula Engine — Node.js Calculator ⚡       ║
╚══════════════════════════════════════════════════════════════╝

▶ Connecting to SQL Server... ✓ Connected
▶ Loaded 16 formulas
▶ Loading 1M rows into memory... 2.34s (1,000,000 rows)

┌────────────────────────────────────────────────┐
│  Running Benchmark                             │
└────────────────────────────────────────────────┘
  ✓ #01    1.234s   (1,000,000 rows)   חיבור שני שדות
  ✓ #02    1.187s   (1,000,000 rows)   כפל שדה בקבוע
  ...

╔══════════════════════════════════════════════════════════════╗
║                      NODE.JS SUMMARY                         ║
╚══════════════════════════════════════════════════════════════╝
  Formulas processed:  16
  Average time:        1.389s
  Total wall time:     28.45s

  ✓ Results saved to SQL Server (method = 'NODEJS')
```
