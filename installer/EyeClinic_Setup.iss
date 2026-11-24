; =======================================================================
; نظام إدارة عيادة العيون - Eye Clinic Management System
; Inno Setup Script: إعداد حزمة التثبيت لجهازي السيرفر والعيادة
; متوافق تماماً مع: Windows 7 SP1 (مع Service Pack 1) وحتى Windows 10/11
; =======================================================================

#define MyAppName "نظام عيادة العيون"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Eng. Ahmed Adel"
#define MyAppExeName "EyeClinic.exe"

[Setup]
AppId={{5A8E4398-33B2-4E2F-A0C1-DDF197368E01}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf}\EyeClinic
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=EyeClinic_Setup_v1.0.1
Compression=lzma2/max
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
MinVersion=6.1sp1
DisableDirPage=no
DisableProgramGroupPage=no
WizardStyle=modern
SetupIconFile=..\src\EyeClinic.WinForms\app_icon.ico

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "server"; Description: "تثبيت السيرفر والاستقبال (جهاز السكرتارية الرئيسي وقاعدة البيانات)"
Name: "client"; Description: "تثبيت جهاز الطبيب (العميل - Client المتصل عبر الشبكة المحلية)"

[Components]
Name: "core"; Description: "ملفات تشغيل البرنامج الأساسية ومكتبات EF6"; Types: server client; Flags: fixed
Name: "dbscripts"; Description: "اسكربتات تهيئة قاعدة البيانات SQL Server 2014 Express"; Types: server

[Files]
; الملفات التنفيذية والمكتبات الأساسية
Source: "..\src\EyeClinic.WinForms\bin\Debug\EyeClinic.exe"; DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\src\EyeClinic.WinForms\bin\Debug\EyeClinic.exe.config"; DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\src\EyeClinic.WinForms\bin\Debug\EyeClinic.Core.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\src\EyeClinic.WinForms\bin\Debug\EyeClinic.Data.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\src\EyeClinic.WinForms\bin\Debug\EyeClinic.Business.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\src\EyeClinic.WinForms\bin\Debug\EntityFramework.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\src\EyeClinic.WinForms\bin\Debug\EntityFramework.SqlServer.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\src\EyeClinic.WinForms\bin\Debug\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion; Components: core
Source: "..\src\EyeClinic.WinForms\bin\Debug\app_icon.ico"; DestDir: "{app}"; Flags: ignoreversion; Components: core

; اسكربتات قاعدة البيانات
Source: "Database_Setup_SQL2014.sql"; DestDir: "{app}\Database"; Flags: ignoreversion; Components: dbscripts
Source: "setup_guide.md"; DestDir: "{app}"; DestName: "دليل_التشغيل_والتثبيت_للطبيب_والفني.md"; Flags: ignoreversion; Components: core

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app_icon.ico"
Name: "{group}\دليل التثبيت والتشغيل"; Filename: "{app}\دليل_التشغيل_والتثبيت_للطبيب_والفني.md"
Name: "{group}\إلغاء تثبيت البرنامج"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\app_icon.ico"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب (Desktop Shortcut)"; GroupDescription: "اختصارات إضافية:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل نظام عيادة العيون الآن"; Flags: nowait postinstall skipifsilent

[Code]
// التحقق من وجود .NET Framework 4.8 على نظام التشغيل
function IsDotNet48Installed: Boolean;
var
  release: Cardinal;
begin
  Result := False;
  // قيمة Release لـ .NET Framework 4.8 هي 528040 فما فوق
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release) then
  begin
    if release >= 528040 then
      Result := True;
  end;
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  if not IsDotNet48Installed then
  begin
    MsgBox(
      'تنبيه: يتطلب نظام عيادة العيون وجود حزمة Microsoft .NET Framework 4.8 مثبتة على هذا الجهاز.' + #13#10 + #13#10 +
      'يرجى تثبيت .NET Framework 4.8 أولاً (متوافقة مع Windows 7 SP1 فما فوق) ثم إعادة تشغيل برنامج التثبيت.',
      mbCriticalError, MB_OK);
    Result := False;
  end;
end;
