# Available-dog pagination

The web page requests `GET /api/dogs?page=1&pageSize=12` and fetches a new page
when the user selects Previous, Next or a page number. Retry repeats the failed
page request. If the last page disappears as data changes, the page reloads the
new last page.

The API returns an array of available dogs and an `X-Total-Count` response header.
CORS exposes this header to the web origin. Filtering, ordering by name then ID,
and Skip/Take run in SQL Server; only one page is materialized. A separate COUNT
query supplies the total. Counts and items can change between these two queries
under concurrent updates; pagination is not a database snapshot.

When either pagination parameter is supplied, omitted page defaults to 1 and
omitted pageSize defaults to 12. page must be positive; pageSize must be 1–100;
the calculated SQL offset must fit in a signed 32-bit integer. Invalid values
return 400. A valid page beyond the last returns an empty array with the total.
Calls without either parameter preserve the original unpaged array contract.

No schema changes are needed. The paged response contains ID, name, breed and age.
The browser no longer slices the complete dog collection. It retains the existing
loading, error and empty states. A missing count header falls back to the returned
array length for compatibility with older API responses.

The initial SQL contract run failed all nine new cases (missing count header or
invalid input accepted). The pagination UI test now asserts requests for pages
1, 2, 3 and 1, replacing its former client-only no-refetch requirement.
