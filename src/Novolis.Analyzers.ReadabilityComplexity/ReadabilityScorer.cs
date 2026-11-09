using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Novolis.Analyzers.ReadabilityComplexity;

/// <summary>
/// Phase 1 readability-complexity scorer. Local analysis only; nested lambdas / local functions
/// are skipped so callers can score them independently.
/// </summary>
internal static class ReadabilityScorer
{
    public static ReadabilityScore Score(SyntaxNode body, SemanticModel model, CancellationToken cancellationToken)
    {
        var walker = new ScoringWalker(model, ScoringContext.Infer(body), cancellationToken);
        walker.Visit(body);
        return walker.ToScore();
    }

    private sealed class ScoringWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _model;
        private readonly ScoringContext _context;
        private readonly CancellationToken _cancellationToken;
        private readonly HashSet<ILocalSymbol> _locallyOwned = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<ISymbol, int> _externalMutationCounts = new(SymbolEqualityComparer.Default);
        private int _cheapInstanceInitPoints;

        private int _declarations;
        private int _localReassignments;
        private int _decisions;
        private int _switchPoints;
        private int _iterationPoints;
        private int _catchPoints;
        private int _throwPoints;
        private int _externalMutationPoints;
        private int _consumedResults;

        public ScoringWalker(SemanticModel model, ScoringContext context, CancellationToken cancellationToken)
            : base(SyntaxWalkerDepth.Node)
        {
            _model = model;
            _context = context;
            _cancellationToken = cancellationToken;
        }

        public ReadabilityScore ToScore() => new(
            _declarations,
            _localReassignments,
            _decisions,
            _switchPoints,
            _iterationPoints,
            _catchPoints,
            _throwPoints,
            _externalMutationPoints,
            _consumedResults);

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            // Nested bodies are scored independently by the analyzer.
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (node.Parent is VariableDeclarationSyntax
                {
                    Parent: LocalDeclarationStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or UsingStatementSyntax or FixedStatementSyntax
                })
            {
                _declarations++;
                TrackLocalOwnership(node);
            }

            if (node.Initializer?.Value is { } value
                && IsConsumptionSite(node.Initializer)
                && ContainsConsumedInvocation(value))
            {
                _consumedResults++;
            }

            base.VisitVariableDeclarator(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _iterationPoints += 2;
            _declarations++; // iteration variable
            base.VisitForEachStatement(node);
        }

        public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _iterationPoints += 2;
            base.VisitForEachVariableStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _iterationPoints += 2;
            if (node.Condition is { } condition && ContainsConsumedInvocation(condition))
                _consumedResults++;

            base.VisitForStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _iterationPoints += 2;
            if (ContainsConsumedInvocation(node.Condition))
                _consumedResults++;

            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _iterationPoints += 2;
            if (ContainsConsumedInvocation(node.Condition))
                _consumedResults++;

            base.VisitDoStatement(node);
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _decisions++;
            if (ContainsConsumedInvocation(node.Condition))
                _consumedResults++;

            // Visit then + else; else-if is another IfStatement and scores itself.
            Visit(node.Statement);
            if (node.Else is not null)
                Visit(node.Else);
        }

        public override void VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _switchPoints++;
            // Arms are declarative mapping expressions — do not charge consumed invocations in arms.
            Visit(node.GoverningExpression);
            foreach (var arm in node.Arms)
            {
                // Pattern may declare locals; visit pattern only.
                Visit(arm.Pattern);
                if (arm.WhenClause is not null)
                    Visit(arm.WhenClause);
            }
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (IsSimpleDispatchSwitch(node))
                _switchPoints += 1;
            else
                _switchPoints += Math.Max(1, node.Sections.Count);

            Visit(node.Expression);
            foreach (var section in node.Sections)
                Visit(section);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _catchPoints += 2;
            base.VisitCatchClause(node);
        }

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _throwPoints++;
            base.VisitThrowStatement(node);
        }

        public override void VisitThrowExpression(ThrowExpressionSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _throwPoints++;
            base.VisitThrowExpression(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (IsObjectOrCollectionInitializerAssignment(node))
            {
                base.VisitAssignmentExpression(node);
                return;
            }

            ScoreAssignmentTarget(node.Left, node.Right);

            if (ContainsConsumedInvocation(node.Right))
                _consumedResults++;

            base.VisitAssignmentExpression(node);
        }

        public override void VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            // out var, is-pattern, and deconstruction locals.
            _declarations++;
            base.VisitSingleVariableDesignation(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (node.IsKind(SyntaxKind.PreIncrementExpression) || node.IsKind(SyntaxKind.PreDecrementExpression))
                ScoreAssignmentTarget(node.Operand, right: null);

            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (node.IsKind(SyntaxKind.PostIncrementExpression) || node.IsKind(SyntaxKind.PostDecrementExpression))
                ScoreAssignmentTarget(node.Operand, right: null);

            base.VisitPostfixUnaryExpression(node);
        }

        private void TrackLocalOwnership(VariableDeclaratorSyntax node)
        {
            if (node.Initializer?.Value is null)
                return;

            var symbol = _model.GetDeclaredSymbol(node, _cancellationToken) as ILocalSymbol;
            if (symbol is null)
                return;

            if (IsObjectCreation(Unwrap(node.Initializer.Value)))
                _locallyOwned.Add(symbol);
            else
                _locallyOwned.Remove(symbol);
        }

        private void ScoreAssignmentTarget(ExpressionSyntax left, ExpressionSyntax? right)
        {
            left = Unwrap(left);

            if (left is IdentifierNameSyntax or DeclarationExpressionSyntax)
            {
                var symbol = _model.GetSymbolInfo(left, _cancellationToken).Symbol;
                if (symbol is ILocalSymbol local)
                {
                    _localReassignments++;
                    if (right is not null && IsObjectCreation(Unwrap(right)))
                        _locallyOwned.Add(local);
                    else
                        _locallyOwned.Remove(local);
                    return;
                }

                if (symbol is IParameterSymbol)
                {
                    // Rebinding a parameter (including ref) is local bookkeeping of the binding.
                    _localReassignments++;
                    return;
                }
            }

            // Classic DI / constructor injection: _repo = repo; this.Services = services;
            if (right is not null && IsDependencyInjectionStore(left, right))
                return;

            if (IsLocallyOwnedMutation(left))
                return;

            // Constructors / UI-DI composition helpers: instance chrome wiring is cheap and capped.
            if (_context.CheapInstanceInit && IsSelfInstanceMutation(left))
            {
                if (_cheapInstanceInitPoints >= ScoringContext.MaxCheapInstanceInitPoints)
                    return;

                _cheapInstanceInitPoints++;
                _externalMutationPoints += 1;
                return;
            }

            if (TryGetExternalMutationTarget(left, out var target) && target is not null)
            {
                if (!_externalMutationCounts.TryGetValue(target, out var count))
                    count = 0;

                count++;
                _externalMutationCounts[target] = count;
                _externalMutationPoints += count == 1 ? 10 : 1;
            }
        }

        private bool IsDependencyInjectionStore(ExpressionSyntax left, ExpressionSyntax right)
        {
            right = Unwrap(right);
            if (right is not IdentifierNameSyntax rightName)
                return false;

            if (_model.GetSymbolInfo(rightName, _cancellationToken).Symbol is not IParameterSymbol parameter)
                return false;

            if (!TryGetAssignedInstanceMember(left, out var member))
                return false;

            // In constructors, any parameter stored into instance state is ordinary DI wiring.
            if (_context.IsConstructor)
                return true;

            // Elsewhere, only the classic name match (_foo/foo/Foo ← foo) counts as free injection.
            return NamesMatchForInjection(member.Name, parameter.Name);
        }

        private bool TryGetAssignedInstanceMember(ExpressionSyntax left, out ISymbol member)
        {
            member = null!;
            left = Unwrap(left);

            switch (left)
            {
                case IdentifierNameSyntax identifier:
                {
                    var symbol = _model.GetSymbolInfo(identifier, _cancellationToken).Symbol;
                    if (symbol is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false })
                    {
                        member = symbol;
                        return true;
                    }

                    return false;
                }
                case MemberAccessExpressionSyntax memberAccess:
                {
                    if (!IsThisOrImplicitThisReceiver(memberAccess.Expression))
                        return false;

                    var symbol = _model.GetSymbolInfo(memberAccess, _cancellationToken).Symbol;
                    if (symbol is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false })
                    {
                        member = symbol;
                        return true;
                    }

                    return false;
                }
                default:
                    return false;
            }
        }

        private bool IsSelfInstanceMutation(ExpressionSyntax left)
        {
            left = Unwrap(left);

            if (left is IdentifierNameSyntax identifier)
            {
                var symbol = _model.GetSymbolInfo(identifier, _cancellationToken).Symbol;
                return symbol is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false };
            }

            if (left is MemberAccessExpressionSyntax member)
            {
                // this.X / this.X.Y / Me.X — root receiver is the instance under construction.
                return IsThisOrInstanceFieldRoot(member.Expression);
            }

            if (left is ElementAccessExpressionSyntax element)
                return IsThisOrInstanceFieldRoot(element.Expression);

            return false;
        }

        private bool IsThisOrInstanceFieldRoot(ExpressionSyntax expression)
        {
            expression = Unwrap(expression);
            if (IsThisOrImplicitThisReceiver(expression))
                return true;

            if (expression is IdentifierNameSyntax identifier)
            {
                var symbol = _model.GetSymbolInfo(identifier, _cancellationToken).Symbol;
                return symbol is IFieldSymbol { IsStatic: false } or IPropertySymbol { IsStatic: false };
            }

            if (expression is MemberAccessExpressionSyntax member)
                return IsThisOrInstanceFieldRoot(member.Expression);

            return false;
        }

        private static bool IsThisOrImplicitThisReceiver(ExpressionSyntax expression)
        {
            expression = Unwrap(expression);
            return expression is ThisExpressionSyntax or BaseExpressionSyntax;
        }

        private static bool NamesMatchForInjection(string memberName, string parameterName)
        {
            static string Normalize(string name)
            {
                if (name.StartsWith("_", StringComparison.Ordinal))
                    name = name.Substring(1);
                else if (name.StartsWith("m_", StringComparison.OrdinalIgnoreCase) && name.Length > 2)
                    name = name.Substring(2);

                return name;
            }

            return string.Equals(
                Normalize(memberName),
                Normalize(parameterName),
                StringComparison.OrdinalIgnoreCase);
        }

        private bool IsLocallyOwnedMutation(ExpressionSyntax left)
        {
            if (TryGetRootIdentifier(left, out var identifier))
            {
                var symbol = _model.GetSymbolInfo(identifier, _cancellationToken).Symbol;
                return symbol is ILocalSymbol local && _locallyOwned.Contains(local);
            }

            return false;
        }

        private bool TryGetExternalMutationTarget(ExpressionSyntax left, out ISymbol? target)
        {
            target = null;
            left = Unwrap(left);

            switch (left)
            {
                case MemberAccessExpressionSyntax member:
                {
                    var symbol = _model.GetSymbolInfo(member, _cancellationToken).Symbol;
                    if (symbol is IFieldSymbol or IPropertySymbol or IEventSymbol)
                    {
                        if (TryGetRootIdentifier(member.Expression, out var root))
                        {
                            target = _model.GetSymbolInfo(root, _cancellationToken).Symbol
                                     ?? _model.GetTypeInfo(member.Expression, _cancellationToken).Type;
                        }
                        else if (IsThisOrImplicitThisReceiver(member.Expression))
                        {
                            // Group all this.* writes under the containing type.
                            target = symbol.ContainingType;
                        }
                        else
                        {
                            target = symbol.ContainingSymbol;
                        }

                        return target is not null;
                    }

                    return false;
                }
                case ElementAccessExpressionSyntax element:
                {
                    if (TryGetRootIdentifier(element.Expression, out var root))
                    {
                        target = _model.GetSymbolInfo(root, _cancellationToken).Symbol;
                        return target is not null;
                    }

                    return false;
                }
                case IdentifierNameSyntax identifier:
                {
                    var symbol = _model.GetSymbolInfo(identifier, _cancellationToken).Symbol;
                    if (symbol is IFieldSymbol { IsStatic: false } field)
                    {
                        // Group instance field writes; statics stay distinct.
                        target = (ISymbol?)field.ContainingType ?? field;
                        return true;
                    }

                    if (symbol is IPropertySymbol { IsStatic: false } property)
                    {
                        target = (ISymbol?)property.ContainingType ?? property;
                        return true;
                    }

                    if (symbol is IFieldSymbol or IPropertySymbol)
                    {
                        target = symbol;
                        return true;
                    }

                    return false;
                }
                default:
                    return false;
            }
        }

        private static bool IsConsumptionSite(EqualsValueClauseSyntax _) => true;

        private static bool ContainsConsumedInvocation(ExpressionSyntax expression)
        {
            expression = Unwrap(expression);
            return expression switch
            {
                InvocationExpressionSyntax => true,
                AwaitExpressionSyntax awaitExpr => ContainsConsumedInvocation(awaitExpr.Expression),
                ConditionalAccessExpressionSyntax conditional
                    when conditional.WhenNotNull is InvocationExpressionSyntax
                         || (conditional.WhenNotNull is MemberBindingExpressionSyntax)
                         || HasInvocation(conditional.WhenNotNull)
                    => true,
                // Fluent / member chains ending in invocation are InvocationExpression already.
                // Object creation is not a method result consumption under this metric.
                ObjectCreationExpressionSyntax => false,
                ImplicitObjectCreationExpressionSyntax => false,
                ArrayCreationExpressionSyntax => false,
                _ => false,
            };
        }

        private static bool HasInvocation(ExpressionSyntax expression)
        {
            expression = Unwrap(expression);
            return expression is InvocationExpressionSyntax
                   || expression is ConditionalAccessExpressionSyntax c && HasInvocation(c.WhenNotNull)
                   || expression is MemberAccessExpressionSyntax;
        }

        private static bool IsObjectCreation(ExpressionSyntax expression)
        {
            expression = Unwrap(expression);
            return expression is ObjectCreationExpressionSyntax
                   or ImplicitObjectCreationExpressionSyntax
                   or ArrayCreationExpressionSyntax
                   or StackAllocArrayCreationExpressionSyntax
                   or CollectionExpressionSyntax
                   or AnonymousObjectCreationExpressionSyntax;
        }

        private static bool IsObjectOrCollectionInitializerAssignment(AssignmentExpressionSyntax node)
        {
            for (var current = node.Parent; current is not null; current = current.Parent)
            {
                if (current is InitializerExpressionSyntax)
                    return true;
                if (current is StatementSyntax or MemberDeclarationSyntax)
                    return false;
            }

            return false;
        }

        private static bool IsSimpleDispatchSwitch(SwitchStatementSyntax switchStatement)
        {
            foreach (var section in switchStatement.Sections)
            {
                foreach (var statement in section.Statements)
                {
                    if (!IsSimpleDispatchStatement(statement))
                        return false;
                }
            }

            return switchStatement.Sections.Count > 0;
        }

        private static bool IsSimpleDispatchStatement(StatementSyntax statement)
        {
            return statement switch
            {
                BreakStatementSyntax => true,
                ThrowStatementSyntax => true,
                ReturnStatementSyntax { Expression: null } => true,
                ReturnStatementSyntax { Expression: { } expression } => IsInvocationOrAwaitInvocation(expression),
                ExpressionStatementSyntax { Expression: { } expression } => IsInvocationOrAwaitInvocation(expression),
                BlockSyntax block => block.Statements.All(IsSimpleDispatchStatement),
                _ => false,
            };
        }

        private static bool IsInvocationOrAwaitInvocation(ExpressionSyntax expression)
        {
            expression = Unwrap(expression);
            return expression switch
            {
                InvocationExpressionSyntax => true,
                AwaitExpressionSyntax awaitExpr => IsInvocationOrAwaitInvocation(awaitExpr.Expression),
                _ => false,
            };
        }

        private static bool TryGetRootIdentifier(ExpressionSyntax expression, out IdentifierNameSyntax identifier)
        {
            expression = Unwrap(expression);
            while (true)
            {
                switch (expression)
                {
                    case IdentifierNameSyntax id:
                        identifier = id;
                        return true;
                    case MemberAccessExpressionSyntax member:
                        expression = Unwrap(member.Expression);
                        continue;
                    case ElementAccessExpressionSyntax element:
                        expression = Unwrap(element.Expression);
                        continue;
                    case ConditionalAccessExpressionSyntax conditional:
                        expression = Unwrap(conditional.Expression);
                        continue;
                    default:
                        identifier = null!;
                        return false;
                }
            }
        }

        private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
        {
            while (true)
            {
                switch (expression)
                {
                    case ParenthesizedExpressionSyntax paren:
                        expression = paren.Expression;
                        continue;
                    case AwaitExpressionSyntax awaitExpr:
                        expression = awaitExpr.Expression;
                        continue;
                    case CastExpressionSyntax cast:
                        expression = cast.Expression;
                        continue;
                    default:
                        return expression;
                }
            }
        }
    }
}
