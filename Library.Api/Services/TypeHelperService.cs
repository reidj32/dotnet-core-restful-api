using System.Reflection;

namespace Library.Api.Services
{
    public class TypeHelperService : ITypeHelperService
    {
        public bool TypeHasProperties<T>(string fields)
        {
            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            // the field are separated by ",", so we split it.
            string[] fieldsAfterSplit = fields.Split(',');

            // check if the requested fields exist on source
            foreach (string field in fieldsAfterSplit)
            {
                // trim each field, as it might contain leading or trailing spaces. Can't trim the var in foreach, so use
                // another var.
                string propertyName = field.Trim();

                // use reflection to check if the property can be found on T.
                PropertyInfo propertyInfo = typeof(T)
                    .GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                // it can't be found, return false
                if (propertyInfo == null)
                {
                    return false;
                }
            }

            // all checks out, return true
            return true;
        }
    }
}