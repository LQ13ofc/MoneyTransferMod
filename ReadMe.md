# ?? Money Transfer Mod

A lightweight multiplayer utility for Stardew Valley. [cite_start]Send gold directly to other players and manage wallet sharing settings in real-time[cite: 1].

**Requires SMAPI.**

---

## ? Features

* [cite_start]**Direct Gold Transfer:** Send money from your personal wallet to any other online player when using separate wallets[cite: 1].
* [cite_start]**Wallet Management (Host Only):** Instantly toggle between **Shared Wallets** (farm funds) and **Separate Wallets** (individual funds) in-game without editing save files[cite: 1].
* [cite_start]**Smart Merging:** When merging wallets, all individual balances are combined into one global pool[cite: 1].
* [cite_start]**Smart Separation:** When separating wallets, the Host keeps the current balance, and other players start fresh (preventing gold duplication)[cite: 1].
* [cite_start]**Simple UI:** Easy-to-use menu with player portraits, cyclic selection, and a clear input box[cite: 1].
* [cite_start]**Visual Feedback:** HUD notifications and sound effects for successful transfers or errors[cite: 1].

---

## ?? How to Use

1.  [cite_start]**Open the Menu:** Press the default hotkey, **V**, to open the Transfer Menu[cite: 1].

### Transferring Money
*(Available when wallets are separated)*

1.  [cite_start]Use the **< Previous** and **Next >** buttons to cycle through online players[cite: 1].
2.  [cite_start]The menu displays the target player's name and portrait[cite: 1].
3.  [cite_start]Type the amount of gold you wish to send in the text box[cite: 1].
4.  [cite_start]Click the **Send Gold** button (envelope icon)[cite: 1].

### Managing Wallets
*(Host Only)*

1.  [cite_start]Open the menu using **V**[cite: 1].
2.  [cite_start]Click the toggle button at the bottom of the screen[cite: 1]:
    * [cite_start]**Merge Money:** Combines everyone's money into a single shared farm account[cite: 1].
    * **Separate Money:** Splits the account. [cite_start]The Host retains the funds, and other players rely on transfers or earning their own gold[cite: 1].

---

## ?? Installation

1.  **Install SMAPI.**
2.  Download the latest release of the **Money Transfer Mod**.
3.  Unzip the file and place the `MoneyTransferMod` folder into your `Stardew Valley/Mods` directory.
4.  Run the game once to generate the configuration file.

---

## ?? Configuration

The mod creates a `config.json` file after the first run. You can edit this file to change the hotkey.

| Setting | Default | Description |
| :--- | :--- | :--- |
| **OpenTransferKey** | `V` | [cite_start]The key used to open the transfer menu[cite: 1]. |

---

## ??? Building from Source

If you want to compile this mod yourself:

1.  Clone the repository or download the source code.
2.  Ensure you have the .NET SDK installed.
3.  Navigate to the source directory.
4.  Run `dotnet build` to compile the mod into a `MoneyTransferMod.dll`.

---

## ?? Credits

* **Author:** LQ13ofc
* **Version:** 1.0.0