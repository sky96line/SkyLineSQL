# SkyLineSQL

**SkyLineSQL** is a lightweight and efficient tool designed to help you quickly filter and search for SQL Server database objects using simple command-based queries.

## Features

- Instantly search for various SQL Server database objects.
- Easy-to-use command-based search format.
- Supports filtering by tables, stored procedures, triggers, functions, views, and more.

## Search Format
/`<command>(optional)` `<search>`

### Available Commands

| Command | Object Type       |
|---------|-------------------|
| `u`     | Table             |
| `p`     | Stored Procedure  |
| `t`     | Trigger           |
| `f`     | Function          |
| `v`     | View              |
| `a`     | All               |

### ✅ Examples
1. /u customer      # Search for tables related to 'customer'
2. /p getInvoice    # Search for stored procedures with 'getInvoice'
3. /t trg_          # Search for triggers starting with 'trg_'
4. /a payment       # Search all object types with 'payment'

## 🤝 Contributions

Feel free to submit issues or feature requests. Contributions are welcome!
