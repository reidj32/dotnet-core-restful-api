using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace Library.Api.Helpers
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(this IEnumerable<TSource> source, string fields)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // create a list to hold our ExpandoObjects
            List<ExpandoObject> expandoObjectList = new List<ExpandoObject>();

            // create a list with PropertyInfo objects on TSource. Reflection is expensive, so rather than doing it for
            // each object in the list, we do it once and reuse the results. After all, part of the reflection is on the
            // type of the object (TSource), not on the instance
            List<PropertyInfo> propertyInfoList = new List<PropertyInfo>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                // all public properties should be in the ExpandoObject
                PropertyInfo[] propertyInfos = typeof(TSource)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                // only the public properties that match the fields should be in the ExpandoObject

                // the field are separated by ",", so we split it.
                string[] fieldsAfterSplit = fields.Split(',');

                foreach (string field in fieldsAfterSplit)
                {
                    // trim each field, as it might contain leading or trailing spaces. Can't trim the var in foreach, so
                    // use another var.
                    string propertyName = field.Trim();

                    // use reflection to get the property on the source object we need to include public and instance,
                    // b/c specifying a binding flag overwrites the already-existing binding flags.
                    PropertyInfo propertyInfo = typeof(TSource)
                        .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (propertyInfo == null)
                        throw new Exception($"Property {propertyName} wasn't found on {typeof(TSource)}");

                    // add propertyInfo to list
                    propertyInfoList.Add(propertyInfo);
                }
            }

            // run through the source objects
            foreach (TSource sourceObject in source)
            {
                // create an ExpandoObject that will hold the selected properties & values
                ExpandoObject dataShapedObject = new ExpandoObject();

                // Get the value of each property we have to return. For that, we run through the list
                foreach (PropertyInfo propertyInfo in propertyInfoList)
                {
                    // GetValue returns the value of the property on the source object
                    object propertyValue = propertyInfo.GetValue(sourceObject);

                    // add the field to the ExpandoObject
                    ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }

                // add the ExpandoObject to the list
                expandoObjectList.Add(dataShapedObject);
            }

            // return the list

            return expandoObjectList;
        }
    }
}