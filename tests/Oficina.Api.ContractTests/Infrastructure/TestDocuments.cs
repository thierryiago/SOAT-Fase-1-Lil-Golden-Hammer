namespace Oficina.Api.ContractTests.Infrastructure;

public static class TestDocuments
{
    public static string ValidCpf(int sequence)
    {
        var baseDigits = ((uint)sequence % 1_000_000_000).ToString().PadLeft(9, '0');
        var firstCheckDigit = CalculateCheckDigit(baseDigits, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var secondCheckDigit = CalculateCheckDigit(baseDigits + firstCheckDigit, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);

        return $"{baseDigits}{firstCheckDigit}{secondCheckDigit}";
    }

    private static int CalculateCheckDigit(string digits, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < digits.Length; i++)
        {
            sum += (digits[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
