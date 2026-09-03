namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// The exact seven states scheduling-framework.md's own Schedule Lifecycle diagram
/// names: "Draft -&gt; Validated -&gt; Approved -&gt; Active -&gt; Paused -&gt; Resumed -&gt; Retired."
/// <see cref="Active"/> and <see cref="Resumed"/> are both "currently triggering"
/// states -- see <see cref="Schedule"/>'s own remarks for why they are kept distinct
/// rather than collapsed into one, matching the document's own diagram exactly.
/// </summary>
public enum ScheduleStatus
{
    Draft = 0,
    Validated = 1,
    Approved = 2,
    Active = 3,
    Paused = 4,
    Resumed = 5,
    Retired = 6,
}
