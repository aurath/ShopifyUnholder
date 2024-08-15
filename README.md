# ShopifyUnhold
Helps with unholding batches of fulfillment orders based on order names.

## Setup
Add `config.json` to the installed directory (where unhold.exe is) and fill it out like this:
```json
{
  "Token": "Shopify app access token",
  "Store": "Store name",
  "Location": "Fulfillment location name to filter orders by"
}
```

## Usage
After adding the install directory to your path, you can call `unhold -?` to print the help info.
```powershell
# Provide a list of order numbers separated by spaces
Unhold X1001 X1002

# Use ranges for contiguous blocks of orders
Uhold X1001 X1005-X1009

# Pound symbol creates powershell comments, so if your orders start with #, use quotations
Unhold "#1001 #1005-#1008 #1025"
```

## Debugging
Logs are saved in the `/logs` directory at the installed location.
Use the `--console-log-level` command line option to enable logging to console.
```powershell
Unhold "#1001" --console-log-level Information
```