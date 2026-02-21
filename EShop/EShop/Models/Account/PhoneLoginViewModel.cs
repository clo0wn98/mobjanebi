namespace EShop.Models.Account
{
    public class PhoneLoginViewModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class SendCodeViewModel
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
