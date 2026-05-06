using System;
using System.Collections.Generic;
using System.Linq;
using TableEditor.DataGrid;

namespace TableEditor.UndoRedo;

// Manages a fixed-depth stack of DgvData snapshots so the user can step backward through edits.
// The stack is bounded at MAX_STACK_DEPTH entries; once full, new states are silently discarded
// rather than evicting old ones (intentional: prevents silent data loss on large tables).
public class Undo
{
    // ---- Properties ----

    public string InstanceName { get; set; }
    public virtual string ClassName { get; set; } = "Undo";
    public bool Debug { get; set; }

    // True when at least MIN_STACK_DEPTH entries exist, meaning a Get() call will succeed.
    public bool CanDo { get; set; }

    // False while an undo/redo replay is in flight so callers can suppress re-capture.
    public bool CanSet { get; set; } = true;

    // Set to true by Get() so external listeners know the operation completed; callers must reset it.
    public bool Completed { get; set; }

    // True between the moment Get() starts retrieving a snapshot and InProgress is cleared, allowing
    // downstream event handlers (e.g. hover-point, auto-capture) to gate themselves.
    public bool InProgress { get; set; }

    public int StackCount { get { return stack.Count; } }

    // ---- Fields ----

    protected readonly int MAX_STACK_DEPTH = 100;
    protected readonly int MIN_STACK_DEPTH = 1;

    protected int stackPointer = 0;

    // Fired after Get() retrieves the target snapshot, giving observers a chance to react
    // without polling the stack directly.
    public event EventHandler<DgvData> NDR;

    // Each list entry is one complete table state; index 0 is the oldest, Last() is the current.
    public List<DgvData> stack = new List<DgvData>();

    // ---- Constructors ----

    public Undo()
    {
    }

    // Overload used by Redo to inject a different class name and minimum stack depth.
    public Undo(string className, int minStackDepth)
    {
        ClassName = className;
        MIN_STACK_DEPTH = minStackDepth;
    }

    // ---- Public Methods ----

    // Appends dgvData to the stack, skipping duplicates and refusing entries beyond the depth cap.
    public void Set(DgvData dgvData)
    {
        if (stackPointer >= MAX_STACK_DEPTH)
            return; // No room left; discard silently

        // Reject consecutive identical states to avoid wasting stack depth on no-op edits.
        if (stack.Count > 0 && stack.Last().Equals(dgvData))
        {
            if (Debug)
                Console.WriteLine($"{InstanceName} - {ClassName} - Duplicate entry rejected");

            return;
        }

        stack.Add(dgvData);
        stackPointer = stack.Count - 1;

        // Clamp the pointer in case stack growth raced ahead of the cap check above.
        if (stackPointer >= MAX_STACK_DEPTH)
            stackPointer = MAX_STACK_DEPTH;

        CanDo = stackPointer >= MIN_STACK_DEPTH;
    }

    // Removes the current (top) state, returns the previous one, fires the NDR event, and
    // updates CanDo. Returns null if there is nothing left to undo.
    public DgvData Get()
    {
        if (stackPointer < MIN_STACK_DEPTH)
            return null; // Nothing to restore

        // Signal that a replay is in flight so the new-data event handler does not re-capture
        // this state back onto the stack, creating an infinite loop.
        InProgress = true;

        DgvData dgvData = GetDgvInstanceAt(stackPointer - 1);

        // Drop the state we are leaving so subsequent Set() calls branch cleanly.
        stack.RemoveAt(stackPointer);
        stackPointer = stack.Count - 1;

        CanDo = stackPointer >= MIN_STACK_DEPTH;
        Completed = true;

        RaiseNdrEvent(dgvData);

        return dgvData;
    }

    // Empties the stack entirely and resets all bookkeeping, e.g. after a table is replaced.
    public void ClearStack()
    {
        stack.Clear();
        stackPointer = 0;
        CanDo = false;
    }

    // ---- Protected / Virtual ----

    // Returns the snapshot at the given index. Redo overrides this to shift the index by one
    // because it does not hold an initial base image the way Undo does.
    protected virtual DgvData GetDgvInstanceAt(int index)
    {
        return stack.ElementAt(index);
    }

    protected void RaiseNdrEvent(DgvData dgvData)
    {
        NDR?.Invoke(this, dgvData);
    }
}
