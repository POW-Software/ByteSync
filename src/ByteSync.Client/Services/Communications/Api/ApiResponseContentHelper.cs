namespace ByteSync.Services.Communications.Api;

public static class ApiResponseContentHelper
{
    public static bool IsEmptyContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content == "null")
        {
            return true;
        }
        else
        {
            var trimmed = content.Trim();
            if (trimmed == "{}")
            {
                return true;
            }

            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                var trimmedNoSpaces = trimmed
                    .Replace(" ", "")
                    .Replace("\t", "")
                    .Replace("\n", "")
                    .Replace("\r", "");

                if (trimmedNoSpaces == """{"$id":"1"}""")
                {
                    return true;
                }
            }

            return false;
        }
    }
}