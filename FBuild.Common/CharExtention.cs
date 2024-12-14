namespace FBuild.Common;

public static class CharExtention
{
    public static bool IsEnter(this char character)
    {
        return character is '\n' or '\r';
    }
    public static bool IsVarible(this char character)
    {
        return char.IsLetter(character) || character == '_';
    }
}
