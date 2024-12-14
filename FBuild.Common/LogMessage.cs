namespace FBuild.Common;

record class LogMessage(LogType LogType, string Message)
{
    public override string ToString()
    {
        return $"{this.LogType}:{this.Message}";
    }
}
