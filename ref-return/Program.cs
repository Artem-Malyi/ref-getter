using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RefReturn
{
    class Program
    {
        static void Main(string[] args)
        {
            // This is an example of getting the memory address
            // of a private static field using reflection and
            // C# 7.0 return-ref feature.
            Example obj = new Example(true);
            string fieldName = "TheFlag";

            // Pin the object to the current memory location,
            // to avoid GC or memory being moved.
            // Not relevant to static fields because they are
            // not a subject to the Garbage Collection.
            var hdl = GCHandle.Alloc(obj);

            unsafe
            {
                IntPtr pFieldValue = ReferenceGetter.GetAddressOf(obj, fieldName);

                bool* ptr = (bool*)pFieldValue;
                Console.WriteLine("Current Value: {0}", *ptr);

                *ptr = false;
                var fieldValue = typeof(Example).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                Console.WriteLine("Updated Value: {0}", fieldValue);
            }

            // Unpin the object.
            hdl.Free();

            Console.ReadLine();
        }
    }
}
