# SkyLineSQL

**SkyLineSQL** is a lightweight and efficient Windows tool designed to help you quickly filter and search for SQL Server database objects using simple command-based queries. It also provides a built-in SQL Profiler for monitoring database activity.

## Features

- Instantly search for various SQL Server database objects (tables, stored procedures, triggers, functions, views, etc.).
- Command-based search format for quick filtering.
- Built-in SQL Profiler window to monitor and analyze SQL Server events.
- Hotkey support for quick window activation.
- Modern WPF UI with responsive controls.

## Requirements

- Windows OS
- .NET 6.0 or later
- SQL Server credentials for connecting to your database

## Configuration

You must create a `DataManager.json` file in the application directory to define your SQL Server connections. Example format:

```json
{
  "Local": "data source=SERVER1;initial catalog=Database1;user id=User1;password=Password1;...",
  "Staging": "data source=SERVER2;initial catalog=Database2;user id=User2;password=Password2;...",
  "Production": "data source=SERVER3;initial catalog=Database3;user id=User3;password=Password3;..."
}
```

You can add as many connections as you need. Each key is a connection name.

## Usage

1. **Launch the application.**
2. **Connect to your SQL Server database.**
3. **Search for objects:**
   - Use the search box with the following format:
     
     `/[commands] search_term`
     
     Example: `/u customer` (searches for tables related to 'customer')

### Available Commands

| Command | Object Type       |
|---------|-------------------|
| `u`     | Table             |
| `p`     | Stored Procedure  |
| `t`     | Trigger           |
| `f`     | Function          |
| `v`     | View              |
| `a`     | All               |

- You can combine commands (e.g., `/up customer` to search tables and procedures, `/upf customer` to search tables, procedures, or functions).
- Use `/a` for a broad search across all object types.

### Examples

1. `/u employee`      — Find tables related to employees
2. `/p calculateTax`  — Locate stored procedures for tax calculations
3. `/f getSalary`     — Search for functions named 'getSalary'
4. `/v sales`         — List views containing 'sales'
5. `/t afterInsert`   — Find triggers named 'afterInsert'
6. `/a invoice`       — Search all object types for 'invoice'
7. `/upf customer`    — Search tables, procedures, or functions for 'customer'

## Hotkeys

- Press **Ctrl + Shift + (Backslash or Numpad Plus)** to open the search window app from anywhere.
- Press **Ctrl + P** to switch between different connections defined in your `DataManager.json` file.

## SQL Profiler

- Open the Profiler window to monitor SQL Server events in real time.
- Start, pause, and stop profiling sessions.
- View detailed event data including text, login, CPU, reads, writes, duration, and more.

## Contributions

Feel free to submit issues or feature requests. Contributions are welcome!
