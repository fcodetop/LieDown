using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LieDown
{
    public static class Extetions
    {
        public static string ToJson(this object value) 
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(value);
        }

        public static T JosnToObj<T>(this string jsonStr) 
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonStr);        
        }
    }
}
