namespace BlazorExecutionFlow.Helpers
{
    public static class StringHelpers
    {
        public static string AddSpaces(string str)
        {
            string result = str[0].ToString(); // Start with the first character
            for (int i = 1; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    result += " "; // Add a space before uppercase letters
                }
                result += str[i];
            }
            return result;
        }
    }
}
