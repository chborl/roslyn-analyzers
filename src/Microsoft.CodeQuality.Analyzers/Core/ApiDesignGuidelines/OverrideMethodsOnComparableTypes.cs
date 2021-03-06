﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeQuality.Analyzers.ApiDesignGuidelines
{
    /// <summary>
    /// CA1036: A public or protected type implements the System.IComparable interface and 
    /// does not override Object.Equals or does not overload the language-specific operator
    /// for equality, inequality, less than, less than or equal, greater than or 
    /// greater than or equal. The rule does not report a violation if the type inherits 
    /// only an implementation of the interface.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class OverrideMethodsOnComparableTypesAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1036";
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(MicrosoftApiDesignGuidelinesAnalyzersResources.OverrideMethodsOnComparableTypesTitle), MicrosoftApiDesignGuidelinesAnalyzersResources.ResourceManager, typeof(MicrosoftApiDesignGuidelinesAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageEquals = new LocalizableResourceString(nameof(MicrosoftApiDesignGuidelinesAnalyzersResources.OverrideMethodsOnComparableTypesMessageEquals), MicrosoftApiDesignGuidelinesAnalyzersResources.ResourceManager, typeof(MicrosoftApiDesignGuidelinesAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageOperators = new LocalizableResourceString(nameof(MicrosoftApiDesignGuidelinesAnalyzersResources.OverrideMethodsOnComparableTypesMessageOperator), MicrosoftApiDesignGuidelinesAnalyzersResources.ResourceManager, typeof(MicrosoftApiDesignGuidelinesAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageBoth = new LocalizableResourceString(nameof(MicrosoftApiDesignGuidelinesAnalyzersResources.OverrideMethodsOnComparableTypesMessageBoth), MicrosoftApiDesignGuidelinesAnalyzersResources.ResourceManager, typeof(MicrosoftApiDesignGuidelinesAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(MicrosoftApiDesignGuidelinesAnalyzersResources.OverrideMethodsOnComparableTypesDescription), MicrosoftApiDesignGuidelinesAnalyzersResources.ResourceManager, typeof(MicrosoftApiDesignGuidelinesAnalyzersResources));

        internal static readonly DiagnosticDescriptor RuleEquals = new DiagnosticDescriptor(RuleId,
                                                                                  s_localizableTitle,
                                                                                  s_localizableMessageEquals,
                                                                                  DiagnosticCategory.Design,
                                                                                  DiagnosticHelpers.DefaultDiagnosticSeverity,
                                                                                  isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultForVsixAndNuget,
                                                                                  description: s_localizableDescription,
                                                                                  helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1036-override-methods-on-comparable-types",
                                                                                  customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RuleOperator = new DiagnosticDescriptor(RuleId,
                                                                                  s_localizableTitle,
                                                                                  s_localizableMessageOperators,
                                                                                  DiagnosticCategory.Design,
                                                                                  DiagnosticHelpers.DefaultDiagnosticSeverity,
                                                                                  isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultForVsixAndNuget,
                                                                                  description: s_localizableDescription,
                                                                                  helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1036-override-methods-on-comparable-types",
                                                                                  customTags: WellKnownDiagnosticTags.Telemetry);

        internal static readonly DiagnosticDescriptor RuleBoth = new DiagnosticDescriptor(RuleId,
                                                                                  s_localizableTitle,
                                                                                  s_localizableMessageBoth,
                                                                                  DiagnosticCategory.Design,
                                                                                  DiagnosticHelpers.DefaultDiagnosticSeverity,
                                                                                  isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultForVsixAndNuget,
                                                                                  description: s_localizableDescription,
                                                                                  helpLinkUri: "https://docs.microsoft.com/visualstudio/code-quality/ca1036-override-methods-on-comparable-types",
                                                                                  customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleBoth, RuleEquals, RuleOperator);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            analysisContext.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol comparableType = WellKnownTypes.IComparable(compilationContext.Compilation);
                INamedTypeSymbol genericComparableType = WellKnownTypes.GenericIComparable(compilationContext.Compilation);

                // Even if one of them is available, we should continue analysis.
                if (comparableType == null && genericComparableType == null)
                {
                    return;
                }

                compilationContext.RegisterSymbolAction(context =>
                {
                    AnalyzeSymbol((INamedTypeSymbol)context.Symbol, comparableType, genericComparableType, context.ReportDiagnostic);
                },
                SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbol(INamedTypeSymbol namedTypeSymbol, INamedTypeSymbol comparableType, INamedTypeSymbol genericComparableType, Action<Diagnostic> addDiagnostic)
        {
            if (namedTypeSymbol.TypeKind == TypeKind.Interface ||
                namedTypeSymbol.TypeKind == TypeKind.Enum ||
                !namedTypeSymbol.IsExternallyVisible())
            {
                return;
            }

            if (namedTypeSymbol.AllInterfaces.Any(t => t.Equals(comparableType) ||
                                                      (t.ConstructedFrom?.Equals(genericComparableType) ?? false)))
            {
                var overridesEquals = namedTypeSymbol.OverridesEquals();
                string comparisonOperatorsString = GetNeededComparisonOperators(namedTypeSymbol);

                if (!overridesEquals && comparisonOperatorsString.Length != 0)
                {
                    addDiagnostic(namedTypeSymbol.CreateDiagnostic(RuleBoth, namedTypeSymbol.Name, comparisonOperatorsString));
                }
                else if (!overridesEquals)
                {
                    addDiagnostic(namedTypeSymbol.CreateDiagnostic(RuleEquals, namedTypeSymbol.Name));
                }
                else if (comparisonOperatorsString.Length != 0)
                {
                    addDiagnostic(namedTypeSymbol.CreateDiagnostic(RuleOperator, namedTypeSymbol.Name, comparisonOperatorsString));
                }
            }
        }

        private static string GetNeededComparisonOperators(INamedTypeSymbol symbol)
        {
            bool first = true;
            StringBuilder sb = null;
            void Append(string @operator)
            {
                if (first)
                {
                    sb = new StringBuilder();
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(@operator);
            }

            if (!symbol.ImplementsOperator(WellKnownMemberNames.EqualityOperatorName))
            {
                Append(symbol.Language == LanguageNames.CSharp ? "==" : "=");
            }

            if (!symbol.ImplementsOperator(WellKnownMemberNames.InequalityOperatorName))
            {
                Append(symbol.Language == LanguageNames.CSharp ? "!=" : "<>");
            }

            if (!symbol.ImplementsOperator(WellKnownMemberNames.LessThanOperatorName))
            {
                Append("<");
            }

            if (!symbol.ImplementsOperator(WellKnownMemberNames.LessThanOrEqualOperatorName))
            {
                Append("<=");
            }

            if (!symbol.ImplementsOperator(WellKnownMemberNames.GreaterThanOperatorName))
            {
                Append(">");
            }

            if (!symbol.ImplementsOperator(WellKnownMemberNames.GreaterThanOrEqualOperatorName))
            {
                Append(">=");
            }

            return sb?.ToString() ?? string.Empty;
        }
    }
}
