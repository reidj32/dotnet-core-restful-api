# Useful Links

[FluentValidation](https://github.com/JeremySkinner/FluentValidation)

## Additional Shaping Options

* Expanding child resources (e.g. /api/resource?expand=child)
* Shaping those expanded resources (e.g. /api/resource?fields=f1,f2,child.f1)
* Complex filters (e.g. /api/resource?field=contains('value'))

## HATEOAS Support

* The `AuthorsController` class currently uses the dynamic approach
* The `BooksController` class currently uses the static approach
* Using `RequestHeaderMatchesMediaTypeAttribute` to shape the Content-Type and Accept headers for a method
* Using a supported method such as:
  * [OData](http://www.odata.org/) (Preferred)
  * [JSON-API](http://jsonapi.org/)
  * [JSON-LD](http://json-ld.org/)
  * Many more...
