---
uid: icons
---

# Icons

Icons can be used in C# scripts:
- Toolbars and menus (classes `toolbar`, `popupMenu`)
- WPF windows (classes `wpfBuilder`, `ImageUtil`)
- Standard dialog windows (class `dialog`)
- Tray icon (classes `trayIcon`, `script`)
- Icon in OSD (class `osdText`)

Also in LibreAutomate UI etc:
- Icons of scripts etc displayed in the **Files** panel and elsewhere
- Icons of LA toolbar buttons (menu **Tools > Customize**)
- Icons of generated EXE files (menu **File > Properties**)

Most of the above features support vector icons found in menu **Tools > Icons**.

Some of the above features support classic icons (eg from ICO or EXE files). To get them use class [icon](). Menus and toolbars can automatically get icons for scripts and files used in button/item action code.

Many of the above features use class [IconImageCache]() to cache icons, because finding/extracting them is slow. To clear the cache: menu **Tools > Update icons**. It updates icons in toolbars etc.

## The **Icons** tool

LibreAutomate installs a database file `icons.db` with about 60000 vector icons from [MahApps.Metro.IconPacks](https://github.com/MahApps/MahApps.Metro.IconPacks). You can see them all in the **Icons** tool (menu **Tools > Icons**). A list item displays icon image, name, and icon pack name in parentheses.

Use the search field to find icons. Type a word, and the list displays icons with the word in icon name. Case-sensitive if the search text contains upper-case characters. Also supports [wildcard expression](xref:wildcard_expression) (if contains `*` or `?`) and `Pack.Name`.

You can use the **AI search** button to find icons by name or image using AI. More info: [LA and AI](xref:ai).

All these icons are monochrome. Select a color in the color tool.

Use the **Set file icon** section to assign the selected icon to the file (script etc) or folder currently selected in the `Files` panel. Or you can assign the icon to all files of that kind.

Use the **Menu/toolbar/etc icon** section to assign the selected icon to the toolbar/menu item at the text cursor position in the code editor. Or copy the icon name or XAML to the clipboard. Also use it when working with the **Customize** tool.

Use the **Export to current workspace folder** section to save the selected icon as an ICO or XAML file in the current folder in the workspace (**Files** panel).

Use the **Custom icon string** section to adjust some parameters of the selected icon or/and compose a multi-layer multi-color icon from 2 or more icons.

In scripts these icons are referenced by an *icon name* string like `"*Pack.Name color"`. It includes the pack name, icon name, optionally color (default black). Also optionally can include size, margins, multiple icons (as layers), etc. The syntax is documented in article [ImageUtil.LoadWpfImageElement](). The **Name** button in the **Menu/toolbar/etc icon** section copies the string to the clipboard.

Use the **Options** section to set options for the **Icons** tool:
- **High contrast color** - append the specified color to icon names. It will be used in high-contrast dark mode.
- **List background** - background color for icon images in the list.
- **List image size** - change the size (default 16) of icons in the list. Note, it does not change the size of the copied icon; it's always 16; the run-time size depends on where and how the icon is used.