# myApp.Tests

This project contains unit and integration tests for the myApp Avalonia desktop application using headless testing.

## Test Structure

- **TestBase.cs** - Base class providing common testing utilities for Avalonia headless tests
- **HeadlessAppFixture.cs** - Fixture for setting up the Avalonia headless testing environment
- **ViewModels/** - Unit tests for view models and services
- **Views/** - UI component tests for Avalonia views
- **Integration/** - Integration tests for the complete application

## Dependencies

- **Avalonia.Headless** - Enables headless testing of Avalonia applications
- **Avalonia.Headless.XUnit** - XUnit integration for Avalonia headless testing
- **xunit** - Testing framework
- **FluentAssertions** - Fluent assertion library
- **Moq** - Mocking framework

## Running Tests

### Command Line
```bash
dotnet test
```

### Visual Studio
1. Open Test Explorer (Test > Test Explorer)
2. Run All Tests or run specific test categories

### Test Categories
- **Unit Tests** - Test individual components in isolation
- **Integration Tests** - Test component interactions
- **UI Tests** - Test Avalonia UI components using headless mode

## Test Data

Test data files are located in the `testdata/` directory and are automatically copied to the output directory during build.

## Headless Testing

The tests use Avalonia's headless testing capabilities, which allow testing UI components without requiring a display server. This makes the tests suitable for CI/CD environments and automated testing.

## Writing New Tests

1. Inherit from `TestBase` for UI tests
2. Use `HeadlessAppFixture` for application-level tests
3. Use `RunOnUIThread()` for UI thread operations
4. Use `CreateTestWindow()` to create test windows
5. Use `Click()` and `SetText()` for user interaction simulation

## Example Test

```csharp
public class MyViewTests : TestBase
{
    public MyViewTests(HeadlessAppFixture app) : base(app) { }

    [Fact]
    public async Task MyView_ShouldCreateSuccessfully()
    {
        // Arrange & Act
        var view = new MyView();
        var window = CreateTestWindow(view);

        // Assert
        view.Should().NotBeNull();
        window.Content.Should().Be(view);
    }
}
```
