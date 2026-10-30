# Auto Theme Switcher | تغییر خودکار تم ویندوز

[فارسی](#fa) | [English](#en)

<a name="fa"></a>

Auto Theme Switcher یک برنامه کوچک و سبک برای ویندوز است که تم سیستم را در ساعت‌هایی که خودتان تعیین می‌کنید به‌صورت خودکار بین حالت **روشن (Light)** و **تاریک (Dark)** تغییر می‌دهد و در صورت تمایل تصویر زمینه را هم همزمان عوض می‌کند. برنامه در نوار کنار ساعت (System Tray) اجرا می‌شود، همراه ویندوز بالا می‌آید و بقیه وقت مزاحم کار شما نیست.

## 📌 معرفی پروژه

ویندوز به‌صورت پیش‌فرض امکان زمان‌بندی تم روشن و تاریک را ندارد، و بسیاری از برنامه‌هایی که این کار را انجام می‌دهند بزرگ‌تر از نیاز یک کاربر عادی هستند یا امکاناتی دارند که لازم نیست.

Auto Theme Switcher با یک ایده ساده ساخته شده است: دو ساعت انتخاب می‌کنید، ذخیره می‌کنید و بعد از آن دیگر نیازی نیست به چیزی دست بزنید. عصر ویندوز خودش تاریک می‌شود و صبح خودش روشن.

## ✨ امکانات

- 🌗 تغییر خودکار تم ویندوز و برنامه‌ها بین حالت روشن و تاریک بر اساس ساعت
- 🌙 پشتیبانی از بازه‌هایی که از نیمه‌شب رد می‌شوند (مثلاً روشن از ۲۲:۰۰ تا ۰۶:۰۰)
- 🖼️ تغییر خودکار تصویر زمینه همزمان با تم، با انتخاب یک عکس برای حالت روشن و یک عکس برای حالت تاریک
- 🚀 اجرای خودکار همراه با ویندوز
- 🔄 اعمال تم درست هنگام روشن شدن سیستم، حتی اگر ساعت تغییر را از دست داده باشید
- ✋ احترام به تغییر دستی: اگر خودتان تم را عوض کنید، تا ساعت بعدی دست نمی‌زند
- 🧩 اجرا در System Tray و باز شدن پنجره با دوبار کلیک روی آیکون
- 🎨 رابط کاربری به سبک ویندوز ۱۱ که با تم و رنگ تاکید (Accent) سیستم هماهنگ می‌شود
- 📦 نصب‌کننده کامل با میان‌بر Start و ثبت در Settings → Apps
- 🗑️ حذف‌کننده تمیز که برنامه، میان‌برها و تنظیمات را پاک می‌کند
- 🔒 بدون نیاز به دسترسی ادمین، بدون اتصال به اینترنت و بدون ارسال هیچ داده‌ای
- 🖥️ پشتیبانی از نمایشگرهای High-DPI

## 🎯 بخش‌های برنامه

برنامه شامل بخش‌های زیر است:

- پنجره تنظیمات
  - انتخاب ساعت شروع حالت روشن
  - انتخاب ساعت شروع حالت تاریک
  - روشن و خاموش کردن تغییر خودکار
  - اجرای خودکار همراه با ویندوز
  - نمایش حالت فعلی ویندوز و ساعت تغییر بعدی
  - صفحه Wallpaper: پیش‌نمایش عکس‌ها، انتخاب عکس و حالت نمایش (Fill، Fit، Stretch، Center، Tile)
  - صفحه About: نسخه و مسیر نصب
- منوی System Tray
  - باز کردن تنظیمات
  - خروج کامل از برنامه
- نصب‌کننده و حذف‌کننده
  - نصب بدون دسترسی ادمین
  - میان‌بر منوی Start و دسکتاپ
  - به‌روزرسانی روی نسخه قبلی با حفظ تنظیمات

<img
  src="./docs/screenshot.png"
  alt="پیش‌نمایش برنامه Auto Theme Switcher"
  width="420"
/>

## 🛠️ تکنولوژی‌های استفاده‌شده

- C# (سازگار با C# 5)
- .NET Framework 4 و Windows Forms
- GDI+ (رسم کنترل‌های سفارشی)
- Windows Registry
- Win32 API (پیام WM_SETTINGCHANGE و DWM)
- Windows Script Host (ساخت میان‌بر)
- Batch Script

## ⚙️ نحوه عملکرد

- ویندوز تم را در دو مقدار رجیستری `AppsUseLightTheme` و `SystemUsesLightTheme` زیر مسیر `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` نگه می‌دارد (مقدار `1` یعنی روشن و `0` یعنی تاریک).
- برنامه هر ۲۰ ثانیه ساعت را بررسی می‌کند. فقط وقتی حالت مطلوب عوض شود، هر دو مقدار را می‌نویسد و پیام `WM_SETTINGCHANGE` را پخش می‌کند تا برنامه‌های باز هم فوراً تغییر کنند.
- تنظیمات برنامه در `HKCU\Software\AutoThemeSwitcher` ذخیره می‌شود و اجرای خودکار با یک ورودی استاندارد `HKCU\...\Run` انجام می‌شود.
- عکس‌های انتخاب‌شده برای تصویر زمینه در پوشه داده برنامه کپی می‌شوند و همزمان با تغییر تم با تابع `SystemParametersInfo` اعمال می‌شوند؛ پس جابه‌جا یا حذف شدن فایل اصلی مشکلی ایجاد نمی‌کند.
- همه‌چیز مخصوص کاربر فعلی است و چیزی در سطح کل سیستم تغییر نمی‌کند.

## 🎨 طراحی رابط کاربری

رابط کاربری با الهام از اصول Fluent Design ویندوز ۱۱ طراحی شده است و تمرکز اصلی روی موارد زیر بوده است:

- کارت‌های ساده با گوشه‌های گرد و خط جداکننده، مشابه صفحه تنظیمات ویندوز
- ناوبری کناری (General، Wallpaper، About) و پیش‌نمایش واقعی عکس‌های تصویر زمینه
- انیمیشن‌های نرم: لغزیدن کلید روشن/خاموش و تغییر رنگ دکمه‌ها هنگام hover
- کلیدهای روشن/خاموش (Toggle Switch) به‌جای چک‌باکس
- انتخاب‌گر ساعت اختصاصی با امکان تایپ مستقیم، فلش‌ها و چرخ ماوس
- هماهنگی کامل با تم روشن/تاریک و رنگ تاکید سیستم؛ ظاهر خود برنامه هم همزمان با تغییر تم عوض می‌شود
- هماهنگ‌سازی نوار عنوان پنجره با تم
- رسم واضح و شارپ روی نمایشگرهای High-DPI
- فضای کافی، فاصله‌گذاری منظم و متن خوانا
- پشتیبانی از کیبورد (Tab، Space، فلش‌ها)

## 📂 ساختار پروژه

```text
auto-theme-switcher/
│
├── AutoThemeSwitcher.cs   # خود برنامه: Tray، زمان‌بند و پنجره تنظیمات
├── Fluent.cs              # کنترل‌ها و رنگ‌های سبک ویندوز ۱۱
├── Setup.cs               # نصب‌کننده و حذف‌کننده
├── app.ico                # آیکون برنامه
├── build.bat              # ساخت برنامه و نصب‌کننده
├── docs/
│   └── screenshot.png
└── README.md
```

## 🚀 اجرای پروژه

ابتدا Repository را Clone کنید:

```bash
git clone https://github.com/your-username/auto-theme-switcher.git
```

و وارد پوشه پروژه شوید:

```bash
cd auto-theme-switcher
```

سپس روی فایل `build.bat` دوبار کلیک کنید. بعد از چند ثانیه فایل `AutoThemeSwitcher_Setup.exe` ساخته می‌شود و پنجره نصب خودکار باز می‌شود.

برای ساخت برنامه به Visual Studio یا .NET SDK نیازی نیست، چون از کامپایلر C# که همراه خود ویندوز است استفاده می‌شود. فقط ویندوز ۱۰ یا ۱۱ لازم است.

> فایل نصب ساخته‌شده امضای دیجیتال ندارد. اگر آن را برای کس دیگری بفرستید ممکن است ویندوز با SmartScreen هشدار بدهد؛ فایلی که خودتان روی همان سیستم می‌سازید معمولاً این هشدار را نمی‌گیرد.

## 🗑️ حذف برنامه

از مسیر **Settings → Apps → Auto Theme Switcher → Uninstall** استفاده کنید، یا فایل `Uninstall.exe` را از پوشه نصب (`%LocalAppData%\Programs\AutoThemeSwitcher`) اجرا کنید. برنامه، میان‌برها، ورودی اجرای خودکار و تنظیمات ذخیره‌شده پاک می‌شوند. عکس‌های کپی‌شده برای تصویر زمینه هم پاک می‌شوند، به‌جز عکسی که ویندوز همان لحظه نشان می‌دهد، تا دسکتاپ شما خالی نماند. تم ویندوز هم همان‌طور که آخرین بار تنظیم شده بود می‌ماند.

## ❓ سؤال‌های رایج

**بعضی برنامه‌ها تمشان عوض نشد؟**
تعداد کمی از برنامه‌ها تم را فقط موقع اجرا می‌خوانند. آن‌ها را ببندید و دوباره باز کنید.

**برنامه با ویندوز اجرا نمی‌شود؟**
مطمئن شوید گزینه **Start with Windows** روشن است و در Task Manager بخش Startup apps بررسی کنید که ویندوز آن را غیرفعال نکرده باشد.

**آیکون برنامه را نمی‌بینم؟**
روی فلش ^ کنار ساعت کلیک کنید. می‌توانید آیکون را بیرون بکشید تا همیشه دیده شود.

**تصویر زمینه عوض نمی‌شود؟**
مطمئن شوید کلید «Change wallpaper with the theme» روشن است و برای هر دو حالت یک عکس انتخاب کرده‌اید. اگر تصویر زمینه ویندوز روی Slideshow یا Windows Spotlight باشد، با فعال شدن این قابلیت به حالت Picture برمی‌گردد.

## 🔮 قابلیت‌های آینده

در نسخه‌های بعدی می‌توان امکانات زیر را اضافه کرد:

- تغییر تم بر اساس طلوع و غروب خورشید
- تصویر زمینه جدا برای هر مانیتور
- گزینه «توقف تا فردا» در منوی Tray
- پشتیبانی از زبان فارسی در خود برنامه و انتخاب زبان
- انتشار فایل نصب آماده در بخش Releases
- امضای دیجیتال نصب‌کننده

## 📜 نسخه‌ها

همه نسخه‌ها و فایل‌های نصب در بخش [Releases](../../releases) هستند.

- **v1.1**: تغییر خودکار تصویر زمینه، ناوبری کناری (General، Wallpaper، About)، پیش‌نمایش عکس‌ها و انیمیشن‌های نرم
- **v1.0**: نسخه اول: زمان‌بندی تم روشن و تاریک، اجرا در System Tray، اجرای خودکار با ویندوز، رابط به سبک ویندوز ۱۱، نصب‌کننده و حذف‌کننده

## 🎯 هدف پروژه

این پروژه با هدف ساخت یک ابزار کوچک و واقعاً کاربردی ایجاد شده است و در عین حال نمونه‌ای از **توسعه برنامه دسکتاپ ویندوز، طراحی رابط کاربری به سبک Fluent، کار با Win32 API و رجیستری، و ساخت نصب‌کننده سفارشی** است.

Auto Theme Switcher نشان می‌دهد که می‌شود یک مشکل روزمره را با ابزاری ساده، سبک و بدون وابستگی حل کرد.

## 📄 مجوز

این پروژه تحت مجوز MIT منتشر شده است. هر کاری خواستید با آن بکنید.


---

<a name="en"></a>

# Auto Theme Switcher | English

[فارسی](#fa) | English

Auto Theme Switcher is a small, lightweight Windows app that automatically switches the system theme between **Light** and **Dark** mode at the times you choose, and can change your wallpaper at the same time. It runs in the system tray, starts with Windows, and otherwise stays out of your way.

## 📌 About the Project

Windows has no built-in way to schedule Light and Dark mode, and many of the apps that do are bigger than the average user needs or come with features nobody asked for.

Auto Theme Switcher is built around one simple idea: pick two times, save, and never touch it again. Windows goes dark in the evening and light in the morning, on its own.

## ✨ Features

- 🌗 Automatically switches Windows and apps between Light and Dark mode based on the time
- 🌙 Supports ranges that cross midnight (for example, Light from 22:00 to 06:00)
- 🖼️ Changes the wallpaper together with the theme: pick one image for Light mode and one for Dark mode
- 🚀 Starts automatically with Windows
- 🔄 Applies the right theme at startup, even if your PC was off at switch time
- ✋ Respects manual changes: if you change the theme yourself, it leaves it alone until the next scheduled time
- 🧩 Runs in the system tray; double-click the icon to open the settings
- 🎨 Windows 11-style interface that follows the system theme and accent color
- 📦 Full installer with a Start menu shortcut and an entry in Settings → Apps
- 🗑️ Clean uninstaller that removes the app, shortcuts and settings
- 🔒 No admin rights, no internet access, no data sent anywhere
- 🖥️ High-DPI display support

## 🎯 App Components

The app consists of the following parts:

- Settings window
  - Choose when Light mode starts
  - Choose when Dark mode starts
  - Turn automatic switching on or off
  - Start with Windows
  - Shows the current Windows mode and the next switch time
  - Wallpaper page: image previews, image picker and picture fit (Fill, Fit, Stretch, Center, Tile)
  - About page: version and install location
- System tray menu
  - Open settings
  - Exit completely
- Installer and uninstaller
  - Installs without admin rights
  - Start menu and desktop shortcuts
  - Updates over a previous version and keeps your settings

<img
  src="./docs/screenshot.png"
  alt="Auto Theme Switcher preview"
  width="420"
/>

## 🛠️ Tech Stack

- C# (C# 5 compatible)
- .NET Framework 4 and Windows Forms
- GDI+ (custom-drawn controls)
- Windows Registry
- Win32 API (WM_SETTINGCHANGE message and DWM)
- Windows Script Host (shortcut creation)
- Batch script

## ⚙️ How It Works

- Windows stores the theme in two registry values, `AppsUseLightTheme` and `SystemUsesLightTheme`, under `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` (`1` = light, `0` = dark).
- The app checks the clock every 20 seconds. Only when the target mode changes does it write both values and broadcast a `WM_SETTINGCHANGE` message so open apps update immediately.
- App settings are stored in `HKCU\Software\AutoThemeSwitcher`, and autostart is a standard `HKCU\...\Run` entry.
- Wallpaper images you pick are copied into the app's data folder and applied with `SystemParametersInfo` at the same moment the theme switches, so moving or deleting the original file doesn't break anything.
- Everything is per-user; nothing system-wide is changed.

## 🎨 UI Design

The interface is inspired by the Fluent Design principles of Windows 11, with a focus on:

- Simple rounded cards with dividers, similar to the Windows Settings page
- A sidebar (General, Wallpaper, About) and real previews of your wallpaper images
- Smooth animations: the toggle knob slides and buttons fade on hover
- Toggle switches instead of checkboxes
- A custom time picker that supports direct typing, arrow buttons and the mouse wheel
- Full theme awareness: it follows Light/Dark mode and the system accent color, and the app's own look changes together with the theme
- Title bar that matches the theme
- Sharp rendering on High-DPI displays
- Generous spacing and readable text
- Keyboard support (Tab, Space, arrow keys)

## 📂 Project Structure

```text
auto-theme-switcher/
│
├── AutoThemeSwitcher.cs   # The app: tray icon, scheduler, settings window
├── Fluent.cs              # Windows 11-style controls and colors
├── Setup.cs               # Installer and uninstaller
├── app.ico                # App icon
├── build.bat              # Builds the app and the installer
├── docs/
│   └── screenshot.png
└── README.md
```

## 🚀 Getting Started

First, clone the repository:

```bash
git clone https://github.com/your-username/auto-theme-switcher.git
```

and go into the project folder:

```bash
cd auto-theme-switcher
```

Then double-click `build.bat`. After a few seconds, `AutoThemeSwitcher_Setup.exe` is created and the installer opens automatically.

You don't need Visual Studio or the .NET SDK to build it, because it uses the C# compiler that ships with Windows. All you need is Windows 10 or 11.

> The built installer isn't code-signed. If you send it to someone else, Windows may show a SmartScreen warning; a file you build yourself on the same machine normally doesn't trigger it.

## 🗑️ Uninstall

Go to **Settings → Apps → Auto Theme Switcher → Uninstall**, or run `Uninstall.exe` from the install folder (`%LocalAppData%\Programs\AutoThemeSwitcher`). The app, shortcuts, autostart entry and saved settings are removed. The copied wallpaper images are removed too, except the one Windows is showing right now, so your desktop doesn't go blank. Your Windows theme stays however it was last set.

## ❓ FAQ

**Some apps didn't change theme?**
A few apps only read the theme when they start. Close and reopen them.

**It doesn't start with Windows?**
Make sure **Start with Windows** is on, and check Task Manager → Startup apps in case Windows disabled it.

**I can't see the tray icon?**
Click the ^ arrow next to the clock. You can drag the icon out to keep it visible.

**The wallpaper doesn't change?**
Make sure "Change wallpaper with the theme" is on and that you picked an image for both modes. If your Windows wallpaper is set to Slideshow or Windows Spotlight, turning this feature on switches it to a single Picture.

## 🔮 Future Plans

Possible additions in future versions:

- Sunrise and sunset based switching
- A different wallpaper per monitor
- A "pause until tomorrow" option in the tray menu
- Persian language support inside the app, with a language switch
- Publishing a ready-made installer in Releases
- Code-signing the installer

## 📜 Versions

All versions and ready-made installers are in the [Releases](../../releases) section.

- **v1.1**: wallpaper switching, sidebar navigation (General, Wallpaper, About), image previews and smooth animations
- **v1.0**: first version: Light/Dark scheduling, system tray, autostart, Windows 11-style UI, installer and uninstaller

## 🎯 Project Goal

This project was created to build a small tool that is genuinely useful, and at the same time to serve as an example of **Windows desktop app development, Fluent-style UI design, working with the Win32 API and the registry, and building a custom installer**.

Auto Theme Switcher shows that an everyday annoyance can be solved with a simple, lightweight tool that has no dependencies.

## 📄 License

This project is released under the MIT license. Do whatever you like with it.
