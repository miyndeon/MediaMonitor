using System.Globalization;
using System.Windows.Controls;

namespace MediaMonitor.Validation;

// Проверяет, что введено целое неотрицательное число (используется для "Пересмотров"),
// и показывает понятную ошибку вместо молчаливого игнорирования некорректного ввода.
public class NonNegativeIntegerValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        var text = value?.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(text))
            return new ValidationResult(false, "Введите число");

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return new ValidationResult(false, "Только целые числа, например 0, 1, 2..");

        if (result < 0)
            return new ValidationResult(false, "Число не может быть отрицательным");

        return ValidationResult.ValidResult;
    }
}
