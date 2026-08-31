using System;
using Android.App;
using Android.Runtime;
using Avalonia.Android;

namespace MusicPlayer2.Avalonia.Android;

[Application]
public sealed class AndroidApp(IntPtr javaReference, JniHandleOwnership transfer) 
    : AvaloniaAndroidApplication<App>(javaReference, transfer)
{
}
