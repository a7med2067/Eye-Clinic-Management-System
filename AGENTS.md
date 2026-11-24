# قواعد المشروع الصارمة (Project Constraints & Guidelines)

## الهدف التقني والبيئة المستهدفة:
- **نظام التشغيل المستهدف:** Windows 7 SP1 (مع دعم Windows 10).
- **إطار العمل:** .NET Framework 4.8 (`TargetFrameworkVersion: v4.8`).
- **لغة البرمجة:** C# 7.3 (`<LangVersion>7.3</LangVersion>`).
  - **ممنوع تماماً:** استخدام أي ميزة من C# 8.0 فما فوق (مثل: Nullable Reference Types كـ `string?`، Switch Expressions، Records، Init-only setters، Target-typed new، Top-level statements).
- **طبقة الوصول للبيانات (ORM):** Entity Framework 6.4.4 (EF6) حصراً. ممنوع استخدام EF Core.
- **قاعدة البيانات:** SQL Server 2014 Express (مستوى التوافق 120).
  - **ممنوع تماماً:** استخدام أي ميزات خاصة بـ SQL Server 2016 فما فوق (مثل: `DROP IF EXISTS`، `STRING_SPLIT`، دوال الـ JSON، الجداول الزمنية Temporal Tables).
- **واجهة المستخدم:** Windows Forms (WinForms) مع دعم كامل للعربية (RTL: RightToLeft) والتوافق التام مع نمط الشاشات القديمة والحديثة.
- **المعمارية:** N-Tier Architecture (Core, Data, Business, WinForms) لتسهيل الصيانة والترقية اللاحقة دون المساس بقواعد العمل.

## التوجيه الأساسي:
تجاهل أي نصيحة أو حزمة NuGet أو مكتبة خارجية أو API يحتاج إصدار أحدث من .NET Framework 4.8 أو C# 7.3 أو SQL Server 2014.
