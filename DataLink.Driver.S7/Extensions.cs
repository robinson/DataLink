namespace DataLink.Driver.S7
{
    public static class Extensions
    {
        /// <summary>
        /// Get byte[] from string with UTF8 Encoding.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] GetBytes(this string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str);
        }
        /// <summary>
        /// Get byte[] from string with UTF8 Encoding.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GetString(this byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
