# Contributing to WhatsAppAIAssistantBot

Thank you for your interest in contributing! Your help is greatly appreciated. Please follow these guidelines to ensure a smooth contribution process.

## How to Contribute

- **Bug Reports & Feature Requests:**
  - Use GitHub Issues to report bugs or suggest features.
  - Please provide as much detail as possible, including steps to reproduce bugs.

- **Pull Requests:**
  - Fork the repository and create a new branch for your changes.
  - Write clear, concise commit messages.
  - Ensure your code follows the existing style and conventions.
  - Add or update tests as appropriate.
  - Run all tests locally and ensure they pass before submitting a PR.
  - Reference related issues in your PR description.

## Development Setup

1. Clone the repository.
2. Install [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
3. Restore dependencies:
   ```sh
   dotnet restore
   ```
4. Build the solution:
   ```sh
   dotnet build
   ```
5. Run tests:
   ```sh
   dotnet test
   ```

## Coding Guidelines

- Use `net8.0` and C# 12 features where appropriate.
- Enable nullable reference types and follow best practices for nullability.
- Use dependency injection for all services.
- Do not commit secrets or sensitive data.
- Add XML documentation for public APIs.

## Code of Conduct

Be respectful and inclusive. Harassment or abusive behavior will not be tolerated.

## License

By contributing, you agree that your contributions will be licensed under the same license as this project.

---

If you have any questions, open an issue or contact the maintainers.
