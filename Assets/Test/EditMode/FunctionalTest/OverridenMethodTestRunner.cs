using System;
using System.Reflection;
using NUnit.Framework;

[Category(TestConstants.Category.FUNCTIONAL)]
public class OverridenMethodTestRunner {

    private class BaseClass {

        private readonly AutoOverridenMethod _autoOverridenMethod;

        protected BaseClass() => _autoOverridenMethod = new AutoOverridenMethod(GetType());

        public virtual void TestCallMethod() => _autoOverridenMethod.HasOverriden(MethodBase.GetCurrentMethod(), true);
        public virtual void TestCallMethod<T>() => _autoOverridenMethod.HasOverriden(MethodBase.GetCurrentMethod(), true);
        public virtual void TestCallMethod<T>(int intValue) => _autoOverridenMethod.HasOverriden(MethodBase.GetCurrentMethod(), true);
        public virtual void TestCallMethod<T>(T genericValue) => _autoOverridenMethod.HasOverriden(MethodBase.GetCurrentMethod(), true);
    }

    private class TestClass : BaseClass {

        // public override void TestCallMethod() { }
        // public override void TestCallMethod<T>() { }
        public override void TestCallMethod<T>(int intValue) { }
        public override void TestCallMethod<T>(T genericValue) { }
    }
    
    [Test]
    public void OverridenMethodTest() {
        var testClass = new TestClass();
        Assert.Throws<MissingMethodException>(() => testClass.TestCallMethod());
        Assert.Throws<MissingMethodException>(() => testClass.TestCallMethod<int>());
        testClass.TestCallMethod<float>(55);
        testClass.TestCallMethod(3.5d);
    }
}
