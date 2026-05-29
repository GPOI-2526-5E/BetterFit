using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Betterfit.Contracts.Sales;
using Betterfit.Models;

namespace Betterfit.Tests;

public sealed class SalesRequestValidationTests
{
    [Fact]
    public void CreateGymSalePaymentRequest_ValidatesAmountUnderItalianCulture()
    {
        var request = new CreateGymSalePaymentRequest
        {
            Amount = 59.90m,
            Method = GymSalePaymentMethod.Card,
            Status = GymSalePaymentStatus.Paid
        };

        ValidateUnderItalianCulture(request);
    }

    [Fact]
    public void AddGymSalePaymentRequest_ValidatesAmountUnderItalianCulture()
    {
        var request = new AddGymSalePaymentRequest
        {
            Amount = 59.90m,
            Method = GymSalePaymentMethod.Card,
            Status = GymSalePaymentStatus.Paid
        };

        ValidateUnderItalianCulture(request);
    }

    private static void ValidateUnderItalianCulture(object request)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            var italianCulture = CultureInfo.GetCultureInfo("it-IT");
            CultureInfo.CurrentCulture = italianCulture;
            CultureInfo.CurrentUICulture = italianCulture;

            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(
                request,
                validationContext,
                validationResults,
                validateAllProperties: true);

            Assert.True(isValid, string.Join("; ", validationResults.Select(result => result.ErrorMessage)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
