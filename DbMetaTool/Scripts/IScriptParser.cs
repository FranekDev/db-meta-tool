namespace DbMetaTool.Scripts
{
    public interface IScriptParser
    {
        ParsedScripts ParseScriptsFromDirectory(string scriptsDirectory);
        ScriptType DetectScriptType(string scriptContent);
    }
}
