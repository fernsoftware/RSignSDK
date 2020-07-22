namespace RSignSDK
{
    public class RSignAPIOptions
    {
        public RSignAPIOptions()
        {
            DateFormat = "EU: dd/mm/yyyy";
            ExpiryType = "30 Days";
            CultureInfo = "en-us";
        }

        public string DateFormat { get; set; }
        public string ExpiryType { get; set; }
        public string CultureInfo { get; set; }
    }
}