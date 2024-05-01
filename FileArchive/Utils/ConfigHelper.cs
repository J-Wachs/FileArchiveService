using Microsoft.Extensions.Configuration;

namespace FileArchive.Utils
{
    internal static class ConfigHelper
    {
        /// <summary>
        /// Retrieves a value from the configuration. If it does not exist an exception is thrown. Used to retrieve
        /// config values that must exists for system/service to function.
        /// </summary>
        /// <typeparam name="T">The data type e.g. string, long</typeparam>
        /// <param name="config"></param>
        /// <param name="configKeyword"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal static T GetMustExistConfigValue<T>(IConfiguration config, string configKeyword)
        {
            // Allowed data-types
            if (typeof(T) != typeof(String) &&
                typeof(T) != typeof(long))
            {
                throw new Exception($"Method called with unsupported type: {typeof(T)}");
            }

            var temp = config.GetValue<T>(configKeyword);
            if (temp is null)
            {
                throw new ArgumentNullException(configKeyword);
            }
            else if (temp is String tempString)
            {
                if (String.IsNullOrEmpty(tempString))
                {
                    throw new ArgumentNullException(configKeyword);
                }

            }
            else if (temp is long tempLong)
            {
                if (tempLong is 0)
                {
                    throw new ArgumentNullException(configKeyword);
                }
            }
            return temp;
        }
    }
}
