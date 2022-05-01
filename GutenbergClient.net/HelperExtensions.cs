using System;
using System.Collections.Generic;
using System.Text;

namespace GutenbergClient.net
{
    public static class HelperExtensions
    {
        public static bool TryGetItem(this string[] array, int index, out int intVal)
        {
            intVal = -1;
            if (array == null || array.Length < index + 1)
            {
                return false;
            }

            var item = array[index];
            return int.TryParse(item, out intVal);
        }
    }
}
