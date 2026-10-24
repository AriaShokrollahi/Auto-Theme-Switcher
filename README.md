<div align="center">
Auto Theme Switcher

Windows switches between Light and Dark mode by itself, at the times you choose. ویندوز در ساعت‌هایی که شما تعیین می‌کنید خودش بین تم روشن و تاریک عوض می‌شود.

Show Image Show Image Show Image

English  |  فارسی

</div> <!-- Add a screenshot here, e.g. docs/screenshot.png --> <!-- ![Auto Theme Switcher](docs/screenshot.png) -->

<a name="english"></a>

English

A tiny Windows app that flips between Light and Dark mode at the times you choose. It lives in the system tray, starts with Windows, and otherwise stays out of the way.

Why I made this

I wanted Windows to go dark in the evening and light in the morning without me touching anything. Windows can't do that on a schedule by itself, and the apps that can were either bigger than I needed or did more than I wanted.

So I made the smallest thing that does the job: pick two times, save, forget about it.

Features
Switches Windows (and apps) to Light or Dark mode at the times you set
Works for overnight ranges too, like Light from 22:00 to 06:00
Starts with Windows and sits quietly in the tray
Fixes the theme on startup, so if your PC was off at switch time you still end up in the right mode
Its own window follows the theme and your accent color, so it matches whatever Windows is doing
Installs without admin rights and shows up in Settings → Apps like a normal program
No internet access, no telemetry, no dependencies
Requirements
Windows 10 or 11
Nothing else. The app is built with the C# compiler that already ships with Windows, so you don't need Visual Studio, the .NET SDK, or admin rights.
Install

There's no .exe in this repo, on purpose. You build it yourself in a few seconds:

Download or clone this repo
Double-click build.bat
The installer (AutoThemeSwitcher_Setup.exe) opens when the build finishes
Click Install

That's it. The installer puts everything in %LocalAppData%\Programs\AutoThemeSwitcher, adds a Start menu shortcut, and registers the app in Settings → Apps.

Windows may show a SmartScreen warning if you share the built installer with someone else, because it isn't code-signed. Files you build on your own machine normally don't trigger it.

<details> <summary>Build by hand (what <code>build.bat</code> does)</summary>
bat
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set REFS=/reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll

rem 1) the app
%CSC% /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:app.ico /out:AutoThemeSwitcher.exe %REFS% AutoThemeSwitcher.cs Fluent.cs

rem 2) the installer, with the app embedded as a resource
%CSC% /nologo /target:winexe /optimize+ /codepage:65001 /win32icon:app.ico /out:AutoThemeSwitcher_Setup.exe /resource:AutoThemeSwitcher.exe,AppPayload %REFS% Setup.cs Fluent.cs
</details>
Using it

Open Auto Theme Switcher from the Start menu (or double-click the tray icon).

Setting	What it does
Switch to Light mode at	The time Windows should go Light
Switch to Dark mode at	The time Windows should go Dark
Enable automatic switching	Turns the schedule on or off without losing your times
Start with Windows	Runs quietly in the tray at every login

To set a time, click the hour or the minute, then use the arrows, the mouse wheel, or just type the digits.

Hit Save and apply and it takes effect immediately. Closing the window doesn't quit the app. To exit completely, right-click the tray icon and choose Exit.

One thing I care about

If you flip the theme by hand in the middle of the day, the app doesn't fight you. It only changes the theme at the moment a scheduled time is crossed, not every few seconds. Your manual choice stays until the next switch.

How it works

Nothing fancy:

Windows stores the theme in two registry values under HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize: AppsUseLightTheme and SystemUsesLightTheme (1 = light, 0 = dark).
The app checks the clock every 20 seconds. When the target mode changes, it writes both values and broadcasts a WM_SETTINGCHANGE ("ImmersiveColorSet") message so open apps update right away.
My own settings live in HKCU\Software\AutoThemeSwitcher. Autostart is a standard HKCU\...\Run entry.
Everything is per-user, so no admin rights and nothing system-wide is touched.
Uninstall

Use Settings → Apps → Auto Theme Switcher → Uninstall, or run Uninstall.exe from the install folder. It removes the app, the shortcuts, the autostart entry and my registry settings. Your Windows theme stays however it was last set.

FAQ

The build fails or says it can't find csc.exe. It needs .NET Framework 4, which is built into Windows 10 and 11. If something else goes wrong, open an issue and paste the console output.

Some apps didn't change theme. A few apps only read the theme when they start. Restart them and they'll pick it up.

It didn't switch at the exact minute. It checks every 20 seconds, so expect it within that window.

It doesn't start with Windows. Make sure Start with Windows is on, and check Task Manager → Startup apps in case Windows disabled it.

I can't see the tray icon. Click the ^ arrow next to the clock. You can drag the icon out to keep it visible.

Project layout
File	What it is
AutoThemeSwitcher.cs	The app: tray icon, scheduler, settings window
Fluent.cs	A small Windows 11-style UI kit (toggle, card, time picker, button) shared by the app and the installer
Setup.cs	The installer and uninstaller in one exe
app.ico	The icon
build.bat	Builds the app, then the installer with the app embedded

It's plain WinForms, written to compile with the C# 5 compiler that comes with Windows, so there are no NuGet packages or external dependencies.

Known limitations
The schedule is fixed times only. There's no sunrise/sunset mode yet.
It doesn't change your wallpaper.
The UI is set up for one display scale at a time, so dragging the window between monitors with different scaling can look slightly soft.
The UI is English only for now.
Ideas for later
Sunrise and sunset based switching
Separate wallpapers for light and dark
A "pause until tomorrow" option in the tray menu
Contributing

Issues and pull requests are welcome. I'd like to keep the project small and dependency-free, so please keep that in mind.

License

MIT. Do whatever you like with it.

<a name="fa"></a>

فارسی

یک برنامه خیلی کوچک برای ویندوز که تم را در ساعت‌هایی که خودتان تعیین می‌کنید بین حالت روشن (Light) و تاریک (Dark) عوض می‌کند. در نوار کنار ساعت (tray) می‌نشیند، همراه ویندوز اجرا می‌شود و بقیه وقت مزاحم کسی نیست.

چرا این را ساختم

می‌خواستم ویندوز عصرها خودش تاریک شود و صبح‌ها خودش روشن، بدون اینکه من دست بزنم. ویندوز این کار را با زمان‌بندی خودش انجام نمی‌دهد، و برنامه‌هایی که انجامش می‌دهند یا بزرگ‌تر از نیاز من بودند یا کارهای بیشتری از چیزی که می‌خواستم می‌کردند.

برای همین کوچک‌ترین چیزی را ساختم که کار را انجام بدهد: دو ساعت انتخاب می‌کنی، ذخیره می‌کنی و فراموشش می‌کنی.

امکانات
ویندوز (و برنامه‌ها) را در ساعت‌های تعیین‌شده به حالت روشن یا تاریک می‌برد
بازه‌هایی که از نیمه‌شب رد می‌شوند هم کار می‌کنند، مثلاً روشن از ۲۲:۰۰ تا ۰۶:۰۰
با ویندوز اجرا می‌شود و بی‌سروصدا در tray می‌ماند
موقع شروع، تم را درست می‌کند؛ یعنی اگر سیستمتان در ساعت تغییر خاموش بوده، با روشن شدن در حالت درست قرار می‌گیرید
پنجره خود برنامه هم از تم و رنگ تاکید ویندوز پیروی می‌کند و با بقیه سیستم هماهنگ است
بدون دسترسی ادمین نصب می‌شود و مثل یک برنامه عادی در Settings → Apps دیده می‌شود
به اینترنت وصل نمی‌شود، چیزی ارسال نمی‌کند و وابستگی بیرونی ندارد
نیازمندی‌ها
ویندوز ۱۰ یا ۱۱
چیز دیگری لازم نیست. برنامه با کامپایلر C# که خود ویندوز دارد ساخته می‌شود، پس نه Visual Studio می‌خواهید، نه .NET SDK و نه دسترسی ادمین.
نصب

عمداً فایل .exe در این مخزن نگذاشتم. ساختنش فقط چند ثانیه طول می‌کشد:

این مخزن را دانلود یا clone کنید
روی build.bat دوبار کلیک کنید
بعد از پایان build، نصب‌کننده (AutoThemeSwitcher_Setup.exe) خودش باز می‌شود
روی Install بزنید

تمام. نصب‌کننده همه‌چیز را در %LocalAppData%\Programs\AutoThemeSwitcher می‌گذارد، یک میان‌بر در منوی Start می‌سازد و برنامه را در Settings → Apps ثبت می‌کند.

اگر نصب‌کننده ساخته‌شده را برای کس دیگری بفرستید، ممکن است ویندوز با SmartScreen هشدار بدهد، چون فایل امضای دیجیتال ندارد. فایلی که خودتان روی همان سیستم می‌سازید معمولاً این هشدار را نمی‌گیرد.

<details> <summary>ساخت دستی (کاری که <code>build.bat</code> انجام می‌دهد)</summary>

دستورها در بخش انگلیسی همین فایل آمده‌اند. خلاصه‌اش این است: اول AutoThemeSwitcher.cs و Fluent.cs را به AutoThemeSwitcher.exe کامپایل می‌کند، بعد Setup.cs و Fluent.cs را با آن exe به‌عنوان resource داخل AutoThemeSwitcher_Setup.exe می‌گذارد.

</details>
استفاده

برنامه Auto Theme Switcher را از منوی Start باز کنید (یا روی آیکون tray دوبار کلیک کنید).

تنظیم	کارش
Switch to Light mode at	ساعتی که ویندوز باید روشن شود
Switch to Dark mode at	ساعتی که ویندوز باید تاریک شود
Enable automatic switching	زمان‌بندی را بدون از دست دادن ساعت‌ها روشن یا خاموش می‌کند
Start with Windows	با هر بار ورود به ویندوز بی‌سروصدا در tray اجرا می‌شود

برای تنظیم ساعت، روی ساعت یا دقیقه کلیک کنید و با فلش‌ها، چرخ ماوس، یا فقط تایپ کردن عددها تغییرش بدهید.

روی Save and apply بزنید تا همان لحظه اعمال شود. بستن پنجره برنامه را نمی‌بندد. برای خروج کامل، روی آیکون tray راست‌کلیک کنید و Exit را بزنید.

یک چیز که برایم مهم بود

اگر وسط روز خودتان دستی تم را عوض کنید، برنامه با شما مبارزه نمی‌کند. فقط در لحظه‌ای که یکی از ساعت‌های تنظیم‌شده رد می‌شود تم را عوض می‌کند، نه هر چند ثانیه یک بار. پس انتخاب دستی شما تا سوییچ بعدی می‌ماند.

چطور کار می‌کند

چیز پیچیده‌ای نیست:

ویندوز تم را در دو مقدار رجیستری زیر مسیر HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize نگه می‌دارد: AppsUseLightTheme و SystemUsesLightTheme (مقدار 1 یعنی روشن و 0 یعنی تاریک).
برنامه هر ۲۰ ثانیه ساعت را چک می‌کند. وقتی حالت مطلوب عوض شود، هر دو مقدار را می‌نویسد و یک پیام WM_SETTINGCHANGE (با پارامتر "ImmersiveColorSet") پخش می‌کند تا برنامه‌های باز هم فوری تغییر کنند.
تنظیمات خود برنامه در HKCU\Software\AutoThemeSwitcher ذخیره می‌شود و اجرای خودکار هم یک ورودی استاندارد HKCU\...\Run است.
همه‌چیز مخصوص کاربر فعلی است، پس نه دسترسی ادمین می‌خواهد و نه چیزی در سطح کل سیستم را تغییر می‌دهد.
حذف برنامه

از Settings → Apps → Auto Theme Switcher → Uninstall استفاده کنید، یا Uninstall.exe را از پوشه نصب اجرا کنید. برنامه، میان‌برها، ورودی اجرای خودکار و تنظیمات من در رجیستری را پاک می‌کند. تم ویندوز شما همان‌طور که آخرین بار تنظیم شده بود می‌ماند.

سؤال‌های رایج

build خطا می‌دهد یا می‌گوید csc.exe پیدا نشد. به .NET Framework 4 نیاز دارد که در ویندوز ۱۰ و ۱۱ از قبل هست. اگر مشکل چیز دیگری بود، یک issue باز کنید و متن خروجی کنسول را بگذارید.

بعضی برنامه‌ها تمشان عوض نشد. تعداد کمی از برنامه‌ها تم را فقط موقع اجرا می‌خوانند. آن‌ها را ببندید و دوباره باز کنید.

دقیقاً سر دقیقه تغییر نکرد. برنامه هر ۲۰ ثانیه چک می‌کند، پس تغییر در همین بازه اتفاق می‌افتد.

با ویندوز اجرا نمی‌شود. مطمئن شوید Start with Windows روشن است و در Task Manager → Startup apps بررسی کنید که ویندوز آن را غیرفعال نکرده باشد.

آیکون tray را نمی‌بینم. روی فلش ^ کنار ساعت کلیک کنید. می‌توانید آیکون را بیرون بکشید تا همیشه دیده شود.

ساختار پروژه
فایل	چیست
AutoThemeSwitcher.cs	خود برنامه: آیکون tray، زمان‌بند و پنجره تنظیمات
Fluent.cs	یک کیت کوچک رابط کاربری به سبک ویندوز ۱۱ (toggle، کارت، انتخاب ساعت، دکمه) که هم برنامه و هم نصب‌کننده ازش استفاده می‌کنند
Setup.cs	نصب‌کننده و حذف‌کننده در یک exe
app.ico	آیکون
build.bat	اول برنامه را می‌سازد، بعد نصب‌کننده را با برنامه داخلش

کد WinForms ساده است و طوری نوشته شده که با کامپایلر C# 5 داخل ویندوز کامپایل شود، پس هیچ بسته NuGet یا وابستگی بیرونی ندارد.

محدودیت‌های فعلی
زمان‌بندی فقط با ساعت ثابت است. هنوز حالت طلوع و غروب خورشید ندارد.
والپیپر را عوض نمی‌کند.
رابط کاربری برای یک مقیاس صفحه‌نمایش در هر لحظه آماده شده؛ پس اگر پنجره را بین مانیتورهایی با مقیاس متفاوت جابه‌جا کنید ممکن است کمی کم‌شارپ‌تر دیده شود.
فعلاً رابط کاربری فقط انگلیسی است.
ایده‌ها برای بعد
تغییر تم بر اساس طلوع و غروب خورشید
والپیپر جدا برای حالت روشن و تاریک
گزینه «تا فردا متوقف شود» در منوی tray
مشارکت

issue و pull request خوش‌آمد است. دوست دارم پروژه کوچک و بدون وابستگی بماند، پس لطفاً این را در نظر داشته باشید.

لایسنس

MIT. هر کاری خواستید با آن بکنید.
