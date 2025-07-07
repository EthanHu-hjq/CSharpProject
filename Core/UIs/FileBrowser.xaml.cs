using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ToucanCore.UIs
{
    /// <summary>
    /// Interaction logic for FileBrowser.xaml
    /// </summary>
    public partial class FileBrowser : Window
    {
        public ProjectDirectory CurrentDir
        {
            get { return (ProjectDirectory)GetValue(CurrentDirProperty); }
            set { SetValue(CurrentDirProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CurrentDir.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentDirProperty =
            DependencyProperty.Register("CurrentDir", typeof(ProjectDirectory), typeof(FileBrowser), new PropertyMetadata(null));

        public FileBrowser(ProjectDirectory pd)
        {
            InitializeComponent();
            var btn_nav = new Button() { Content = ".", Tag = pd, AllowDrop = true };
            btn_nav.Drop += ListView_Drop;
            btn_nav.Click += Btn_nav_Click;
            wp_Nav.Children.Add(btn_nav);
            pd.Refresh();
            CurrentDir = pd;
        }

        private void Btn_nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                CurrentDir = btn.Tag as ProjectDirectory;
                CurrentDir.Refresh();
                var idx = wp_Nav.Children.IndexOf(btn);
                wp_Nav.Children.RemoveRange(idx + 1, wp_Nav.Children.Count - idx - 1);
            }
        }

        private void ListView_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView lv)
            {
                if (lv.SelectedItem is ProjectDirectory pd)
                {
                    if (pd.IsFile)
                    {
                        using(Process ps = new Process())
                        {
                            ps.StartInfo.FileName = pd.Name;
                            ps.StartInfo.WorkingDirectory = pd.Path;
                            ps.Start();
                        }    
                    }
                    else
                    {
                        CurrentDir = pd;
                        CurrentDir.Refresh();
                        var btn_nav = new Button() { Content = pd.Name, Tag = pd, AllowDrop = true };
                        btn_nav.Click += Btn_nav_Click;
                        btn_nav.Drop += ListView_Drop;
                        wp_Nav.Children.Add(btn_nav);
                    }
                }
            }
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Effects.HasFlag(DragDropEffects.Copy) || e.Effects.HasFlag(DragDropEffects.Move))
            {
                var data = e.Data.GetData("FileDrop");
                if (data is string[] srcs)
                {
                    string destdir = CurrentDir.Path;

                    if (sender is ListView lv)
                    {
                        object srcnode = e.OriginalSource;
                        do
                        {
                            if (srcnode is FrameworkElement ui)
                            {
                                srcnode = ui.Parent;
                            }
                            else if (srcnode is Button btn)
                            {
                            }

                            if (srcnode is GridViewRowPresenter gvrp)
                            {
                                if (gvrp.Content is ProjectDirectory pd)
                                {
                                    destdir = pd.Path;
                                }
                                break;
                            }
                        }
                        while (srcnode is FrameworkElement);
                    }
                    else if (sender is Button btn)
                    {
                        if (btn.Tag is ProjectDirectory pd)
                        {
                            destdir = pd.Path;
                        }
                    }
                    else
                    {
                        return;
                    }

                    var srcroot = System.IO.Directory.GetParent(srcs.FirstOrDefault()).FullName;

                    var islocal = srcroot == CurrentDir.Path;

                    foreach (var src in srcs)
                    {
                        var name = System.IO.Path.GetFileName(src);
                        var dest = System.IO.Path.Combine(destdir, name);

                        if (src == dest) return;
                        if (dest.StartsWith(src)) return;

                        if (System.IO.File.Exists(src))
                        {
                            if (System.IO.File.Exists(dest))
                            {
                                if (MessageBox.Show($"There File {name} exist, Do you want to overwrite it", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                {
                                    return;
                                }

                                File.Delete(dest);
                            }

                            if (islocal)
                            {
                                File.Move(src, dest);
                            }
                            else
                            {
                                File.Copy(src, dest);
                            }

                        }
                        else if (System.IO.Directory.Exists(src))
                        {
                            Directory.Move(src, dest);
                        }
                    }

                    CurrentDir.Refresh();
                }
            }
        }

        private void ListView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is ListView lv)
                {
                    ProjectDirectory pd = null;

                    object srcnode = e.OriginalSource;
                    do
                    {
                        if (srcnode is FrameworkElement ui)
                        {
                            srcnode = ui.Parent;
                        }
                        else if (srcnode is Button btn)
                        {
                        }

                        if (srcnode is GridViewRowPresenter gvrp)
                        {
                            if (gvrp.Content is ProjectDirectory content)
                            {
                                pd = content;
                            }
                            break;
                        }
                    }
                    while (srcnode is FrameworkElement);

                    if (pd != null)
                    {
                        IDataObject d = new DataObject();
                        d.SetData("FileDrop", new string[] { pd.Path });

                        try
                        {
                            DragDrop.DoDragDrop((DependencyObject)sender, d, DragDropEffects.Copy);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Warning");
                        }

                        //FileInfo fi = new FileInfo(pd.Path);
                        //FileSystemWatcher fsw = new FileSystemWatcher(pd.Path);
                    }
                }
            }
        }

        private void btn_Refresh_Click(object sender, RoutedEventArgs e)
        {
            CurrentDir.Refresh();
        }

        private void btn_Add_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if(ofd.ShowDialog() == true)
            {
                string destdir = CurrentDir.Path;
                foreach (var file in ofd.FileNames)
                {
                    try
                    {
                        var name = System.IO.Path.GetFileName(file);
                        var dest = System.IO.Path.Combine(destdir, name);

                        if (file == dest) continue;
                        if (System.IO.File.Exists(file))
                        {
                            if (System.IO.File.Exists(dest))
                            {
                                if (MessageBox.Show($"There File {name} exist, Do you want to overwrite it", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                                {
                                    return;
                                }

                                File.Delete(dest);
                            }
                            File.Copy(file, dest);

                        }

                        CurrentDir.Subs.Add(new ProjectDirectory(file));
                    }
                    catch
                    {
                        
                    }
                }
            }
        }

        private void btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (lv_Dir.SelectedItem is ProjectDirectory pd)
            {
                if(MessageBox.Show($"Are you sure to DELETE {pd.Name}", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    if(pd.IsFile)
                    {
                        File.Delete(System.IO.Path.Combine(CurrentDir.Path, pd.Name));
                    }
                    else
                    {
                        Directory.Delete(System.IO.Path.Combine(CurrentDir.Path, pd.Name), true);
                    }

                    CurrentDir.Subs.Remove(pd);
                }
            }
        }
    }

    public class ProjectDirectory
    {
        public string Path { get; private set; }
        public string Name { get; private set; }
        public DateTime LastWriteTime { get; }

        public ObservableCollection<ProjectDirectory> Subs { get; }

        public bool IsFile { get; private set; }
        public string Icon { get; }

        private ProjectDirectory()
        {
            Subs = new ObservableCollection<ProjectDirectory>();
        }
        public ProjectDirectory(string path) : this()
        {
            if (Directory.Exists(path))
            {
                Path = path;
                Name = System.IO.Path.GetFileName(Path);

                DirectoryInfo di = new DirectoryInfo(path);
                LastWriteTime = di.LastWriteTime;

                Icon = "";  // Copy from
            }
            else if (File.Exists(path))
            {
                Path = System.IO.Path.GetDirectoryName(path);
                Name = System.IO.Path.GetFileName(path);

                FileInfo fi = new FileInfo(path);
                LastWriteTime = fi.LastWriteTime;

                IsFile = true;
                Icon = "";

                return;
            }
            else
            {
                throw new InvalidDataException();
            }
        }

        public void Refresh()
        {
            Subs?.Clear();

            var subs = Directory.GetDirectories(Path);
            foreach (var sub in subs)
            {
                Subs.Add(new ProjectDirectory(sub));
            }

            var files = Directory.GetFiles(Path);
            foreach (var file in files)
            {
                Subs.Add(new ProjectDirectory(file));
            }
        }
    }
}
