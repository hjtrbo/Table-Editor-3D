using TableEditor.DataGrid;

namespace TableEditor.UndoRedo;

// Repurposes Undo as a redo stack. The only behavioural difference is that the redo stack
// does not hold an initial base-image entry, so every Get() must offset its lookup by +1
// to align with the actual stored snapshot index. MIN_STACK_DEPTH is 0 so the first entry
// on the stack is already retrievable (unlike Undo, which requires at least 2 entries).
public class Redo : Undo
{
    public override string ClassName
    {
        get { return base.ClassName; }
        set { base.ClassName = value; }
    }

    public Redo() : base("Redo", 0)
    {
    }

    // Shift the index by +1 to compensate for the missing base-image entry that Undo carries.
    protected override DgvData GetDgvInstanceAt(int index)
    {
        return base.GetDgvInstanceAt(index + 1);
    }
}
