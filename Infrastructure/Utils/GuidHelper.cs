namespace Infrastructure.Utils
{
    internal class GuidHelper
    { 
        /// <summary>
      /// Attempts to convert the given string into a Guid. 
      /// Returns Guid.Empty if the input is null, empty, or not a valid Guid format.
      /// </summary>
        public static Guid ConvertStringToGuid(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return Guid.Empty;

            // TryParse returns true if parsing succeeded, false otherwise.
            // If parsing fails, we return Guid.Empty (all zeros).
            return Guid.TryParse(input, out var result)
                ? result
                : Guid.Empty;
        }
    }
}
