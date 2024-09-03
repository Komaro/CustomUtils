using System;

public class PriorityAttribute : Attribute {

    public int priority;

    public PriorityAttribute() => priority = 0;
    public PriorityAttribute(int priority) => this.priority = priority;
}