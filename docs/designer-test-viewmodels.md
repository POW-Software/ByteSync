# Design Preview Test ViewModels Guide

This guide explains how to craft lightweight *TestUI* view models packed with static data so Avalonia's Designer Preview can render any view
without running the full app. It is written for both humans and AI agents.

## Goals

- Provide representative UI snapshots directly from the Avalonia XAML previewer.
- Speed up prototyping, UX reviews, and documentation screenshots.

## Ground Rules

1. **Naming & Location**
    - Use the suffix `<ViewModelName>TestUI.cs`.
    - Store the class next to the real view model (e.g., `src/ByteSync.Client/ViewModels/<Feature>/`).
    - Place a clear warning comment directly above the constructor explaining the class is design-only, temporary, and must never be merged
      in a Pull Request.

2. **Inheritance**
    - Inherit from the production view model whenever possible to reuse the same bindings.
    - Shadow (`new`) or expose setters for properties/commands if the base class hardcodes them.

3. **Fake Data**
    - Populate every user-facing string with realistic text.
    - Fill observable collections with 2–3 sample entries.
    - Create inert commands via `ReactiveCommand.Create(() => { })` (or overloads) to avoid service calls.

4. **Wiring the View**
    - Inside the `.axaml` file, point the designer DataContext to the *TestUI* class:
      ```xml
      <Design.DataContext>
          <ratings:RatingPromptViewModelTestUI />
      </Design.DataContext>
      ```
    - Ensure the XML namespace (e.g., `xmlns:ratings`) targets the folder containing the class.

5. **Cleanup**
    - Plan to delete these classes once the view is finalized or no preview is needed.
    - Avoid shipping them in release builds unless they remain useful for future tweaks.

## Step-by-Step Procedure

1. **Inspect the Target ViewModel**
    - List every property bound in XAML: text labels, collections, toggles, commands.

2. **Create the Test Class**
    - Copy required `using` directives (`System.Collections.ObjectModel`, `System.Reactive`, `ReactiveUI`, etc.).
    - Provide a parameterless constructor that seeds all demo data.
    - Use readable, production-like values (full sentences, plausible URLs, etc.).

3. **Neutralize Runtime Logic**
    - Replace service calls with hardcoded values (e.g., localization strings, API responses).
    - For async APIs such as `WaitForResultAsync`, return pre-completed tasks or safe stubs.

4. **Attach to the View**
    - Declare the `Design.DataContext` block (see above).
    - Open the XAML designer and fix any compile issues by simplifying the *TestUI* model when needed.

5. **Validate in the Designer**
    - Confirm lists, buttons, and dynamic states appear with believable data.
    - Adjust sample values until the preview matches the intended UX scenario.

6. **Build the Solution**
    - Run a full solution build to ensure the new TestUI class compiles cleanly with the rest of the project.
    - Fix any warnings/errors immediately; the build must pass before sharing the view or opening a Pull Request.

## Quick Example

```csharp
public sealed class RatingPromptViewModelTestUI : RatingPromptViewModel
{
    // DESIGN-TIME ONLY: remove this TestUI class before merging to master.
    public RatingPromptViewModelTestUI()
    {
        RatingOptions = new ObservableCollection<RatingOption>
        {
            new("Rate in the Store", "https://bytesync.app/store"),
            new("Send Feedback", "https://bytesync.app/feedback")
        };

        RateCommand = ReactiveCommand.Create<string>(_ => { });
        AskLaterCommand = ReactiveCommand.Create(() => { });
        DoNotAskAgainCommand = ReactiveCommand.Create(() => { });
    }

    public new string Message { get; } = "Let us know how ByteSync feels today!";
    public new string AskLaterText { get; } = "Remind me later";
    public new string DoNotAskAgainText { get; } = "Don't show again";
}
```

## Extra Tips

- Use data that looks good in screenshots to support docs and marketing.
- Keep only one *TestUI* per view to minimize clutter.
- Mention the presence of *TestUI* files in PR descriptions so reviewers know they are temporary.

## End-of-Cycle Cleanup

Once the view ships:

- Remove the matching `*TestUI.cs` file.
- Remove (or keep, if still useful) the `Design.DataContext` block.

Following this recipe keeps Avalonia previews reliable and makes it easy for humans and agents to craft believable fake data for any
ByteSync view.
