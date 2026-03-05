using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;

namespace SignalPulse.Shared.UnitTests.FluentAssertions;

public abstract class ValidatorTestsBase<TValidator, TMessage>
    where TValidator : AbstractValidator<TMessage>, new()
{
    protected readonly Fixture Fixture;
    protected readonly TValidator Sut;

    protected ValidatorTestsBase()
    {
        Fixture = new Fixture();
        Sut = new TValidator();
    }

    protected ValidationResult Validate(TMessage message)
        => Sut.Validate(message);

    protected void ValidateRule(Action<TMessage> mutate, string expectedErrorMessage)
    {
        // Act
        var message = Fixture.Create<TMessage>();
        mutate(message);

        // Assert
        ValidateRule(message, expectedErrorMessage);
    }

    protected void ValidateRule(TMessage message, string expectedErrorMessage)
    {
        // Act
        var actual = Validate(message);

        // Assert
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle();
        actual.Errors[0].ErrorMessage.Should().Be(expectedErrorMessage);
    }

    protected void ValidateValidMessage(TMessage message)
    {
        // Act
        var actual = Validate(message);

        // Assert
        actual.IsValid.Should().BeTrue();
        actual.Errors.Should().BeEmpty();
    }

    protected void ShouldBeValid(TMessage message)
    {
        // Act
        var result = Validate(message);

        // Assert
        result.IsValid.Should().BeTrue("the request should be valid");
    }

    protected void ShouldHaveError(TMessage message, string expectedMessage)
    {
        // Act
        var result = Validate(message);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == expectedMessage);
    }
}