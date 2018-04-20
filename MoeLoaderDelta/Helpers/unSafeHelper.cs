using System;
using System.Reflection;

namespace MoeLoaderDelta
{
    public class unSafeHelper
    {

        /// <summary>
        /// Reflection get private members
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="fieldname"></param>
        /// <returns></returns>
        public static T GetPrivateField<T>(object instance, string fieldname)
        {
            BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
            Type type = instance.GetType();
            FieldInfo field = type.GetField(fieldname, flag);
            T to = default(T);
            try
            {
                to = (T)field.GetValue(instance);
            }
            catch { }
            return to;
        }

    }
}
