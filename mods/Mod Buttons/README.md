# ModButtons: The Action Hub Mod

**ModButtons** is a lightweight, centralized action hub for Unity games. It dynamically injects a custom menu popup that allows mods to cleanly integrate their own interactive UI buttons. 

Instead of writing complex Harmony UI patches for every single mod you create, you simply provide a JSON file and your target methods. `ModButtons` handles all the UI generation, layout, visual styling, and asynchronous icon loading.

**Note:** `ModButtons` is entirely standalone. While it is fully compatible with the `ModMenu` framework and will position itself cleanly next to it if detected, `ModMenu` is **NOT** required.

---

## 🛠️ How to Add Your Own Buttons

Adding buttons to the hub requires absolutely zero C# UI code. You just need to create a JSON file in your mod's directory that tells `ModButtons` what methods to execute.

### 1. The Directory Structure
Create a `ModButtons` folder in your mod's root directory. Place your `buttons.json` and any optional icons directly inside it:

```
YourModFolder/
├── ModButtons/
│   ├── buttons.json      <-- Your button configuration
│   └── money_icon.png    <-- (Optional) Button icons
└── YourMod.dll
```

### 2. The `buttons.json` File
This JSON file is a simple array of actions. An action with no `inputs` remains a compact button. An action with inputs is rendered as an associated form, so the player can choose values before pressing its button.

```json
[
  {
    "label": "Add Yen",
    "codeLabel": "YourMod.Cheat.AddMoney",
    "tooltip": "Add the selected amount of yen.",
    "icon": "",
    "assembly": "YourAwesomeMod",
    "class": "YourAwesomeMod.CheatManager",
    "method": "AddMoney",
    "inputs": [
      {
        "id": "amount",
        "type": "integer",
        "label": "Amount",
        "default": 1000000,
        "min": 1,
        "max": 1000000000
      }
    ]
  },
  {
    "label": "Restore Idol Stamina",
    "codeLabel": "YourMod.Cheat.RestoreStamina",
    "icon": "",
    "assembly": "YourAwesomeMod",
    "class": "YourAwesomeMod.CheatManager",
    "method": "RestoreStamina"
  }
]
```

**JSON Properties:**
* **`label`**: (Required) The fallback text displayed on the button or tooltip.
* **`codeLabel`**: (Optional) The translation key used for localizing this button's text (see Localization below).
* **`icon`**: (Optional) The exact filename of the PNG to use as the button graphic. Must be in the `ModButtons` folder alongside the JSON. 
* **`assembly`**: (Required) The name of your compiled DLL (without the `.dll` extension).
* **`class`**: (Required) The full namespace and class name where your target method lives.
* **`method`**: (Required) The exact name of the C# method to execute.
* **`inputs`**: (Optional) An ordered array of typed values passed to the target method. Omitting it preserves the original parameterless-button behavior.

### 3. Your Target C# Method
The method you specify in your JSON **must be both `public` and `static`**. Its parameters must exactly match the `inputs` array, in the same order. Mod Buttons resolves a public static overload by parameter count and exact parameter types.

```csharp
namespace YourAwesomeMod
{
    public class CheatManager
    {
        // The integer input in the JSON above is passed here.
        public static void AddMoney(int amount)
        {
            staticVars.company.money += amount;
            Debug.Log("Added " + amount + " yen!");
        }

        // This will fail: not static.
        public void RestoreStamina() { }

        // This will fail for the example above: the JSON declares int, not string.
        public static void AddMoney(string amount) { }
    }
}
```

### 4. Action Inputs

Each input has an `id`, `type`, and optional label/default/range properties. Inputs are passed to the method in their JSON order; `id` is for UI/debugging and does not change argument order.

| `type` | C# parameter type by default | Notes |
| --- | --- | --- |
| `text` / `string` | `string` | A text field. |
| `integer` / `int` | `int` | A whole-number field. `min` and `max` are validated before invocation. |
| `float` / `number` | `float` | A decimal-number field. `min` and `max` are validated before invocation. |
| `slider` | `float` | Uses a game slider template. Defaults to `0`–`100` when no range is supplied. Set `"valueType": "integer"` for an `int` parameter. |
| `dropdown` / `select` | `string` | Uses a Modern UI Pack dropdown template when available. Supply `options`; set `valueType` to `integer` or `float` when its values should be converted. |

Example with a slider and dropdown:

```json
{
  "label": "Configure Promotion",
  "assembly": "YourAwesomeMod",
  "class": "YourAwesomeMod.PromotionActions",
  "method": "ApplyPromotion",
  "inputs": [
    {
      "id": "budget",
      "type": "slider",
      "valueType": "integer",
      "label": "Budget",
      "default": 25000,
      "min": 1000,
      "max": 100000
    },
    {
      "id": "audience",
      "type": "dropdown",
      "label": "Audience",
      "default": "general",
      "options": [
        { "label": "General public", "value": "general" },
        { "label": "Core fans", "value": "fans" }
      ]
    }
  ]
}
```

```csharp
public static void ApplyPromotion(int budget, string audience)
{
    // Use both values here.
}
```

For a dropdown, an option may also be a simple string such as `"general"`. Object options allow the player-facing `label` and method argument `value` to differ. If no Modern UI Pack dropdown template is available, Mod Buttons provides a game-styled selector fallback that cycles through the configured options.

Input actions stay open when validation or method resolution fails and show the failure under the action. They close after the reflected method completes successfully.

---

## 🌍 How to Add Localizations

`ModButtons` features a built-in, multi-mod localization manager. It automatically detects the player's active language settings and translates your button labels, tooltips, and your Mod's title header.

### 1. The Directory Structure
Create a `Localization` folder in your mod's root directory. Inside, create folders for your supported language codes (e.g., `en`, `jp`, `cn`, `ru`).

```
YourModFolder/
├── Localization/
│   ├── en/
│   │   └── strings.txt
│   └── jp/
│       └── strings.txt
├── ModButtons/
│   └── buttons.json
└── YourMod.dll
```

### 2. The `strings.txt` File
The localization file uses a simple `key=value` format.

**Example `Localization/en/strings.txt`:**
```text
# This translates the main header above your grid of buttons
mod.title=My Awesome Cheat Mod

# These translate specific buttons based on the "codeLabel" in your JSON
YourMod.Cheat.AddMoney=Add 1,000,000 Yen
YourMod.Cheat.RestoreStamina=Restore Idol Stamina
```

**Example `Localization/jp/strings.txt`:**
```text
mod.title=素晴らしいチートMOD
YourMod.Cheat.AddMoney=1,000,000円を追加
YourMod.Cheat.RestoreStamina=アイドルのスタミナを回復
```

### 3. Localization Logic
* **No `codeLabel` provided in JSON?** `ModButtons` will automatically generate a translation key using `YourClass.YourMethod`.
* **Missing Translations?** The manager will always load `Localization/en/strings.txt` as a baseline. If the user plays in Japanese but you haven't translated a specific button, it will safely fall back to your English baseline, or ultimately the `label` provided in your JSON.

---

## 🎨 Icon Guidelines
* **Size Requirements:** ModButtons supports buttons anywhere between **64 to 72 pixels** in width and height. You are not forced to use perfect squares—a 64x69 image is perfectly valid! The framework assigns an invisible 72x72 maximum bounding box and dynamically sizes your interactive button element to match your icon's exact pixel dimensions within those limits.
* **Format:** PNG is highly recommended.
* **Fallback:** If you do not provide an icon, `ModButtons` automatically generates a standard, rectangular text-button cloned from the game's native UI style instead of using the icon grid.
