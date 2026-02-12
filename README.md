# 🎮 Pokerogue Xbox WebView App (Dev Mode)

A lightweight **Xbox Dev Mode** UWP app that runs **Pokerogue** in a full-screen, controller-friendly kiosk experience using **WebView2**.

Designed to feel like a native Xbox game rather than a browser where possible.

<img width="1240" height="1240" alt="rogue (2)" src="https://github.com/user-attachments/assets/4119311a-04ac-4d03-b463-af90aad78b7b" />


---

## ✨ What this app does

- 🚀 **Launches directly into Pokerogue**
- 🖥️ **Runs in full-screen** (no borders, no safe-area padding)
- 🧹 **No browser UI** (no menus, popups, dev tools, or zoom)
- 🎮 **Hides the mouse cursor once ingame**
- 🔒 **Prevents accidental exits**

---

## 💥 First load crash (why it happens)

Pokerogue may crash on the first load

This is expected behaviour

It happens while the game caches its assets locally for the first time

After this:

Re-open the game

Pokerogue should then launch normally

---

## 🎯 Controller & navigation behaviour

- Xbox **B / Back button is completely disabled**
- No browser shortcuts or keyboard commands work
- All focus is pushed directly into the game

This prevents:
- accidental navigation
- leaving the game unintentionally
- browser overlays appearing

---

## 🚪 Hidden force exit combo!

To close the app safely (without a visible exit button):

**Hold the following buttons for ~1 second:**

- `LB` + `RB` + `D-Pad Down`

This exits the app cleanly.

## 🧱 Kiosk-style restrictions

The app deliberately disables:

- Dev tools
- Context menus
- Popups / new windows
- Autofill & password saving
- Zoom controls
- Browser accelerator keys

Any attempted popup or new window is opened **inside the same view** instead.

---

## ⚠️ Notes

- This app is **not affiliated** with Pokerogue
- Requires **Xbox Developer Mode**
- Uses **WebView2 (UWP)**

---

Enjoy 🎉
