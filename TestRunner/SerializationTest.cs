using System.Reflection;
using BlazorExecutionFlow.Drawflow.BaseNodes;
using BlazorExecutionFlow.Helpers;
using BlazorExecutionFlow.Models.NodeV2;

namespace TestRunner
{
    public static class SerializationTest
    {
        public static void Run()
        {
            Console.WriteLine("=== Method Serialization/Deserialization Test ===\n");

            // Test with the actual ForEachString method
            var forEachMethod = typeof(AdvancedIterationNodes).GetMethod(
                "ForEachString",
                BindingFlags.Public | BindingFlags.Static
            );

            if (forEachMethod == null)
            {
                Console.WriteLine("ERROR: ForEachString method not found!");
                return;
            }

            Console.WriteLine($"[Test 1] Original Method: {forEachMethod.Name}");
            Console.WriteLine($"  Declaring Type: {forEachMethod.DeclaringType?.FullName}");
            Console.WriteLine($"  Parameters:");
            foreach (var param in forEachMethod.GetParameters())
            {
                Console.WriteLine($"    - {param.Name}: {param.ParameterType.FullName}");
            }
            Console.WriteLine();

            // Serialize the method
            Console.WriteLine("[Test 2] Serializing Method...");
            var serialized = MethodInfoHelpers.ToSerializableString(forEachMethod);
            Console.WriteLine($"  Serialized: {serialized}");
            Console.WriteLine();

            // Now try to deserialize it
            Console.WriteLine("[Test 3] Deserializing Method...");
            try
            {
                var deserialized = MethodInfoHelpers.FromSerializableString(serialized);
                Console.WriteLine($"  SUCCESS!");
                Console.WriteLine($"  Method Name: {deserialized.Name}");
                Console.WriteLine($"  Declaring Type: {deserialized.DeclaringType?.FullName}");
                Console.WriteLine($"  Parameters:");
                foreach (var param in deserialized.GetParameters())
                {
                    Console.WriteLine($"    - {param.Name}: {param.ParameterType.FullName}");
                }

                // Verify it's the same method
                if (deserialized == forEachMethod)
                {
                    Console.WriteLine($"\n  ✓ Deserialized method matches original!");
                }
                else
                {
                    Console.WriteLine($"\n  ✗ WARNING: Deserialized method doesn't match original");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}");
                Console.WriteLine($"  Stack: {ex.StackTrace}");
            }

            Console.WriteLine();

            // Test with the actual string from user's JSON
            Console.WriteLine("[Test 4] Testing with actual saved workflow data...");
            var userMethodSig = "BlazorExecutionFlow.Drawflow.BaseNodes.AdvancedIterationNodes, BlazorExecutionFlow, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null|ForEachString|System.Collections.Generic.List`1[[System.String, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e,BlazorExecutionFlow.Models.NodeV2.NodeContext, BlazorExecutionFlow, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null";

            try
            {
                var deserialized = MethodInfoHelpers.FromSerializableString(userMethodSig);
                Console.WriteLine($"  SUCCESS!");
                Console.WriteLine($"  Method Name: {deserialized.Name}");
                Console.WriteLine($"  Parameters: {deserialized.GetParameters().Length}");
                foreach (var param in deserialized.GetParameters())
                {
                    Console.WriteLine($"    - {param.Name}: {param.ParameterType.Name}");
                }
                Console.WriteLine($"\n  ✓ Successfully deserialized workflow method!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  Inner: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("=== End Serialization Test ===\n");
        }
    }
}
