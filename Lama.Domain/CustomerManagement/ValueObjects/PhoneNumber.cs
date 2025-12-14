using Lama.Domain.Common;

namespace Lama.Domain.CustomerManagement.ValueObjects;

public class PhoneNumber : ValueObject
{
    public string Value { get; private set; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

        var cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (cleanNumber.Length < 10)
            throw new ArgumentException("Phone number must have at least 10 digits", nameof(phoneNumber));

        return new PhoneNumber(cleanNumber);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
