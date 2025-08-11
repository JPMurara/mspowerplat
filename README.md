# Overview

This is the conciese system level report for the tecnical tasks framed around the end-to-end goal in moving filtered records from an open CKAN dataset into REDCap via a workflow that applies validation and phone normalisation, with Power BI as the data transformation and Power Automate to orchestrate the process.

This division of responsibilities limits coupling: Power BI get the data, shapes and filter it, REDCap handles DB schema and validation, Power Automate handles orchestration and trigger actions (side effects). Tradeoff: more components to manage, but the benefit is better tracking, reuse, and using the right tool for each task.

# Microsoft Power BI Report - desktop app

Purpose: create a custom table for data acquisition and filtering (Power Query + CKAN API). This creates a stable, queryable dataset for automation. Also, in this step I clean and filter at the source, so no unnecesary rows are processed.

For querying CKAN DataStore in Power BI, I have decided that the best way is to use datastore_search_sql for these reasons:

1. Precise filtering on specific fields
2. Server side filtering: reduces payload and improves performance
3. SQL permits to use ILIKE: for partial matching and case insensitive
4. I have used Uri.EscapeDataString to make the syntax easier, avoiding the use of special characters
5. Map \_id (CKAN) to record_id(REDCap) because RECap uses record_id as primary key

## Query used in Power Query

let
Source = Json.Document(
Web.Contents(
"https://www.data.qld.gov.au/api/3/action/datastore_search_sql?sql=" &
Uri.EscapeDataString(
"SELECT \* FROM ""b690d52b-dab6-4066-a818-db8e1fda9fe7"" WHERE ""Name"" ILIKE '%sunshine coast%'"
)
)
),
Records = Source[result][records],
ToTable = Table.FromRecords(Records),
Renamed = Table.RenameColumns(ToTable, {{"_id", "record_id"}})
in
Renamed

# REDCap Data Collection Instrument

Purpose: data collection structure and constraints live in REDCap using instrument, validations, calculated field, action tags, API enabled.

## @HIDDEN field logic

The logic below transforms the phone number to Australian Phone Number Format.

phone_1 = 712345678
if(
left(concat("0", [phone_1]), 2) = "07",
concat("+617", right([phone_1], length([phone_1]) - 1)),
concat("0", [phone_1])
)

1.  Appends 0 to be beging of the phone number (as per issue mentioned above) and compares the 2 leftmost char to the "07" string.
2.  IF TRUE mutates the phone_1 to international Australian phone number format:
    2.1 takes the phone_1 length subtracted 1 and returns the remaining char from the right = 12345678
    2.2 append "+617" to the left of the "12345678" = +61712345678
3.  FALSE: appends "0" to the begining of phone_1 variable = 0712345678

## Form field validations

At first, I have implemented validation for some of the form fields. However, since I am injecting Women’s Support Services dataset into REDCap DB, the field validation caused problem because some data like Phone 2 contain not only numbers but also string. Since it is not a production application, I decided to remove all form field validation. For production environment and in a scenario where the data is injected from the actual form, some form validations and normalisation are necessary.

# Microsoft Power Automate Solution - https://make.powerautomate.com/

Purpose: data movement and remediation run in Power Automate (DAX query against the dataset, phone cleanup via a custom connector, HTTP import to REDCap).

## Custom Connector for phone number cleanup

Reusable script that removes non-digit characters from a phone number using regex, so it matches what REDCap expects.

## With HTTP action, post the transformed data to REDCap using the "Import Records" API method.

Sends the cleaned, validated data to the REDCap API.

## Power Atomate Flow

- 1.Trigger - Manually trigger a flow
  Purpose: run-on-demand while you iterate.
  Considerations: replace (for production) with a Recurrence, a Power Apps button, or a Dataverse trigger. - explain

- 2.Data fetch - run a query against a dataset (Power BI)
  Purpose: execute the DAX query (below) you validated in Power BI Desktop, and return only the columns needed.
  Key outputs:

EVALUATE
SELECTCOLUMNS(
FILTER(
'WSSQuery',
CONTAINSSTRING( LOWER('WSSQuery'[Name]), "sunshine coast" )
),
"Name", 'WSSQuery'[Name],
"Address 1", 'WSSQuery'[Address 1],
"Address 2", 'WSSQuery'[Address 2],
"Suburb", 'WSSQuery'[Suburb],
"Postcode", 'WSSQuery'[Postcode],
"State", 'WSSQuery'[State],
"Phone 1", 'WSSQuery'[Phone 1],
"Phone 2", 'WSSQuery'[Phone 2],
"Website", 'WSSQuery'[Website],
"Hours 1", 'WSSQuery'[Hours 1],
"Category", 'WSSQuery'[Category],
"Keywords", 'WSSQuery'[Keywords],
"Services", 'WSSQuery'[Services]
)

- 3.Parse JSON
  Purpose: give the flow a typed schema for each row so you can pick fields by name (e.g. [Name], [Suburb]).

Schema
{
"type": "object",
"properties": {
"[Name]": {
"type": "string"
},
"[Suburb]": {
"type": "string"
},
"[Postcode]": {
"type": "string"
},
"[State]": {
"type": "string"
},
"[Phone 1]": {
"type": "string"
},
"[Website]": {
"type": "string"
},
"[Hours 1]": {
"type": "string"
},
"[Category]": {
"type": "string"
},
"[Keywords]": {
"type": "string"
},
"[Services]": {
"type": "string"
}
}
}

- 4.Initialize variable (record_id)
  Purpose: provide a sequential ID when REDCap auto-numbering isn’t used.

At the end of the "Apply nto each" loop, the variable is incremented.

- 5. Apply to each (rows from Parse JSON)
- 5.1 Clean phone number (custom connector)
  Purpose: normalises raw phone input to the format the REDCap project expects: Autralian international phone number.

- 5.2 Compose (build one REDCap record)
  Purpose: Creates a single JSON object with field names that match the REDCap data dictionary. All mapping and transformation is done in one place. Then, the HTTP step simply sends it.

{
"record_id": "@{variables('record_id')}",
"name": "@{items('Apply_to_each')?['[Name]']}",
"address_1": "@{coalesce(items('Apply_to_each')?['[Address 1]'], '')}",
"address_2": "@{coalesce(items('Apply_to_each')?['[Address 2]'], '')}",
"suburb": "@{coalesce(items('Apply_to_each')?['[Suburb]'], '')}",
"postcode": "@{coalesce(items('Apply_to_each')?['[Postcode]'], '')}",
"state": "@{coalesce(items('Apply_to_each')?['[State]'], '')}",
"phone_1": "@{outputs('clean_phone_number')?['body/clean']}",
"phone_2": "@{coalesce(items('Apply_to_each')?['[Phone 2]'], '')}",
"website": "@{coalesce(items('Apply_to_each')?['[Website]'], '')}",
"hours_1": "@{coalesce(items('Apply_to_each')?['[Hours 1]'], '')}",
"category": "@{coalesce(items('Apply_to_each')?['[Category]'], '')}",
"keywords": "@{coalesce(items('Apply_to_each')?['[Keywords]'], '')}",
"services": "@{coalesce(items('Apply_to_each')?['[Services]'], '')}"
}

- 5.3 HTTP (Import Records)
  Purpose: Posts the record to REDCap’s Import Records API (setup below).

Method: POST

Headers:
Content-Type: application/x-www-form-urlencoded
Accept: application/json

Body:
concat(
'token=REDCAP_PROJECT_TOKEN',
'&content=record',
'&format=json',
'&type=flat',
'&returnContent=ids',
'&data=',
uriComponent(concat('[', string(outputs('Compose')), ']'))
)

### concat('[', string(outputs('Compose')), ']')

I used this method to wraps your single object as an array.

### uriComponent(...)

I used this to URL-encodes the JSON so quotes and newlines cannot corrupt the form body.

### Security

In HTTP Settings, I turned on Secure inputs and Secure outputs so the token is not exposed.

- 5.4 Increment variable
  Purpose: adds 1 to record_id for the next iteration.

# Considerations for production enviroment:

- 1.Implement a Try/Catch block inside the loop.
- 2.Store the token in an environment variable.
