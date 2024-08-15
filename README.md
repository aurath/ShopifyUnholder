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

## API Usage
The following permissions are needed on your Shopify app token:
- `read_orders`
- `write_merchant_managed_fulfillment_orders`
- `write_third_party_fulfillment_orders`

The app uses the `manualHoldsFulfillmentOrders` query to pull a list of all held orders for the location provided in the config file. It then matches order name (the human friendly order number I.E. #1001) to fulfillment order ID. If orders matching the provided names can't be found, it exits here after printing those order numbers to console.

It uses the `fulfillmentOrdersReleaseHolds` mutation to request the discovered fulfillment orders be unheld. It gets a job ID back, so it polls the job using the `job` query until the job finishes and it gets a list of changed orders.

It then correlates the returned list to the requested list to see if any of the orders were not modified. If so, it prints those order numbers to the console before exiting.