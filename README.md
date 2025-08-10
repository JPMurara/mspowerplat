Conclusion taken after Microsoft Power BI Report stage

CKAN and the Women’s Support Services API
The Queensland Government Open Data Portal is powered by CKAN, a data management platform.

The Women’s Support Services dataset is hosted in CKAN’s DataStore, which enables structured querying via RESTful APIs.

There are two main interfaces to access the data:

1. FileStore: for downloading static CSV files.
2. DataStore: for dynamic querying via the API (the one I am using).

Querying CKAN DataStore in Power BI
I have decided that the best way is to use datastore_search_sql for these reasons:

1. Precise filtering on specific fields
2. Server side filtering: reduces payload and improves performance
3. SQL permits to use ILIKE: for partial matching and case insensitive
4. I have used Uri.EscapeDataString to make the syntax easier, avoiding the use of special characters
5. Map \_id (CKAN) to record_id(REDCap) because RECap uses record_id as primary key

Query used in Power Query
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

For the phone input in the form, I consider some scenarios:

- Variations on user input: user inputs space within the phone number, dashes or parentheses which makes harder to validate and perform calcularion equation (to transform the number to international Australian phone number format)
- Validate phone input to only allow exactly 10 chars: to achieve this I had to use number validation minimum 10 in addition to @CHARLIMIT=10. In this way the user is only allowed to input a number of 10 digits without spaces, dashes or parentheses. However, REDCap removes the starting 0 behind the scenes which has to be handled in the calculation equation below:

phone_1 = 712345678
if(
left(concat("0", [phone_1]), 2) = "07", #Appends 0 to be beging of the phone number (as per issue mentioned above) and compares the 2 leftmost char to the "07" string.
concat("+617", right([phone_1], length([phone_1]) - 1)), #IF TRUE mutates the phone_1 to international Australian phone number format:
#1. takes the phone_1 length subtracted 1 and returns the remaining char from the right = 12345678
#3. append "+617" to the left of the "12345678" = +61712345678
concat("0", [phone_1]) #IF FALSE: appends "0" to the begining of phone_1 variable = 0712345678
)

- I can conclude that REDCap treats numbers/integers as strings behind the scenes, since I am allowed to append a string ("07") to phone_1 variable that has a number validation. Or the concat() method converts the number to a string before perming the operation.
