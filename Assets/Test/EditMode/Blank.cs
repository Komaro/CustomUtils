// using System;
// using UnityEditor.Callbacks;
//
// [DidReloadScripts]
// public class TestClass : AbstractClass {
//         
// }
//
// [RequiresStaticMethodImplementation("TestMethod", typeof(DidReloadScripts))]
// public abstract class AbstractClass {
//         
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