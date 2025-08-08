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
ToTable = Table.FromRecords(Records)
in
ToTable
