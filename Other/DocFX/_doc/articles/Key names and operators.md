---
uid: key_names
---

# Key names and operators

This string syntax is used with [keys.send]() and other keyboard functions of this library. A string can contain one or more US keyboard key names separated with a space or operator like `+`.

Example:

```csharp
keys.send("A F2 Ctrl+Shift+A Enter*2"); //keys A, F2, Ctrl+Shift+A, Enter Enter
```

## Key names

### Named keys
- **Modifier:** `Alt`, `Ctrl`, `Shift`, `Win`, `RAlt`, `RCtrl`, `RShift`, `RWin`
- **Navigate:** `Esc`, `End`, `Home`, `PgDn`, `PgUp`, `Down`, `Left`, `Right`, `Up`
- **Other:** `Back`, `Delete`, `Enter`, `Apps`, `Pause`, `PrtSc`, `Space`, `Tab`
- **Function:** `F1`-`F24`
- **Lock:** `CapsLock`, `NumLock`, `ScrollLock`, `Insert`

A key name must start with an uppercase character. Other characters - any case. Only the first 3 characters are significant. For example, for `"Back"` you can also use `"Bac"`, `"Backspace"` or `"BACK"`.

Alias: `AltGr`=`RAlt`, `Menu`=`Apps`, `PageDown`=`PgDn`, `PD`=`PgDn`, `PageUp`=`PgUp`, `PU`=`PgUp`, `PrintScreen`=`PrtSc`, `PS`=`PrtSc`, `BS`=`Back`, `PB`=`Pause`, `CL`=`CapsLock`, `NL`=`NumLock`, `SL`=`ScrollLock`, `HM`=`Home`.

### Text keys
- **Alphabetic:** `A`-`Z` or `a`-`z` (case-insensitive)
- **Number:** `0`-`9`
- **Numeric keypad:** `#/`, `#*`, `#-`, `#+`, `#.`, `#0`-`#9`
- **Other:** `` ` ``, `-`, `=`, `[`, `]`, `\`, `;`, `'`, `,`, `.`, `/`

Note: these are key names on US keyboard. They may not match key names on your keyboard. To specify characters instead, use operators (see below). Also in LibreAutomate you can use the input recorder.

Only uppercase A-Z must be separated with spaces. Example of a valid sequence: `"A B ab"`.

Alias: `~`=`` ` ``, `{`=`[`, `}`=`]`, `|`=`\`, `:`=`;`, `"`=`'`, `<`=`,`, `>`=`.`, `?`=`/`.

### Other keys
- Names of enum [KKey]() members. Example: `keys.send("BrowserBack MediaNextTrack VolumeUp IMEKanaMode");`
- Virtual-key codes. Prefix `VK` or `Vk`. Example: `keys.send("VK65 VK0x42");`
- Unavailable: `Fn` (hardware-only key).

### Special characters
- **Operator:** `+`, `*`, `(`, `)`, `_`, `^`
- **Numpad key prefix:** `#`
- **Text/HTML argument prefix:** `!`, `%`
- **Reserved:** `@`, `$`, `&`

These characters cannot be used as key names. Instead use key names listed in the "Text keys" section.

## Operators

| Operator | Examples | Description |
| --- | --- | --- |
| `+` | `"Ctrl+Shift+A"`<br>`"Alt+E+P"` | Hotkey. Like `"Ctrl*down Shift*down A Shift*up Ctrl*up"` and `"Alt*down E*down P E*up Alt*up"`. |
| `+()` | `"Alt+(E P)"` | Hotkey. Like `"Alt*down E P Alt*up"`.<br>Inside `()` cannot be used operators `+`, `+()` and `^`. |
| `*down` | `"Ctrl*down"` | Press key and don't release. |
| `*up` | `"Ctrl*up"` | Release key. |
| `*n` | `"Left*3"`<br>`$"Left*{i}"` | Press key n times, like `"Left Left Left"`.<br>See [keys.AddRepeat](). |
| `_` | `"Tab _A_b Tab"`<br>`"Alt+_e_a"`<br>`"_**20"` | Send next character like text with option [OKeyText.KeysOrChar]().<br>Can be used to `Alt`-select items in menus, ribbons and dialogs regardless of current keyboard layout. |
| `^` | `"Alt+^ea"` | Send all remaining characters and whitespace like text with option [OKeyText.KeysOrChar]().<br>For example `"Alt+^ed b"` is the same as `"Alt+_e_d Space _b"`.<br>`Alt` is released after the first character. Don't use other modifiers. |

Operators and related keys can be in separate arguments. Examples: `keys.send("Shift+", KKey.A); keys.send(KKey.A, "*3");`.

### Raw text

The primary way to send raw text using `keys.send` - use a separate string argument with prefix `!`. Example: `keys.send("!User", "Tab", "!" + clipboard.text, "Tab Enter");`.

Unlike operators `^` and `_`:
- Uses option `opt.key.TextHow` (keys/characters/paste). The default is characters.
- Supports 2-char Unicode characters.

To paste HTML, use prefix `%` instead.
