﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        // Now that we are updating the theme's default color for classifications instead of updating the applied classification color, we need to 
        // update the classifications whose applied color matches the theme's color. These need to be reverted to the default color so that when we
        // change theme colors it will be reflected in the editor.
        private sealed class ForegroundColorDefaulter : ForegroundThreadAffinitizedObject
        {
            private readonly IVsFontAndColorStorage _fontAndColorStorage;
            private readonly IVsFontAndColorStorage3 _fontAndColorStorage3;
            private readonly IVsFontAndColorUtilities _fontAndColorUtilities;

            // Default colors have a special meaning in VS. They will evaluate to the
            // VS Theme's registered color for the particular classification.
            private const uint DefaultForegroundColor = 0x01000000u;
            private const uint DefaultBackgroundColor = 0x01000001u;

            // Colors are in 0x00BBGGRR

            // These colors should match the colors in the Visual Studio 2017.xml and Enhanced.xml scheme files.
            private const uint DarkThemePlainText = 0x00DCDCDCu;
            private const uint DarkThemeIdentifier = DarkThemePlainText;
            private const uint DarkThemeOperator = 0x00B4B4B4u;
            private const uint DarkThemeKeyword = 0x00D69C56u;
            private const uint DarkThemeClass = 0x00B0C94Eu;
            private const uint DarkThemeEnum = 0x00A3D7B8;
            private const uint DarkThemeLocal = 0x00FEDC9Cu;
            private const uint DarkThemeMethod = 0x00AADCDCu;
            private const uint DarkThemeControlKeyword = 0x00DFA0D8u;
            private const uint DarkThemeStruct = 0x0091C686u;

            private const uint LightThemePlainText = 0x00000000u;
            private const uint LightThemeIdentifier = LightThemePlainText;
            private const uint LightThemeOperator = LightThemePlainText;
            private const uint LightThemeKeyword = 0x00FF0000u;
            private const uint LightThemeClass = 0x00AF912Bu;
            private const uint LightThemeLocal = 0x007F371Fu;
            private const uint LightThemeMethod = 0x001F5374u;
            private const uint LightThemeControlKeyword = 0x00C4088Fu;

            private const uint AdditionalContrastThemeClass = 0x00556506u;

            private const string PlainTextClassificationTypeName = "plain text";

            private static readonly Guid TextEditorMEFItemsColorCategory = new Guid("75a05685-00a8-4ded-bae5-e7a50bfa929a");

            // Dark Theme

            // These classification colors should match the Visual Studio 2017.xml scheme file.
            private static ImmutableDictionary<string, uint> DarkThemeClassicForeground =>
                new Dictionary<string, uint>()
                {
                    [PlainTextClassificationTypeName] = DarkThemePlainText,
                    [ClassificationTypeNames.ClassName] = DarkThemeClass,
                    [ClassificationTypeNames.ConstantName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.ControlKeyword] = DarkThemeKeyword,
                    [ClassificationTypeNames.DelegateName] = DarkThemeClass,
                    [ClassificationTypeNames.EnumMemberName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.EnumName] = DarkThemeEnum,
                    [ClassificationTypeNames.EventName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.ExtensionMethodName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.FieldName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.Identifier] = DarkThemeIdentifier,
                    [ClassificationTypeNames.InterfaceName] = DarkThemeEnum,
                    [ClassificationTypeNames.Keyword] = DarkThemeKeyword,
                    [ClassificationTypeNames.LabelName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.LocalName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.MethodName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.ModuleName] = DarkThemeClass,
                    [ClassificationTypeNames.NamespaceName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.Operator] = DarkThemeOperator,
                    [ClassificationTypeNames.OperatorOverloaded] = DarkThemeOperator,
                    [ClassificationTypeNames.ParameterName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.PropertyName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.StructName] = DarkThemeClass,
                    [ClassificationTypeNames.TypeParameterName] = DarkThemeEnum,
                }.ToImmutableDictionary();

            // This represents the changes in classification colors between Visual Studio 2017.xml and Enhanced.xml scheme files.
            private static ImmutableDictionary<string, uint> DarkThemeEnhancedForegroundChanges =>
                new Dictionary<string, uint>()
                {
                    [ClassificationTypeNames.ControlKeyword] = DarkThemeControlKeyword,
                    [ClassificationTypeNames.ExtensionMethodName] = DarkThemeMethod,
                    [ClassificationTypeNames.LocalName] = DarkThemeLocal,
                    [ClassificationTypeNames.MethodName] = DarkThemeMethod,
                    [ClassificationTypeNames.OperatorOverloaded] = DarkThemeMethod,
                    [ClassificationTypeNames.ParameterName] = DarkThemeLocal,
                    [ClassificationTypeNames.StructName] = DarkThemeStruct,
                }.ToImmutableDictionary();

            // Light or Blue themes

            // These classification colors should match the Visual Studio 2017.xml scheme file.
            private static ImmutableDictionary<string, uint> BlueLightThemeClassicForeground =>
                new Dictionary<string, uint>()
                {
                    [PlainTextClassificationTypeName] = LightThemePlainText,
                    [ClassificationTypeNames.ClassName] = LightThemeClass,
                    [ClassificationTypeNames.ConstantName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ControlKeyword] = LightThemeKeyword,
                    [ClassificationTypeNames.DelegateName] = LightThemeClass,
                    [ClassificationTypeNames.EnumMemberName] = LightThemeIdentifier,
                    [ClassificationTypeNames.EnumName] = LightThemeClass,
                    [ClassificationTypeNames.EventName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ExtensionMethodName] = LightThemeIdentifier,
                    [ClassificationTypeNames.FieldName] = LightThemeIdentifier,
                    [ClassificationTypeNames.Identifier] = LightThemeIdentifier,
                    [ClassificationTypeNames.InterfaceName] = LightThemeClass,
                    [ClassificationTypeNames.Keyword] = LightThemeKeyword,
                    [ClassificationTypeNames.LabelName] = LightThemeIdentifier,
                    [ClassificationTypeNames.LocalName] = LightThemeIdentifier,
                    [ClassificationTypeNames.MethodName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ModuleName] = LightThemeClass,
                    [ClassificationTypeNames.NamespaceName] = LightThemeIdentifier,
                    [ClassificationTypeNames.Operator] = LightThemeOperator,
                    [ClassificationTypeNames.OperatorOverloaded] = LightThemeOperator,
                    [ClassificationTypeNames.ParameterName] = LightThemeIdentifier,
                    [ClassificationTypeNames.PropertyName] = LightThemeIdentifier,
                    [ClassificationTypeNames.StructName] = LightThemeClass,
                    [ClassificationTypeNames.TypeParameterName] = LightThemeClass,
                }.ToImmutableDictionary();

            // This represents the changes in classification colors between Visual Studio 2017.xml and Enhanced.xml scheme files.
            private static ImmutableDictionary<string, uint> BlueLightThemeEnhancedForegroundChanges =>
                new Dictionary<string, uint>()
                {
                    [ClassificationTypeNames.ControlKeyword] = LightThemeControlKeyword,
                    [ClassificationTypeNames.ExtensionMethodName] = LightThemeMethod,
                    [ClassificationTypeNames.LocalName] = LightThemeLocal,
                    [ClassificationTypeNames.MethodName] = LightThemeMethod,
                    [ClassificationTypeNames.OperatorOverloaded] = LightThemeMethod,
                    [ClassificationTypeNames.ParameterName] = LightThemeLocal,
                }.ToImmutableDictionary();

            // AdditionalContrast Theme

            // These classification colors should match the Visual Studio 2017.xml scheme file. The changes for Enhanced.xml are
            // captured in the `BlueLightThemeEnhancedForegroundChanges` dictionary.
            private static ImmutableDictionary<string, uint> AdditionalContrastThemeClassicForeground =>
                new Dictionary<string, uint>()
                {
                    [PlainTextClassificationTypeName] = LightThemePlainText,
                    [ClassificationTypeNames.ClassName] = AdditionalContrastThemeClass,
                    [ClassificationTypeNames.ConstantName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ControlKeyword] = LightThemeKeyword,
                    [ClassificationTypeNames.DelegateName] = AdditionalContrastThemeClass,
                    [ClassificationTypeNames.EnumMemberName] = LightThemeIdentifier,
                    [ClassificationTypeNames.EnumName] = LightThemeIdentifier,
                    [ClassificationTypeNames.EventName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ExtensionMethodName] = LightThemeIdentifier,
                    [ClassificationTypeNames.FieldName] = LightThemeIdentifier,
                    [ClassificationTypeNames.Identifier] = LightThemeIdentifier,
                    [ClassificationTypeNames.InterfaceName] = AdditionalContrastThemeClass,
                    [ClassificationTypeNames.Keyword] = LightThemeKeyword,
                    [ClassificationTypeNames.LabelName] = LightThemeIdentifier,
                    [ClassificationTypeNames.LocalName] = LightThemeIdentifier,
                    [ClassificationTypeNames.MethodName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ModuleName] = AdditionalContrastThemeClass,
                    [ClassificationTypeNames.NamespaceName] = LightThemeIdentifier,
                    [ClassificationTypeNames.Operator] = LightThemeOperator,
                    [ClassificationTypeNames.OperatorOverloaded] = LightThemeOperator,
                    [ClassificationTypeNames.ParameterName] = LightThemeIdentifier,
                    [ClassificationTypeNames.PropertyName] = LightThemeIdentifier,
                    [ClassificationTypeNames.StructName] = AdditionalContrastThemeClass,
                    [ClassificationTypeNames.TypeParameterName] = AdditionalContrastThemeClass,
                }.ToImmutableDictionary();

            // The High Contrast theme is not included because we do not want to make changes when the user is in High Contrast mode.

            // When we build our classification map we will need to look at all the classifications with foreground color.
            private static ImmutableArray<string> Classifications => DarkThemeClassicForeground.Keys.ToImmutableArray();

            public ForegroundColorDefaulter(IThreadingContext threadingContext, IServiceProvider serviceProvider)
                : base(threadingContext)
            {
                _fontAndColorStorage = serviceProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
                // IVsFontAndColorStorage3 has methods to default classifications but does not include the methods defined in IVsFontAndColorStorage
                _fontAndColorStorage3 = (IVsFontAndColorStorage3)_fontAndColorStorage;
                _fontAndColorUtilities = serviceProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorUtilities>();
            }

            /// <summary>
            /// Determines if all Classification foreground colors are DefaultColor or can be safely reverted to DefaultColor.
            /// </summary>
            public bool AreForegroundColorsDefaultable(Guid themeId)
            {
                AssertIsForeground();

                // Make no changes when in high contast mode.
                if (SystemParameters.HighContrast || !IsKnownTheme(themeId))
                {
                    return false;
                }

                var themeClassicForeground =
                    (themeId == KnownColorThemes.Dark) ? DarkThemeClassicForeground
                    : (themeId == KnownColorThemes.AdditionalContrast) ? AdditionalContrastThemeClassicForeground
                    : BlueLightThemeClassicForeground;

                var themeEnhancedForegroundChanges = (themeId == KnownColorThemes.Dark)
                    ? DarkThemeEnhancedForegroundChanges
                    : BlueLightThemeEnhancedForegroundChanges;

                // Open Text Editor category for readonly access
                if (_fontAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)__FCSTORAGEFLAGS.FCSF_READONLY) != VSConstants.S_OK)
                {
                    // We were unable to access color information.
                    return false;
                }

                var allDefaulted = true;

                foreach (var classification in Classifications)
                {
                    var colorItems = new ColorableItemInfo[1];
                    _fontAndColorStorage.GetItem(classification, colorItems);

                    var colorItem = colorItems[0];

                    // If a colorItem is not DefaultColor and cannot be defaulted, then the classifications cannot be 
                    // considered Defaulted.
                    if (!IsItemForegroundTheDefaultColor(colorItem)
                        && !CanItemBeSetToDefaultColors(colorItem, classification, themeClassicForeground, themeEnhancedForegroundChanges))
                    {
                        allDefaulted = false;
                        break;
                    }
                }

                _fontAndColorStorage.CloseCategory();

                return allDefaulted;
            }

            private bool IsItemForegroundTheDefaultColor(ColorableItemInfo colorInfo)
            {
                // Since we are primarily concerned with the foreground color, return early
                // if the setting isn't populated or is defaulted.
                return colorInfo.bForegroundValid == 0
                    || colorInfo.crForeground == DefaultForegroundColor;
            }

            /// <summary>
            /// Determines if the ColorableItemInfo can be reverted to its default state. This requires checking both color and font configuration,
            /// since reverting will reset all information for the item.
            /// </summary>
            private bool CanItemBeSetToDefaultColors(ColorableItemInfo colorInfo,
                string classification,
                ImmutableDictionary<string, uint> themeClassicForeground,
                ImmutableDictionary<string, uint> themeEnhancedForegroundChanges)
            {
                var foregroundColorRef = colorInfo.crForeground;
                _fontAndColorUtilities.GetColorType(foregroundColorRef, out var foregroundColorType);

                // Check if the color is an RGB. If the color has been changed to a system color or other, then we won't treat it as defaultable.
                if (foregroundColorType != (int)__VSCOLORTYPE.CT_RAW)
                {
                    return false;
                }

                var classicColor = themeClassicForeground[classification];
                var enhancedColor = themeEnhancedForegroundChanges.ContainsKey(classification)
                    ? themeEnhancedForegroundChanges[classification]
                    : classicColor; // Use the classic color since there is not an enhanced color for this classification

                var foregroundIsDefault = foregroundColorRef == classicColor || foregroundColorRef == enhancedColor;

                var backgroundColorRef = colorInfo.crBackground;
                var backgroundAndFontIsDefault = backgroundColorRef == DefaultBackgroundColor
                    && colorInfo.dwFontFlags == (uint)FONTFLAGS.FF_DEFAULT;

                return foregroundIsDefault && backgroundAndFontIsDefault;
            }

            /// <summary>
            /// Reverts Classification to DefaultColors if Foreground is not DefaultColor.
            /// </summary>
            public bool TrySetForegroundColorsToDefault(Guid themeId)
            {
                AssertIsForeground();

                // Make no changes when in high contast mode.
                if (SystemParameters.HighContrast || !IsKnownTheme(themeId))
                {
                    return false;
                }

                // Open Text Editor category for read/write.
                if (_fontAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES) != VSConstants.S_OK)
                {
                    // We were unable to access color information.
                    return false;
                }

                // Try to default any items that are not the DefaultColor but are already the default theme color.
                foreach (var classification in Classifications)
                {
                    var colorItems = new ColorableItemInfo[1];
                    _fontAndColorStorage.GetItem(classification, colorItems);

                    var colorItem = colorItems[0];

                    if (IsItemForegroundTheDefaultColor(colorItem))
                    {
                        continue;
                    }

                    _fontAndColorStorage3.RevertItemToDefault(classification);
                }

                _fontAndColorStorage.CloseCategory();

                return true;
            }
        }
    }
}
