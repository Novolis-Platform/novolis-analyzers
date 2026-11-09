using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.Analyzers.ReadabilityComplexity;

/// <summary>
/// Member-level context that softens init / DI wiring without hiding real comprehension cost.
/// </summary>
internal readonly record struct ScoringContext(
    bool IsConstructor,
    bool IsInitLikeMember,
    string? MemberName)
{
    /// <summary>
    /// Constructors and UI/DI composition helpers treat instance-state wiring as cheap.
    /// </summary>
    public bool CheapInstanceInit => IsConstructor || IsInitLikeMember;

    /// <summary>
    /// After this many cheap instance-init mutations, further self-writes in an init context are free.
    /// </summary>
    public const int MaxCheapInstanceInitPoints = 5;

    /// <summary>
    /// Cap for ordinary methods that stamp own instance state / owned controls
    /// (<c>_busy = true</c>, <c>_button.IsEnabled = …</c>). External (parameter) mutation stays uncapped 10+1.
    /// </summary>
    public const int MaxOwnedSurfaceUpdatePoints = 10;

    public static ScoringContext Infer(SyntaxNode body)
    {
        for (var node = body; node is not null; node = node.Parent)
        {
            switch (node)
            {
                case ConstructorDeclarationSyntax:
                    return new ScoringContext(IsConstructor: true, IsInitLikeMember: false, MemberName: ".ctor");
                case MethodDeclarationSyntax method:
                    return new ScoringContext(
                        IsConstructor: false,
                        IsInitLikeMember: IsInitLikeName(method.Identifier.ValueText),
                        MemberName: method.Identifier.ValueText);
                case LocalFunctionStatementSyntax local:
                    return new ScoringContext(
                        IsConstructor: false,
                        IsInitLikeMember: IsInitLikeName(local.Identifier.ValueText),
                        MemberName: local.Identifier.ValueText);
                case AccessorDeclarationSyntax accessor:
                {
                    // set/init accessors are usually field/chrome stamps, not domain algorithms.
                    var isSetOrInit = accessor.IsKind(SyntaxKind.SetAccessorDeclaration)
                                      || accessor.IsKind(SyntaxKind.InitAccessorDeclaration);
                    return new ScoringContext(
                        IsConstructor: false,
                        IsInitLikeMember: isSetOrInit,
                        MemberName: accessor.Keyword.ValueText);
                }
                case OperatorDeclarationSyntax:
                case ConversionOperatorDeclarationSyntax:
                case DestructorDeclarationSyntax:
                    return new ScoringContext(false, false, null);
            }
        }

        return default;
    }

    /// <summary>
    /// Names that are predominantly composition / chrome wiring rather than domain logic.
    /// Kept narrow on purpose — bare <c>Build</c> / <c>Initialize</c> are too common in domain code.
    /// </summary>
    internal static bool IsInitLikeName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (name.Equals("InitializeComponent", StringComparison.OrdinalIgnoreCase)
            || name.Equals("InitializeComponentCore", StringComparison.OrdinalIgnoreCase)
            || name.Equals("ConfigureServices", StringComparison.OrdinalIgnoreCase)
            || name.Equals("ConfigureContainer", StringComparison.OrdinalIgnoreCase)
            || name.Equals("RegisterServices", StringComparison.OrdinalIgnoreCase)
            || name.Equals("AddServices", StringComparison.OrdinalIgnoreCase)
            || name.Equals("CreateHostBuilder", StringComparison.OrdinalIgnoreCase)
            || name.Equals("CreateBuilder", StringComparison.OrdinalIgnoreCase)
            || name.Equals("BuildAvaloniaApp", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Main", StringComparison.OrdinalIgnoreCase)
            || name.Equals("OnCreate", StringComparison.OrdinalIgnoreCase)
            || name.Equals("OnFrameworkInitializationCompleted", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (name.StartsWith("Configure", StringComparison.OrdinalIgnoreCase)
            && name.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (name.StartsWith("Build", StringComparison.OrdinalIgnoreCase) && name.Length > "Build".Length)
        {
            var rest = name.Substring("Build".Length);
            return rest.Equals("Layout", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Ui", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("UI", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("View", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Shell", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Chrome", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Content", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Window", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Controls", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Tree", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Panel", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Page", StringComparison.OrdinalIgnoreCase)
                   || rest.Equals("Card", StringComparison.OrdinalIgnoreCase)
                   || rest.EndsWith("Panel", StringComparison.OrdinalIgnoreCase)
                   || rest.EndsWith("Page", StringComparison.OrdinalIgnoreCase);
        }

        if (name.StartsWith("Create", StringComparison.OrdinalIgnoreCase))
        {
            return name.IndexOf("Layout", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("View", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Ui", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("Page", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Refresh*/Update*Ui/Apply*State — surface stamps, not domain algorithms.
        if (name.StartsWith("Refresh", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("UpdateUi", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("UpdateUI", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("UpdateView", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("UpdateVisual", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("ApplyUi", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("ApplyUI", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("ApplyVisual", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("ApplyState", StringComparison.OrdinalIgnoreCase)
            || name.Equals("UpdateChrome", StringComparison.OrdinalIgnoreCase)
            || name.Equals("BindControls", StringComparison.OrdinalIgnoreCase)
            || name.Equals("WireEvents", StringComparison.OrdinalIgnoreCase)
            || name.Equals("WireUi", StringComparison.OrdinalIgnoreCase)
            || name.Equals("WireUI", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
