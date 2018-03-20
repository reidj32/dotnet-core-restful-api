using Library.Api.Entities;
using Library.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Library.Api.Services
{
    public class PropertyMappingService : IPropertyMappingService
    {
        private readonly Dictionary<string, PropertyMappingValue> _authorPropertyMapping =
            new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                {"Id", new PropertyMappingValue(new List<string>{"Id"})},
                {"Genre", new PropertyMappingValue(new List<string>{"Genre"})},
                {"Age", new PropertyMappingValue(new List<string>{"DateOfBirth"}, true)},
                {"Name", new PropertyMappingValue(new List<string>{"FirstName", "LastName"})}
            };

        private readonly IList<IPropertyMapping> _propertyMappings = new List<IPropertyMapping>();

        public PropertyMappingService()
        {
            _propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
        }

        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
        {
            IEnumerable<PropertyMapping<TSource, TDestination>> match =
                _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();
            IList<PropertyMapping<TSource, TDestination>> propertyMappings = match.ToList();

            if (propertyMappings.Count == 1)
            {
                return propertyMappings[0].MappingDictionary;
            }
            throw new Exception($"Cannot find exact property mapping instance for <{typeof(TSource)},{typeof(TDestination)}>");
        }

        public bool ValidMappingExistsFor<TSource, TDestination>(string fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            Dictionary<string, PropertyMappingValue> propertyMapping = GetPropertyMapping<TSource, TDestination>();

            // the string is separated by ",", so we split it.
            string[] fieldsAfterSplit = fields.Split(',');

            // run through the fields clauses
            foreach (string field in fieldsAfterSplit)
            {
                // trim
                string trimmedField = field.Trim();

                // remove everything after the first " " - if the fields are coming from an orderBy string, this part
                // must be ignored
                int indexOfFirstSpace = trimmedField.IndexOf(" ", StringComparison.Ordinal);

                string propertyName = indexOfFirstSpace == -1 ?
                    trimmedField : trimmedField.Remove(indexOfFirstSpace);

                // find the matching property
                if (!propertyMapping.ContainsKey(propertyName))
                {
                    return false;
                }
            }
            return true;
        }
    }
}