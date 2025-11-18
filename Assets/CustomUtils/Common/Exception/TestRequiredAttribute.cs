using System;
using System.Diagnostics;

public abstract class TodoRequiredAttribute : Attribute { }

// TODO. Attribute를 수집 관리하는 시스템 개발 필요
[Alias("Test")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Struct)]
public class TestRequiredAttribute : TodoRequiredAttribute {

    public TEST_TYPE type;
    public string description;
    
    public TestRequiredAttribute() {
        type = TEST_TYPE.FUNCTIONAL;
        description = string.Empty;
    }
    
    public TestRequiredAttribute(TEST_TYPE type, string description = "") {
        this.type = type;
        this.description = description;
    }
    
    public TestRequiredAttribute(string description) {
        type = TEST_TYPE.FUNCTIONAL;
        description = description;
    }
}

[Flags]
public enum TEST_TYPE {
    FUNCTIONAL,
    PERFORMANCE,
    STRESS,
    // ..
}

// TODO. 위와 동일
[Alias("Refactoring")]
public class RefactoringRequiredAttribute : TodoRequiredAttribute {

    public readonly int priority;
    public readonly string description;

    public RefactoringRequiredAttribute() => priority = 10;
    public RefactoringRequiredAttribute(int priority) => this.priority = Math.Clamp(priority, 0, 10);
    public RefactoringRequiredAttribute(int priority, string description) : this(priority) => this.description = description;
    public RefactoringRequiredAttribute(string description) : this(10) => this.description = description;
}

// TODO. 위와 동일
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Struct)]
public class TempAttribute : TodoRequiredAttribute {

    public readonly string title;
    public readonly string description;

    public TempAttribute(string title = "", string description = "") {
        this.title = title;
        this.description = description;
    }
}