﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Special IDE analyzer to flag unnecessary pragma suppressions.
    /// </summary>
    internal interface IPragmaSuppressionsAnalyzer
    {
        /// <summary>
        /// Analyzes the tree, with an optional span scope, and report unnecessary pragma suppressions.
        /// </summary>
        Task AnalyzeAsync(
            SemanticModel semanticModel,
            TextSpan? span,
            CompilationWithAnalyzers compilationWithAnalyzers,
            Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getSupportedDiagnostics,
            Func<DiagnosticAnalyzer, bool> getIsCompilationEndAnalyzer,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken);
    }
}
