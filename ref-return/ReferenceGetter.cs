using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace RefReturn
{
    //
    // inspired by:
    //     https://stackoverflow.com/questions/50769916/get-memory-address-of-field-using-reflection-c
    //     https://stackoverflow.com/a/45046664/425678
    //     https://www.benbowen.blog/post/fun_with_makeref/
    //

    class ReferenceGetter
    {
        private static readonly BindingFlags bindingFlags =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.Static |
            BindingFlags.DeclaredOnly;

        public delegate ref U RefGetter<T, U>(T obj);

        public static IntPtr GetAddressOf(object pinnedObject, string fieldName)
        {
            var fiVal = pinnedObject.GetType().GetField(fieldName, bindingFlags);
            var miAll = typeof(ReferenceGetter).GetMethod("CreateRefGetter", BindingFlags.Static | BindingFlags.Public);

            var miGeneric = miAll.MakeGenericMethod(pinnedObject.GetType(), fiVal.FieldType);
            var ptr = (IntPtr)miGeneric.Invoke(null, new object[] { pinnedObject, fieldName });

            return ptr;
        }

        public static IntPtr CreateRefGetter<T, U>(object obj, string fieldName)
        {
            var fi = typeof(T).GetField(fieldName, bindingFlags);
            if (fi == null)
                throw new MissingFieldException(typeof(T).Name, fieldName);

            var methodName = "__refget_" + typeof(T).Name + "_fi_" + fi.Name;

            // workaround for using ref-return with DynamicMethod:
            //   a.) initialize with dummy return value
            var dm = new DynamicMethod(methodName, typeof(U), new[] { typeof(T) }, typeof(T), true);

            //   b.) replace with desired 'ByRef' return value
            dm.GetType().GetField("m_returnType", bindingFlags).SetValue(dm, typeof(U).MakeByRefType());

            var il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, fi);
            il.Emit(OpCodes.Ret);

            RefGetter<T, U> refGetter = (RefGetter<T, U>)dm.CreateDelegate(typeof(RefGetter<T, U>));

            unsafe
            {
                // https://www.benbowen.blog/post/fun_with_makeref/
                TypedReference typedRef = __makeref(refGetter((T)obj));
                IntPtr ptr = *((IntPtr*)&typedRef);
                return ptr;
            }
        }
    }
}
