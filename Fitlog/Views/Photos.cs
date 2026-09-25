using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private string? _openAlbum;
    private readonly HashSet<string> _comparison = [];
    private Button? _compareButton;
    private Control PhotosPage()
    {
        _comparison.IntersectWith(_data.Photos.Select(x => x.Id));
        var page = Stack(24); var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(Button("＋  New album", () => EditAlbum(), true));
        var add = Button("＋  Add photos", () => AddPhotos()); add.Name = "AddPhotos"; add.Margin = new Thickness(10, 0); actions.Children.Add(add);
        page.Children.Add(Split(Heading("A different perspective", "Progress Photos", "Your progress, in your own time. Photos stay on this device."), actions));
        var compareActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        _compareButton = Button("Compare photos", ComparePhotos, true); _compareButton.Name = "ComparePhotos"; UpdateCompareButton(); compareActions.Children.Add(_compareButton);
        compareActions.Children.Add(Button("Clear selection", () => { _comparison.Clear(); RenderPage(); }));
        compareActions.Children.Add(T("Select exactly two photos to compare.", 12, color: Muted)); page.Children.Add(compareActions);
        if (_openAlbum == null)
        {
            var albums = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (var album in _data.Albums.OrderByDescending(x => x.Date))
            {
                var photos = _data.Photos.Where(x => x.AlbumId == album.Id).OrderByDescending(x => x.Date).ToList();
                var content = Stack(12);
                if (photos.Count > 0) content.Children.Add(Thumbnail(photos[0], 130)); else content.Children.Add(new Border { Height = 130, CornerRadius = new(12), Background = Raised, Child = T("Albums", 24, true, Muted) });
                content.Children.Add(T(album.Title, 18, true)); content.Children.Add(T($"{DateText(album.Date)} · {photos.Count} {Tr("Photos")}", 12, color: Muted));
                var open = Button(content, () => { _openAlbum = album.Id; RenderPage(); }); open.Name = "OpenAlbum"; open.Width = 260; open.Margin = new Thickness(0, 0, 14, 14); albums.Children.Add(open);
            }
            if (albums.Children.Count > 0) { page.Children.Add(T("Albums", 23, true)); page.Children.Add(albums); }
        }
        var current = _data.Albums.FirstOrDefault(x => x.Id == _openAlbum);
        if (current != null)
        {
            var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 }; header.Children.Add(Button("Back", () => { _openAlbum = null; RenderPage(); })); header.Children.Add(T(current.Title, 24, true)); header.Children.Add(Button("Edit", () => EditAlbum(current))); page.Children.Add(header);
        }
        else page.Children.Add(T("All photos", 24, true));
        var visible = _data.Photos.Where(x => _openAlbum == null || x.AlbumId == _openAlbum).OrderByDescending(x => x.Date).ThenBy(x => x.Id).ToList();
        if (visible.Count == 0)
        {
            page.Children.Add(Empty(current == null ? "See how far you have come" : "This album is empty", current == null ? "Add photos or create an album to collect a whole progress session." : "Add multiple photos to keep them together in this album.", "Add photos", () => AddPhotos())); return page;
        }
        var wrap = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var photo in visible)
        {
            var stack = Stack(12);
            var open = Button(Thumbnail(photo, 235), () => ShowPhoto(photo, visible)); open.Name = "OpenPhoto"; open.Padding = new(0); open.HorizontalAlignment = HorizontalAlignment.Stretch; ToolTip.SetTip(open, Tr("Open photo")); stack.Children.Add(open);
            stack.Children.Add(T(DateText(photo.Date), 17, true)); if (!string.IsNullOrWhiteSpace(photo.Caption)) stack.Children.Add(T(photo.Caption, 13, color: Muted));
            var albumName = _data.Albums.FirstOrDefault(x => x.Id == photo.AlbumId)?.Title; if (albumName != null) stack.Children.Add(T(albumName, 11, color: Muted));
            var select = new CheckBox { Name = "SelectPhoto", Content = Tr("Select for comparison"), IsChecked = _comparison.Contains(photo.Id), FontSize = 12 };
            select.IsCheckedChanged += (_, _) =>
            {
                if (select.IsChecked == true)
                {
                    if (_comparison.Count >= 2 && !_comparison.Contains(photo.Id)) { select.IsChecked = false; _status.Text = Tr("Two photos selected. Clear one before selecting another."); return; }
                    _comparison.Add(photo.Id);
                }
                else _comparison.Remove(photo.Id);
                UpdateCompareButton();
            };
            stack.Children.Add(select); stack.Children.Add(Button("Move to album", () => MovePhoto(photo))); stack.Children.Add(Button("Delete photo", () => DeleteRecord("photo", photo.Id)));
            var card = Card(stack, 16); card.Width = 280; card.Margin = new Thickness(0, 0, 16, 16); wrap.Children.Add(card);
        }
        page.Children.Add(wrap); return page;
    }
    private Control Thumbnail(ProgressPhoto photo, double height)
    {
        try { var bitmap = DecodePhoto(photo, 640); _images.Add(bitmap); return new Image { Source = bitmap, Height = height, Stretch = Stretch.UniformToFill, FlowDirection = FlowDirection.LeftToRight }; }
        catch { return new Border { Height = height, Child = T("This photo could not be displayed.", 12, color: Muted) }; }
    }
    private static Bitmap DecodePhoto(ProgressPhoto photo, int width = 1800)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(photo.Base64)); return Bitmap.DecodeToWidth(stream, width);
    }
    private void UpdateCompareButton()
    {
        if (_compareButton == null) return; _compareButton.Content = $"{Tr("Compare photos")} ({_comparison.Count}/2)"; _compareButton.IsEnabled = _comparison.Count == 2;
    }
    private async Task EditAlbum(PhotoAlbum? album = null)
    {
        var dialog = Dialog(album == null ? "New album" : "Edit"); var form = Stack(18); form.Children.Add(T("Album", 26, true));
        var title = new TextBox { Name = "AlbumTitle", Text = album?.Title ?? "", MaxLength = 100 }; var date = DatePicker(album?.Date ?? Today);
        form.Children.Add(Field("Album name", title)); form.Children.Add(Field("Album date", date));
        EditorContent(dialog, form, () => { if (string.IsNullOrWhiteSpace(title.Text)) throw new InvalidDataException("Enter an album name."); var id = album?.Id ?? Guid.NewGuid().ToString("N"); _repository.Save("album", id, new PhotoAlbum(id, title.Text.Trim(), Selected(date))); _openAlbum = id; });
        await dialog.ShowDialog(this);
    }
    private ComboBox AlbumPicker(string? selectedId)
    {
        var options = new List<ComboBoxItem> { new() { Content = Tr("Without album"), Tag = null } };
        options.AddRange(_data.Albums.Select(x => new ComboBoxItem { Content = x.Title, Tag = x.Id }));
        return new ComboBox { Name = "PhotoAlbum", ItemsSource = options, SelectedIndex = Math.Max(0, options.FindIndex(x => Equals(x.Tag, selectedId))), HorizontalAlignment = HorizontalAlignment.Stretch };
    }
    private async Task MovePhoto(ProgressPhoto photo)
    {
        var dialog = Dialog("Move to album"); var form = Stack(18); var albums = AlbumPicker(photo.AlbumId); form.Children.Add(Field("Album", albums));
        EditorContent(dialog, form, () => _repository.SavePhotos([photo with { AlbumId = (albums.SelectedItem as ComboBoxItem)?.Tag as string }])); await dialog.ShowDialog(this);
    }
    private async Task AddPhotos()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = Tr("Choose progress photos"), AllowMultiple = true, FileTypeFilter = [new FilePickerFileType(Tr("Photos")) { Patterns = ["*.jpg", "*.jpeg", "*.png"] }] });
        if (files.Count == 0) return;
        if (files.Count > 20) throw new InvalidDataException("Select up to 20 photos at a time.");
        var photos = new List<byte[]>();
        foreach (var file in files)
        {
            await using var stream = await file.OpenReadAsync(); if (stream.Length > 10_000_000) throw new InvalidDataException("Please choose images smaller than 10 MB each.");
            using var memory = new MemoryStream(); await stream.CopyToAsync(memory); var bytes = memory.ToArray();
            using var validation = new MemoryStream(bytes); using var bitmap = Bitmap.DecodeToWidth(validation, 128); photos.Add(bytes);
        }
        await SavePhotoBatch(photos);
    }
    internal async Task SavePhotoBatch(IReadOnlyList<byte[]> photos)
    {
        var dialog = Dialog("Add photos"); var form = Stack(18); form.Children.Add(T($"{Tr("Add photos")} · {photos.Count}", 26, true));
        var currentAlbum = _data.Albums.FirstOrDefault(x => x.Id == _openAlbum); var date = DatePicker(currentAlbum?.Date ?? Today); var caption = new TextBox { Watermark = "A note about this moment", MaxLength = 500 }; var albums = AlbumPicker(_openAlbum);
        form.Children.Add(Field("Date", date)); form.Children.Add(Field("Caption", caption)); form.Children.Add(Field("Album", albums));
        EditorContent(dialog, form, () =>
        {
            var selectedDate = Selected(date); var albumId = (albums.SelectedItem as ComboBoxItem)?.Tag as string;
            _repository.SavePhotos(photos.Select(bytes => new ProgressPhoto(Guid.NewGuid().ToString("N"), selectedDate, caption.Text?.Trim() ?? "", Convert.ToBase64String(bytes)) { AlbumId = albumId }));
        }); await dialog.ShowDialog(this);
    }
    private async Task ShowPhoto(ProgressPhoto photo, IReadOnlyList<ProgressPhoto> gallery)
    {
        var dialog = Dialog("Photo viewer"); dialog.Width = 1050; dialog.Height = 820; dialog.SizeToContent = SizeToContent.Manual; dialog.CanResize = true;
        var root = new Grid { RowDefinitions = new("Auto,*,Auto"), RowSpacing = 16 }; var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        int index = gallery.ToList().FindIndex(x => x.Id == photo.Id); Bitmap? bitmap = null;
        var picture = new Image { Stretch = Stretch.Uniform, Name = "FullPhoto", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FlowDirection = FlowDirection.LeftToRight };
        var viewport = new ScrollViewer { Content = picture, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, FlowDirection = FlowDirection.LeftToRight };
        var zoom = new Slider { Name = "PhotoZoom", Minimum = .5, Maximum = 3, Value = 1, Width = 180, VerticalAlignment = VerticalAlignment.Center, FlowDirection = FlowDirection.LeftToRight };
        var caption = T("", 14, color: Muted);
        void ResizePhoto()
        {
            if (bitmap == null) return;
            var fit = Math.Min(Math.Max(200, viewport.Bounds.Width - 20) / bitmap.PixelSize.Width, Math.Max(200, viewport.Bounds.Height - 20) / bitmap.PixelSize.Height);
            picture.Width = bitmap.PixelSize.Width * fit * zoom.Value; picture.Height = bitmap.PixelSize.Height * fit * zoom.Value;
        }
        void LoadPhoto()
        {
            var next = DecodePhoto(gallery[index]); picture.Source = next; bitmap?.Dispose(); bitmap = next;
            caption.Text = $"{DateText(gallery[index].Date)} · {gallery[index].Caption}  ({index + 1}/{gallery.Count})"; zoom.Value = 1; ResizePhoto();
        }
        toolbar.Children.Add(Button("Previous", () => { index = (index - 1 + gallery.Count) % gallery.Count; LoadPhoto(); }));
        toolbar.Children.Add(Button("Next", () => { index = (index + 1) % gallery.Count; LoadPhoto(); })); toolbar.Children.Add(T("Zoom", 12)); toolbar.Children.Add(zoom); toolbar.Children.Add(Button("Fit", () => zoom.Value = 1)); toolbar.Children.Add(Button("Close", () => dialog.Close()));
        zoom.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) ResizePhoto(); }; viewport.SizeChanged += (_, _) => ResizePhoto();
        root.Children.Add(toolbar); Grid.SetRow(viewport, 1); root.Children.Add(viewport); Grid.SetRow(caption, 2); root.Children.Add(caption); dialog.Content = root;
        try { LoadPhoto(); await dialog.ShowDialog(this); } finally { picture.Source = null; bitmap?.Dispose(); }
    }
    private async Task ComparePhotos()
    {
        var photos = _data.Photos.Where(x => _comparison.Contains(x.Id)).OrderBy(x => x.Date).ToList();
        if (photos.Count != 2) { await Message("Compare photos", "Select exactly two photos to compare."); return; }
        var dialog = Dialog("Compare photos"); dialog.Width = 1150; dialog.Height = 800; dialog.SizeToContent = SizeToContent.Manual; dialog.CanResize = true;
        var root = new Grid { RowDefinitions = new("Auto,*"), RowSpacing = 20 }; root.Children.Add(Split(T("Compare photos", 26, true), Button("Close", () => dialog.Close())));
        var columns = new Grid { ColumnDefinitions = new("*,*"), ColumnSpacing = 20, FlowDirection = FlowDirection.LeftToRight }; var bitmaps = new List<Bitmap>();
        try
        {
            for (int i = 0; i < 2; i++)
            {
                var photo = photos[i]; var bitmap = DecodePhoto(photo); bitmaps.Add(bitmap);
                var panel = new Grid { RowDefinitions = new("Auto,*,Auto"), RowSpacing = 12, FlowDirection = FlowDirection };
                panel.Children.Add(T($"{Tr(i == 0 ? "Before" : "After")} · {DateText(photo.Date)}", 18, true));
                var image = new Image { Source = bitmap, Stretch = Stretch.Uniform, Name = "ComparisonImage", FlowDirection = FlowDirection.LeftToRight }; Grid.SetRow(image, 1); panel.Children.Add(image);
                var caption = T(photo.Caption, 13, color: Muted); Grid.SetRow(caption, 2); panel.Children.Add(caption); Grid.SetColumn(panel, i); columns.Children.Add(panel);
            }
            Grid.SetRow(columns, 1); root.Children.Add(columns); dialog.Content = root; await dialog.ShowDialog(this);
        }
        finally { foreach (var bitmap in bitmaps) bitmap.Dispose(); }
    }
}
