﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;

namespace ShaderTools.CodeAnalysis.Editor
{
    /// <summary>
    /// Interface to allow providers to return some sort of quick info content whose creation can be
    /// deferred to a later point (and on the UI thread)
    /// </summary>
    internal interface IDeferredQuickInfoContent
    {
        FrameworkElement Create();
    }
}
