using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using MusicPlayer2_Avalonia.Application.Player;
using MusicPlayer2_Avalonia.Styles.NowPlaying;
using MusicPlayer2_Avalonia.Styles.AudioSpectrum;
using MusicPlayer2_Avalonia.Views;
using MusicPlayer2_Avalonia.Application.Playlist.Models;

AppBuilder.Configure<Avalonia.Application>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
var first = Guid.NewGuid();
var middle = Guid.NewGuid();
var last = Guid.NewGuid();
var queue = new PlaybackQueue();
queue.Replace([first, middle, last]);
Check(queue.Peek(1) is null, "unselected queue has no preview");
queue.SetCurrent(middle);
var revision = queue.Revision;
Check(queue.Peek(-1) == first && queue.Peek(1) == last && queue.CurrentTrackId == middle && queue.Revision == revision,
    "previewing neighbors leaves the playback position unchanged");
queue.SetCurrent(first);
Check(queue.Peek(-1) is null, "first track has no previous preview");
queue.SetCurrent(last);
Check(queue.Peek(1) is null, "last track has no next preview");

var nextCalls = 0;
var previousCalls = 0;
var carousel = new ArtworkCarousel { TrackId = middle, HasNextTrack = true, HasPreviousTrack = true };
carousel.NextCommand = new RelayCommand(() => { nextCalls++; carousel.TrackId = last; });
carousel.PreviousCommand = new RelayCommand(() => { previousCalls++; carousel.TrackId = first; });
var window = new Window { Width = 400, Height = 600, Content = carousel };
window.Show();
Pump(50);

window.MouseDown(new Point(200, 220), MouseButton.Left);
window.MouseMove(new Point(150, 220));
Pump(20);
var middleSlide = carousel.GetVisualDescendants().OfType<Grid>().Where(grid => grid.RenderTransform is TranslateTransform).ElementAt(1);
Check(((TranslateTransform)middleSlide.RenderTransform!).X < -40 && nextCalls == 0,
    "artwork follows the pointer without changing music while held");
Pump(140); // Holding before release removes flick velocity.
window.MouseUp(new Point(150, 220), MouseButton.Left);
Pump(500);
Check(nextCalls == 0 && Math.Abs(((TranslateTransform)middleSlide.RenderTransform!).X) < 0.01,
    "short drag returns to the current cover");

Drag(new Point(320, 220), new Point(100, 220));
Check(nextCalls == 1 && previousCalls == 0 && carousel.TrackId == last && carousel.CaptionOpacity == 1,
    "left swipe advances once and restores caption opacity");
carousel.TrackId = middle;
Drag(new Point(80, 220), new Point(300, 220));
Check(previousCalls == 1 && carousel.TrackId == first, "right swipe goes to the previous track");

carousel.HasPreviousTrack = false;
Drag(new Point(80, 220), new Point(320, 220));
Check(previousCalls == 1, "queue edge bounces without executing a command");
carousel.HasNextTrack = false;
Drag(new Point(320, 220), new Point(80, 220));
Check(nextCalls == 1, "last queue edge also bounces");
carousel.HasNextTrack = true;
Drag(new Point(200, 120), new Point(190, 420));
Check(nextCalls == 1 && previousCalls == 1, "vertical drag never skips a track");

window.MouseDown(new Point(320, 220), MouseButton.Left);
window.MouseMove(new Point(80, 220));
window.MouseUp(new Point(80, 220), MouseButton.Left);
window.MouseDown(new Point(320, 220), MouseButton.Left);
window.MouseMove(new Point(80, 220));
window.MouseUp(new Point(80, 220), MouseButton.Left);
Pump(600);
Check(nextCalls == 2, "touches during settling do not queue another skip");

carousel.TrackId = middle;
window.MouseDown(new Point(320, 220), MouseButton.Left);
window.MouseMove(new Point(80, 220));
carousel.TrackId = first;
window.MouseUp(new Point(80, 220), MouseButton.Left);
Pump(500);
Check(nextCalls == 2, "external track change cancels an active drag");

window.MouseDown(new Point(320, 220), MouseButton.Left);
window.MouseMove(new Point(80, 220));
window.MouseUp(new Point(80, 220), MouseButton.Left);
window.Content = null;
Pump(500);
Check(nextCalls == 2, "detaching during animation cancels the pending skip");
window.Close();

var spectrum = new AudioSpectrum { Width = 320, Height = 44, ShowReflection = false };
spectrum.Measure(new Size(320, 44));
spectrum.Arrange(new Rect(0, 0, 320, 44));
var drawing = new DrawingGroup();
using (var context = drawing.Open()) spectrum.Render(context);
Check(drawing.Children.Count == 64 && drawing.Children.All(bar => Math.Abs(bar.GetBounds().Bottom - 44) < 0.01),
    "spectrum draws only upward bars with every base at the bottom pixel");

Avalonia.Application.Current!.Styles.Add(new StyleInclude(new Uri("avares://MusicPlayer2-Avalonia/"))
    { Source = new Uri("avares://MusicPlayer2-Avalonia/Styles/Styles.axaml") });
Avalonia.Application.Current.RequestedThemeVariant = MusicPlayer2_Avalonia.AppThemes.Original;
var main = new MainView();
var panel = main.FindControl<NowPlaying>("PlayerPanel")!;
panel.CurrentTrack = new ListTrackDto(Guid.Empty, middle, 0, "A long song title for layout verification", "Artist name", "Album", TimeSpan.FromMinutes(3), 0);
panel.Artwork = new DrawingImage { Drawing = new GeometryDrawing
    { Brush = Brushes.Coral, Geometry = new RectangleGeometry(new Rect(0, 0, 1600, 900)) } };
var layoutWindow = new Window { Content = main, Width = 360, Height = 800 };
layoutWindow.Show();
foreach (var size in new[] { new Size(360, 800), new Size(320, 568), new Size(640, 360) })
{
    layoutWindow.Width = size.Width;
    layoutWindow.Height = size.Height;
    Pump(100);
    var mobileSpectrum = main.GetVisualDescendants().OfType<AudioSpectrum>().Single(control => !control.ShowReflection);
    mobileSpectrum.IsVisible = true; // An audio backend is not needed for layout verification.
    Pump(50);
    var position = mobileSpectrum.TranslatePoint(new Point(0, mobileSpectrum.Bounds.Height), main)!.Value;
    Check(Math.Abs(position.Y - main.Bounds.Height) < 0.1, $"spectrum touches the toolbar bottom at {size}");
    var cover = main.GetVisualDescendants().OfType<ArtworkCarousel>().Single();
    Check(cover.Bounds.Width > 0 && cover.Bounds.Height > 0, $"artwork retains usable space at {size}");
}
layoutWindow.Close();
Console.WriteLine("All carousel checks passed.");

void Drag(Point from, Point to)
{
    window.MouseDown(from, MouseButton.Left);
    window.MouseMove(to);
    window.MouseUp(to, MouseButton.Left);
    Pump(600);
}
static void Pump(int milliseconds)
{
    using var cancellation = new CancellationTokenSource(milliseconds);
    Dispatcher.UIThread.MainLoop(cancellation.Token);
    Dispatcher.UIThread.RunJobs();
}
static void Check(bool result, string message)
{
    if (!result) throw new InvalidOperationException("FAIL: " + message);
    Console.WriteLine("PASS: " + message);
}
