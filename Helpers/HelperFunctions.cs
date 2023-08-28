using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS.Helpers
{
    class HelperFunctions
    {
        public string BuildUrlParamsFromStruct<T>(T myStruct) where T : struct
        {
            var properties = typeof(T).GetProperties();
            var paramList = new List<string>();

            foreach (var property in properties)
            {
                var value = property.GetValue(myStruct)?.ToString();
                if (value != null)
                {
                    var paramName = property.Name;
                    var paramValue = Uri.EscapeDataString(value);
                    paramList.Add($"{paramName}={paramValue}");
                }
            }

            return string.Join("&", paramList);
        }
        public string BuildUrlParamsFromClass<T>(T myClass) where T : class
        {
            var properties = typeof(T).GetProperties();
            var paramList = new List<string>();

            foreach (var property in properties)
            {
                var value = property.GetValue(myClass);
                if (value != null)
                {
                    var paramName = property.Name;
                    string paramValue;

                    // Custom Exits
                    if (property.PropertyType == typeof(int) && paramName == "fk_categoria")
                    {
                        if ((int)value == 0)
                        {
                            continue;
                        }
                    }
                    if (property.PropertyType == typeof(Categoria) || property.PropertyType == typeof(TipoDoc))
                    {
                        continue;
                    }

                    // Custom Format
                    if (property.PropertyType == typeof(DateOnly))
                    {
                        DateOnly dateOnly = (DateOnly)value;
                        paramValue = dateOnly.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        paramValue = Uri.EscapeDataString(value.ToString());
                    }

                    paramList.Add($"{paramName}={paramValue}");
                }
            }

            return string.Join("&", paramList);
        }
        public string BuildUrlParamsFromDictionary(Dictionary<string, object> dict)
        {
            if (dict == null)
            {
                return string.Empty;
            }

            var paramList = new List<string>();

            foreach (var kvp in dict)
            {
                var paramName = kvp.Key;
                if (kvp.GetType() == typeof(int))
                {
                    paramList.Add($"{paramName}={(int)kvp.Value}");
                }
                else
                {
                    paramList.Add($"{paramName}={Uri.EscapeDataString(kvp.Value.ToString())}");
                }
            }

            return string.Join("&", paramList);
        }

    }
}
