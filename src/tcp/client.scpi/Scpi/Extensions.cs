
namespace cc.isr.Iot.Tcp.Client.Scpi;

/// <summary>   An extensions. </summary>
/// <remarks>   2023-08-17. </remarks>
public static class Extensions
{

    /// <summary>   A <see cref="string" /> extension method that returns the count of character from the right. </summary>
    /// <remarks>   2023-08-17. </remarks>
    /// <param name="value">       The value to act on. </param>
    /// <param name="maxLength">   The maximum length of the string to return. </param>
    /// <returns>   A <see cref="string" />. </returns>
    public static string Right( this string value, int maxLength )
    {
        // Check if the value is valid

        if ( string.IsNullOrEmpty( value ) )
        {
            // Set valid empty string as string could be null

            value = string.Empty;
        }
        else if ( value.Length > maxLength )
        {
            //Make the string no longer than the max length

            value = value.Substring( value.Length - maxLength, maxLength );
        }

        // Return the string

        return value;
    }
}
