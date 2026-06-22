# Version 1.31.1 Data Cleanup Compile Fix

## Issue fixed
Visual Studio reported many syntax errors starting in `Services/DataCleanupService.cs` around line 147.

Root cause: one generated HTML row used escaped quotes inside a C# interpolated expression:

```csharp
string.Join(\", \", group.Select(g => g.Label))
```

That is invalid C# in this context and caused the compiler to misread the rest of the method.

## Fix applied
Changed the expression to:

```csharp
string.Join(", ", group.Select(g => g.Label))
```

## Validation performed
- ZIP extraction check
- Project file XML parse
- XAML XML parse
- Confirmed the bad `string.Join(\", \", ...)` pattern is removed
- Confirmed no interpolated raw strings `$"""` are present in C# files
- Confirmed `DataCleanupService.cs` line 147 now uses safe normal quoted string syntax

