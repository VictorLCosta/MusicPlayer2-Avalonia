using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using MusicPlayer2_Avalonia.ViewModels;

namespace MusicPlayer2_Avalonia;

[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    private static readonly ConcurrentDictionary<Type, Type?> Cache = [];

    private static readonly Assembly CurrentAssembly = Assembly.GetExecutingAssembly();

    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var viewModelType = param.GetType();
        var viewType = Cache.GetOrAdd(viewModelType, type =>
        {
            var nameSpace = type.Namespace;
            if (nameSpace is null) return null;

            var name = type.Name;
            var viewNameSpace = nameSpace.Replace("ViewModel", "View", StringComparison.Ordinal);

            var viewName = name.Replace("ViewModel", "View", StringComparison.Ordinal);
            var fullName = $"{viewNameSpace}.{viewName}";

            var view = CurrentAssembly.GetType(fullName);

            if (view is not null) return view;

            viewName = name.Replace("ViewModel", "Content", StringComparison.Ordinal);
            view = CurrentAssembly.GetType($"{viewNameSpace}.{viewName}");

            return view;
        });

        if (viewType is null) return new TextBlock { Text = "View not found: " + viewModelType.FullName };

        var control = (Control)Activator.CreateInstance(viewType)!;
        control.DataContext = param;

        // ViewModels are owned by DI and reused when navigating between pages.

        return control;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}