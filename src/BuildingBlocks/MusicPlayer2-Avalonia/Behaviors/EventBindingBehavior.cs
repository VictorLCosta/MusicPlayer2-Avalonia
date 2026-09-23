using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MusicPlayer2_Avalonia.Behaviors;

internal sealed class EventBindingBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<EventBindingBehavior, Control, ICommand?>(
            "Command");

    public static readonly AttachedProperty<object?> CommandParameterProperty =
        AvaloniaProperty.RegisterAttached<EventBindingBehavior, Control, object?>(
            "CommandParameter");

    public static readonly AttachedProperty<string?> EventNameProperty =
        AvaloniaProperty.RegisterAttached<EventBindingBehavior, Control, string?>(
            "EventName");

    public static readonly AttachedProperty<bool> PassEventArgsToCommandProperty =
        AvaloniaProperty.RegisterAttached<EventBindingBehavior, Control, bool>(
            "PassEventArgsToCommand");

    private static readonly AttachedProperty<RoutedEvent?> RegisteredEventProperty =
        AvaloniaProperty.RegisterAttached<EventBindingBehavior, Control, RoutedEvent?>(
            "RegisteredEvent");

    public static ICommand? GetCommand(Control element) =>
        element.GetValue(CommandProperty);

    public static void SetCommand(Control element, ICommand? value) =>
        element.SetValue(CommandProperty, value);

    public static object? GetCommandParameter(Control element) =>
        element.GetValue(CommandParameterProperty);

    public static void SetCommandParameter(Control element, object? value) =>
        element.SetValue(CommandParameterProperty, value);

    public static string? GetEventName(Control element) =>
        element.GetValue(EventNameProperty);

    public static void SetEventName(Control element, string? value) =>
        element.SetValue(EventNameProperty, value);

    public static bool GetPassEventArgsToCommand(Control element) =>
        element.GetValue(PassEventArgsToCommandProperty);

    public static void SetPassEventArgsToCommand(Control element, bool value) =>
        element.SetValue(PassEventArgsToCommandProperty, value);

    static EventBindingBehavior()
    {
        CommandProperty.Changed.AddClassHandler<Control>(OnBindingPropertyChanged);
        EventNameProperty.Changed.AddClassHandler<Control>(OnBindingPropertyChanged);
    }

    private static void OnBindingPropertyChanged(Control control, AvaloniaPropertyChangedEventArgs args)
    {
        RemoveHandler(control);
        AttachHandler(control);
    }

    private static void RemoveHandler(Control control)
    {
        var registeredEvent = control.GetValue(RegisteredEventProperty);

        if (registeredEvent is null)
        {
            return;
        }

        control.RemoveHandler(registeredEvent, ExecuteCommand);
        control.ClearValue(RegisteredEventProperty);
    }

    private static void AttachHandler(Control control)
    {
        var command = GetCommand(control);
        var eventName = GetEventName(control);

        if (command is null || string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        Type type = control.GetType();
        var routedEvent = GetRoutedEvent(type, eventName);

        if (routedEvent is not null)
        {
            control.AddHandler(
                routedEvent,
                ExecuteCommand,
                RoutingStrategies.Direct |
                RoutingStrategies.Bubble |
                RoutingStrategies.Tunnel
            );

            control.SetValue(RegisteredEventProperty, routedEvent);
        }
        
    }

    private static RoutedEvent? GetRoutedEvent(Type elementType, string eventName)
    {
        for (var currentType = elementType;
            currentType is not null;
            currentType = currentType.BaseType)
        {
            var routedEvent = RoutedEventRegistry.Instance
                .GetRegistered(currentType)
                .FirstOrDefault(x =>
                    string.Equals(
                        x.Name,
                        eventName,
                        StringComparison.Ordinal));

            if (routedEvent is not null)
            {
                return routedEvent;
            }
        }

        return null;
    }

    private static void ExecuteCommand(object? sender, RoutedEventArgs args)
    {
        if (sender is not Control control)
        {
            return;
        }

        var command = GetCommand(control);
        var commandParameter = GetPassEventArgsToCommand(control)
            ? args
            : GetCommandParameter(control);

        if (command?.CanExecute(commandParameter) == true)
        {
            command.Execute(commandParameter);
            args.Handled = true;
        }
    }
}
