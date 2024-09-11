using System;

public class PriorityAttribute : Attribute {

    public uint priority;

    public PriorityAttribute() => priority = 0;
    public PriorityAttribute(uint priority) => this.priority = priority;
}