// using System;
// using UnityEditor.Callbacks;
//
// namespace Test.EditMode {
//     
//     public class SampleClass : SampleAbstractClass {
//         
//         [DidReloadScripts]
//         private static void SampleMethod() {
//             
//         }
//     }
//
//     [RequiresStaticMethodImplementation("SampleMethod", typeof(DidReloadScripts))]
//     public abstract class SampleAbstractClass {
//         
//     }
// }
//
// [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
// public class RequiresStaticMethodImplementationAttribute : Attribute {
//
//     public string methodName;
//     public Type includeAttributeType;
//
//     public RequiresStaticMethodImplementationAttribute(string methodName) => this.methodName = methodName;
//     public RequiresStaticMethodImplementationAttribute(string methodName, Type includeAttributeType) : this(methodName) => this.includeAttributeType = includeAttributeType;
// }
