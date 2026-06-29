namespace JewelleryBusinessManager.Services;

public interface IWorkspaceCloseGuard
{
    bool CanCloseWorkspace();
}

public enum WorkspaceCloseDecision
{
    Close,
    Cancel,
    Handled
}

public interface IWorkspaceCloseRequestHandler
{
    WorkspaceCloseDecision RequestWorkspaceClose();
}
