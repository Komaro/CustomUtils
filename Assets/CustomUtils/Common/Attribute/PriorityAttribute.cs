using System;

public class PriorityAttribute : Attribute, IComparable<PriorityAttribute> {

    public uint priority;

    public PriorityAttribute() => priority = 0;
    public PriorityAttribute(uint priority) => this.priority = priority;

    public int CompareTo(PriorityAttribute other) {
        if (other == null) {
            return 0;
        }

        return priority.CompareTo(other.priority);
    }
}
