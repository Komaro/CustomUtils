using System;

// TODO. Attribute를 수집 관리하는 시스템 개발 필요
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
public class TestRequiredAttribute : Attribute {

    public TEST_TYPE type;
    public string description;


    public TestRequiredAttribute(TEST_TYPE type, string description = "No description") {
        this.type = type;
        this.description = description;
    }
    
    public TestRequiredAttribute() : this(default) { }
    public TestRequiredAttribute(string description) : this(TEST_TYPE.FUNCTIONAL, description) { }

}

[Flags]
public enum TEST_TYPE {
    FUNCTIONAL,
    PERFORMANCE,
    STRESS,
    // ..
}

// TODO. 위와 동일
public class RefactoringRequiredAttribute : Attribute {

    public readonly int priority;
    public readonly string description;

    public RefactoringRequiredAttribute() => priority = 10;
    public RefactoringRequiredAttribute(int priority) => this.priority = Math.Clamp(priority, 0, 10);
    public RefactoringRequiredAttribute(int priority, string description) : this(priority) => this.description = description;
    public RefactoringRequiredAttribute(string description) : this(10) => this.description = description;
}

// TODO. 위와 동일
public class TempAttribute : Attribute {

    public readonly string title;
    public readonly string description;

    public TempAttribute(string title = "", string description = "") {
        this.title = title;
        this.description = description;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class TempMethodAttribute : TempAttribute {


    public TempMethodAttribute(string title = "", string description = "") : base(title, description) { }
}

[AttributeUsage(AttributeTargets.Class)]
public class TempClassAttribute : TempAttribute {


    public TempClassAttribute(string title = "", string description = "") : base(title, description) { }
}