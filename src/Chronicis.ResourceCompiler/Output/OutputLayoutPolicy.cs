namespace Chronicis.ResourceCompiler.Output;

public sealed class OutputLayoutPolicy
{
    public string GetEntityFolderName(string entityName)
    {
        _ = entityName;
        throw new NotImplementedException();
    }

    public string GetCompiledDocumentsPath(string outputRoot, string entityFolder)
    {
        _ = outputRoot;
        _ = entityFolder;
        throw new NotImplementedException();
    }

    public string GetPkIndexPath(string outputRoot, string entityFolder)
    {
        _ = outputRoot;
        _ = entityFolder;
        throw new NotImplementedException();
    }

    public string GetFkIndexPath(string outputRoot, string entityFolder, string childEntityName, string fkFieldName)
    {
        _ = outputRoot;
        _ = entityFolder;
        _ = childEntityName;
        _ = fkFieldName;
        throw new NotImplementedException();
    }
}
