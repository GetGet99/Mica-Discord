# Mica-Discord

An application with Mica background using transparent-background WebView2 to host webpage, and go to discord and inject some basic CSS.

# How to use?
1. Download Evergreen WebView2 and .NET 6 Desktop Runtime if you haven't already
- Evergreen https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section
- .NET 6 Desktop Runtime https://dotnet.microsoft.com/en-us/download/dotnet/6.0
2. Go to the release page, download it, and double click Mica Discord.exe (OR you can download the source and compile it yourself since the dev is ~lazy~ I mean busy.)

# Disclaimer
If you plan to use this with replace background enabled, I'm not sure if it violates Discord TOS or not. I'm not a lawyer. But here's how it works
1. The app itself is basically like a web browser, hosting the web content in WebView2 that points to discord.com
2. It injects this code.
```javascript
(function () {
    let s = document.createElement('style');
    s.innerHTML = `
*{
    --background-primary: #fff0;
    --background-secondary: rgba(50,50,50,0.25);
    --background-secondary-alt: rgba(50,50,50,0.25);
    --background-tertiary: #fff0;
}
`.trim();
    document.head.appendChild(s);
```
If you use any part of this software, it's your responsibility if you get banned. But the app is not harmful itself and the security should as safe as other apps using Microsoft Edge WebView2.
