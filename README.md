# ASUSDriverHelper
A small **prototype tool** for ASUS laptops to simplify Wi-Fi and Bluetooth driver management. This project is built for learning and practice purposes, not a commercial product.

---
## Demo


https://github.com/user-attachments/assets/9b1de467-c8da-487e-a51d-b283ccdd9c67


---

## Problem

On my ASUS laptop, sometimes **Wi-Fi or Bluetooth would stop working**, and checking driver versions through Device Manager and the ASUS website was tedious.

---

## Solution

This tool automatically:

- Detects installed Wi-Fi and Bluetooth drivers.
- Checks installed versions against latest versions from a JSON file.
- Displays driver status as:
  - **Up-to-date**
  - **Outdated**
  - **Missing**
  - **Version not available**
- Provides **one-click download links** for missing or outdated drivers.
- Offers a **Refresh button** to re-check drivers.
- Shows a **Details popup** with version info.

> Currently, latest driver versions are hardcoded for my laptop model, but the tool can be adapted for other laptops by updating URLs and JSON entries.

---

## Features

- Automatic detection of Wi-Fi and Bluetooth drivers.
- Version comparison: Installed vs Latest.
- Driver status display.
- Refresh button for updates.
- One-click download for missing or outdated drivers.
- Detailed driver info popup.
- Built-in fallback using NetworkInterface if admin access is limited.

---

## Tech Stack

- **C#** with **WinForms**
- **PowerShell / WMI** for driver detection
- **JSON** for storing latest driver versions
- NetworkInterface fallback for detection without admin access

---

## Limitations

- Requires **administrator access** for full version detection.
- Latest driver versions are **hardcoded** in JSON.
- Currently tailored for **ASUS FX506HM**, but easily adaptable.
- Not a replacement for Windows Update—more of a personal **prototype for practice**.

---

## Future Scope

- Dynamically fetch the latest driver versions from the web or AI-powered service.
- Implement **fully one-click driver updates** without visiting manufacturer websites.
- Extend support for **all laptop brands** with configurable URLs and JSON.

---

## Usage

1. Launch `ASUSDriverHelper.exe`.
2. Check the status of Wi-Fi and Bluetooth drivers.
3. Click **Download** if a driver is missing or outdated.
4. Click **Refresh** to update status after changes.
5. Click **Details** for version information.

---

This project is primarily for **learning and practice**, showcasing driver detection, version comparison, and a small WinForms desktop application.
