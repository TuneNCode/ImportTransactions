namespace ImportTransactions.Models
{
    public class TransactionLine
    {
        public string Account { get; set; }
        public string Description { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Value { get; set; }

        public TransactionLine(string account, string description, string currencyCode, decimal value)
        {
            Account = account;
            Description = description;
            CurrencyCode = currencyCode;
            Value = value;
        }

        public override string ToString()
        {
            return string.Format("('{0}','{1}','{2}',{3})",Account, Description, CurrencyCode, Value );
        }

    }
}