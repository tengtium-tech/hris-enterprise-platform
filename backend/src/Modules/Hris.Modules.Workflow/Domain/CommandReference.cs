using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// Identifies the module command an automated step invokes. Source:
/// docs/04-modules/workflow/domain/value-objects.md.
///
/// This type is the whole of WR-001's expression in the domain model: a step names
/// a module and a command, and has no field capable of describing an operation on
/// that module's aggregates, a persistence type, or an internal handler. Logic
/// authored into a definition instead of a command would bypass the owning
/// aggregate's invariants, could not be unit-tested, would break silently when the
/// owning module changed, and would duplicate the moment a second workflow needed
/// the same calculation.
///
/// Whether the named command actually exists in that module's public contract
/// (CTR-WFL-001) is validated at publication from a caller-supplied signal, not
/// here: no module in this codebase publishes a queryable registry of its own
/// command surface yet, and this module takes no compile-time reference on a
/// sibling. Tracked in STATUS.md as a documented gap.
/// </summary>
public sealed class CommandReference : ValueObject
{
    public string ModuleName { get; }

    public string CommandId { get; }

    private CommandReference(string moduleName, string commandId)
    {
        ModuleName = moduleName;
        CommandId = commandId;
    }

    public static Result<CommandReference> Create(string? moduleName, string? commandId)
    {
        if (string.IsNullOrWhiteSpace(moduleName))
        {
            return Result.Failure<CommandReference>(WorkflowErrors.CommandReferenceModuleRequired);
        }

        return string.IsNullOrWhiteSpace(commandId)
            ? Result.Failure<CommandReference>(WorkflowErrors.CommandReferenceCommandRequired)
            : Result.Success(new CommandReference(moduleName.Trim(), commandId.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ModuleName;
        yield return CommandId;
    }

    public override string ToString() => $"{ModuleName}.{CommandId}";
}
