# TeamCityApi project instructions

## Solution structure
- `src\TeamCityConsole` contains the CLI surface: verb constants, option classes, command classes, settings, and dependency registration in `Program.cs`.
- `src\TeamCityApi` contains TeamCity REST clients, domain models, locators, field builders, helpers, and use cases.
- `src\TeamCityApi.Tests` covers API/domain/use-case behavior.
- `src\TeamCityConsole.Tests` covers console-side command and utility behavior.

## Command implementation pattern
When adding a new CLI command:
1. Add a verb constant in `src\TeamCityConsole\Options\Verbs.cs`.
2. Add an options class in `src\TeamCityConsole\Options` using `CommandLine` `[Verb]` and `[Option]` attributes.
3. Add a command class in `src\TeamCityConsole\Commands` implementing `ICommand`.
4. Add a use case in `src\TeamCityApi\UseCases` for the actual behavior.
5. Register the use case and command in `Program.SetupContainer()`.

Keep command classes thin. Put TeamCity orchestration, parsing, and formatting logic in `TeamCityApi` use cases or helpers.

## TeamCity access pattern
- Put new REST calls in the appropriate client under `src\TeamCityApi\Clients`.
- Reuse existing domain models, locators, and field builders before adding new ones.
- For TeamCity properties, prefer the existing `Properties` and `PropertyList` types.
- Use existing build-chain helpers as reference points, but choose the helper that matches the required dependency type instead of forcing unrelated behavior into an existing class.

## File and output handling
- For console-side file output, prefer `IFileSystem` instead of direct file I/O so behavior stays testable.
- Produce deterministic output where possible by sorting rows or records before writing files.

## Testing conventions
- Use xUnit 1.x with `Fact`/`Theory`.
- Use NSubstitute for collaborators and AutoFixture helpers from the existing test projects.
- Add API/use-case tests in `src\TeamCityApi.Tests`.
- Add command wiring tests in `src\TeamCityConsole.Tests` when a new command is introduced.

## Validation workflow
- Use the existing psake build flow from `default.ps1`.
- The normal validation entry point is:
  `.\tools\psake\psake.ps1 .\default.ps1 local -parameters @{major_ver='1'; minor_ver='0'; initial_year='2013'; build_counter='0'; build_vcs_number='LOCAL'}`
