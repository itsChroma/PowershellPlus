using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using JetBrains.Annotations;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using PowerPlus.Commands;
using PowerPlus.Models;
using PowerPlus.Views;
using Syroot.Windows.IO;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using SearchOption = System.IO.SearchOption;

namespace PowerPlus.ViewModel
{
    public class ViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        readonly ResourceDictionary _iconDictionary = 
            Application.LoadComponent(new Uri("/PowerPlus;component/Resources/Icons.xaml",
                UriKind.RelativeOrAbsolute)) as ResourceDictionary;

        public string ParentDirectory { get; set; }
        public string PreviousDirectory { get; set; }
        public string CurrentDirectory { get; set; }
        public string NextDirectory { get; set; }
        public string SelectedDriveSize { get; set; }
        public string SelectedFolderDetails { get; set; }
        public string NewFolderName { get; set; }
        public bool IsListView { get; set; }
        public string DriveSize { get; set; }

        public ObservableCollection<FileDetailsModel> FavoriteFolders { get; set; }
        public ObservableCollection<FileDetailsModel> RemoteFolders { get; set; }
        public ObservableCollection<FileDetailsModel> LibraryFolders { get; set; }
        public ObservableCollection<FileDetailsModel> ConnectedDevices { get; set; }
        public ObservableCollection<FileDetailsModel> NavigatesFolderFiles { get; set; }
        public ObservableCollection<SubMenuItemDetails> HomeTabSubMenuCollection { get; set; }
        public ObservableCollection<SubMenuItemDetails> ViewTabSubMenuCollection { get; set; }
        public ObservableCollection<FileDetailsModel> ClipBoardCollection { get; set; }

        public ObservableCollection<string> PathHistoryCollection { get; set; }
        internal int position = 0;
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public bool IsAtRootDirectory { get; set; }
        public bool IsMoveOperation { get; set; }

        internal bool _pathDisrupted;

        public bool PathDisrupted
        {
            get => _pathDisrupted;
            set
            {
                _pathDisrupted = value;
                if (_pathDisrupted)
                {
                    var tempCollection = new ObservableCollection<string>();
                    for (int i = position; i < PathHistoryCollection.Count - 1; i++)
                    {
                        tempCollection.Add(PathHistoryCollection[i]);
                    }

                    foreach (var path in tempCollection)
                    {
                        PathHistoryCollection.Remove(path);
                    }
                    OnPropertyChanged(nameof(PathHistoryCollection));
                    _pathDisrupted = false;
                }
            }
        }

        internal ReadOnlyCollection<string> tempFolderCollection;

        private BackgroundWorker bgGetFilesBackgroundWorker = new BackgroundWorker()
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = true
        };
        private BackgroundWorker bgGetFilesSizeBackgroundWorker = new BackgroundWorker()
        {
            WorkerSupportsCancellation = true
        };

        #region Functions

        internal bool IsFileHidden(string fileName)
        {
            var attr = FileAttributes.Normal;
            try
            {
                attr = File.GetAttributes(fileName);
            }
            catch
            {
                // Ignore
            }

            return attr.HasFlag(FileAttributes.Hidden);
        }

        internal bool IsReadOnly(string path)
        {

            try
            {
                if (Directory.Exists(path))
                    return (FileSystem.GetDirectoryInfo(path).Attributes & FileAttributes.ReadOnly) != 0;
                return (FileSystem.GetFileInfo(path).Attributes & FileAttributes.ReadOnly) != 0;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        internal bool IsDirectory(string fileName)
        {
            var attr = FileAttributes.Normal;
            try
            {
                attr = File.GetAttributes(fileName);
            }
            catch
            {
                // Ignore
            }

            return attr.HasFlag(FileAttributes.Directory);
        }

        internal string GetFilesExtension(string fileName)
        {
            if (fileName == null) return string.Empty;
            var extension = Path.GetExtension(fileName);
            var CultureInfo = Thread.CurrentThread.CurrentCulture;
            var textInfo = CultureInfo.TextInfo;
            var data = textInfo.ToTitleCase(extension.Replace(".", string.Empty));

            return data;
        }

        internal static readonly List<string> ImageExtensions = new List<string>()
        {
            ".jpg",
            ".png",
            ".jpeg",
            ".bmp",
            ".gif",
        };

        internal static readonly List<string> VideoExtensions = new List<string>()
        {
            ".wmv",
            ".mov",
            ".m4v",
            ".flv",
            ".mp4",
            ".avi",
            ".avchd",
            ".f4v",
            ".swf",
            ".mkv",
            ".webm",
        };

        internal PathGeometry GetImageForExtension(FileDetailsModel file)
        {
            var fileExtension = file.FileExtension;
            if (Directory.Exists(file.Path))
                return (PathGeometry)_iconDictionary["Folder"];

            if (file.IsImage)
                return (PathGeometry)_iconDictionary["ImageFile"];

            if (file.IsVideo)
                return (PathGeometry)_iconDictionary["VideoFile"];

            if ((PathGeometry)_iconDictionary[$"{fileExtension}File"] == null)
                return (PathGeometry)_iconDictionary["File"];

            return (PathGeometry)_iconDictionary[$"{fileExtension}File"];
        }

        void LoadDirectory(FileDetailsModel fileDetailsModel)
        {
            CanGoBack = position != 0;
            OnPropertyChanged(nameof(CanGoBack));

            NavigatesFolderFiles.Clear();
            tempFolderCollection = null;

            DriveSize = CalculateSize(new DriveInfo(fileDetailsModel.Path).TotalSize);
            OnPropertyChanged((nameof(DriveSize)));

            if (PathHistoryCollection != null && position > 0)
            {
                if (PathHistoryCollection.ElementAt(position) != fileDetailsModel.Path)
                    PathDisrupted = true;
            }

            if (!bgGetFilesBackgroundWorker.IsBusy)
                bgGetFilesBackgroundWorker.CancelAsync();

            bgGetFilesBackgroundWorker.RunWorkerAsync(fileDetailsModel); 
        }

        private void BgGetFilesBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var fileOrFolder = (FileDetailsModel)e.Argument;

            tempFolderCollection = new ReadOnlyCollectionBuilder<string>(FileSystem.GetDirectories(fileOrFolder.Path)
                .Concat(FileSystem.GetFiles(fileOrFolder.Path))).ToReadOnlyCollection();

            foreach (var filename in tempFolderCollection)
                bgGetFilesBackgroundWorker.ReportProgress(1, filename);


            CurrentDirectory = fileOrFolder.Path;
            OnPropertyChanged(nameof(CurrentDirectory));

            var root = Path.GetPathRoot(fileOrFolder.Path);
            if (string.IsNullOrWhiteSpace(CurrentDirectory)
                || CurrentDirectory == root)
            {
                IsAtRootDirectory = true;
                OnPropertyChanged(nameof(IsAtRootDirectory));
            }
            else
            {
                IsAtRootDirectory = false;
                OnPropertyChanged(nameof(IsAtRootDirectory));
            }
        }

        private void BgGetFilesBackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var fileName = e.UserState.ToString();
            var file = new FileDetailsModel();
            file.Name = Path.GetFileName(fileName);
            file.Path = fileName;

            file.CreatedOn = GetCreatedOn(fileName);
            file.DateModified = GetDateModified(fileName);
            file.AccessedOn = GetLastAccessedOn(fileName);

            file.IsHidden = IsFileHidden(fileName);
            file.IsReadOnly = IsReadOnly(fileName);
            file.IsDirectory = IsDirectory(fileName);
            file.FileExtension = GetFilesExtension(fileName);
            file.IsImage = ImageExtensions.Contains(file.FileExtension.ToLower());
            file.IsVideo = VideoExtensions.Contains(file.FileExtension.ToLower());
            file.FileIcon = GetImageForExtension(file);

            NavigatesFolderFiles.Add(file);
            OnPropertyChanged(nameof(NavigatesFolderFiles));
        }

        private void BgGetFilesBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var file in NavigatesFolderFiles)
            {
                var subWorker = new BackgroundWorker();
                subWorker.DoWork += (o, args) =>
                {
                    file.Size = CalculateSize(GetDirectorySize(file.Path));
                };
                subWorker.RunWorkerCompleted += (o, args) =>
                {
                    subWorker.Dispose();
                    CollectionViewSource.GetDefaultView(NavigatesFolderFiles).Refresh();
                };
                subWorker.RunWorkerAsync();
            }
        }

        private string CalculateSize(long bytes)
        {
            var suffix = new[] { "B", "KB", "MB", "GB", "TB" };
            float byteNumber = bytes;
            for (var i = 0; i < suffix.Length; i++)
            {
                if (byteNumber < 1000)
                {
                    if (i == 0)
                        return $"{byteNumber} {suffix[i]}";
                    else
                        return $"{byteNumber:0.#0} {suffix[i]}";
                }
                else
                {
                    byteNumber /= 1024;
                }
            }
            return $"{byteNumber:N} {suffix[suffix.Length-1]}";
        }

        internal static long GetDirectorySize(string directoryPath)
        {
            try
            {
                if (FileSystem.DirectoryExists(directoryPath))
                {
                    var d = new DirectoryInfo(directoryPath);
                    return d.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
                }

                return new FileInfo(directoryPath).Length;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
            catch (FileNotFoundException)
            {
                return 0;
            }
            catch (DirectoryNotFoundException)
            {
                return 0;
            }
        }

        private void BgGetFilesSizeBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var Size = NavigatesFolderFiles.Where(File => File.IsSelected && !File.IsDirectory)
                .Sum(x => new FileInfo(x.Path).Length);

            SelectedFolderDetails = CalculateSize(Size);
            OnPropertyChanged(nameof(SelectedFolderDetails));

            var Directories = NavigatesFolderFiles.Where(directory => directory.IsSelected && directory.IsDirectory);
            try
            {
                foreach (var directory in Directories)
                {
                    Size += GetDirectorySize(directory.Path);
                    SelectedFolderDetails = CalculateSize(Size);
                    OnPropertyChanged(nameof(SelectedFolderDetails));
                }
            }
            catch (InvalidOperationException)
            {
                // Ignore
            }

        }

        internal void UpdatePathHistory(string path)
        {
            if (PathHistoryCollection != null && !string.IsNullOrEmpty(path))
            {
                PathHistoryCollection.Add(path);
                position++;
                OnPropertyChanged(nameof(PathHistoryCollection));
            }
        }

        internal void PinFolder()
        {
            if (FavoriteFolders == null)
                FavoriteFolders = new ObservableCollection<FileDetailsModel>();

            try
            {
                var selectedFile =
                    NavigatesFolderFiles.Where(folder => folder.IsSelected && !folder.IsPinned && folder.IsDirectory);

                foreach (var directory in selectedFile)
                {
                    directory.IsPinned = true;
                    FavoriteFolders.Add(directory);
                    OnPropertyChanged(nameof(FavoriteFolders));
                }
            }
            catch
            {
                // Ignore
            }
        }

        internal void Copy()
        {
            if (ClipBoardCollection == null)
                ClipBoardCollection = new ObservableCollection<FileDetailsModel>();
            ClipBoardCollection.Clear();

            var selectedFiles = NavigatesFolderFiles.Where(File => File.IsSelected);
            foreach (var file in selectedFiles)
            {
                if(!ClipBoardCollection.Contains(file))
                    ClipBoardCollection.Add(file);
            }
            OnPropertyChanged(nameof(ClipBoardCollection));
            IsMoveOperation = false;
        }

        internal void Cut()
        {
            if (ClipBoardCollection == null)
                ClipBoardCollection = new ObservableCollection<FileDetailsModel>();
            ClipBoardCollection.Clear();

            var selectedFiles = NavigatesFolderFiles.Where(File => File.IsSelected);
            foreach (var file in selectedFiles)
            {
                if (!ClipBoardCollection.Contains(file))
                    ClipBoardCollection.Add(file);
            }
            OnPropertyChanged(nameof(ClipBoardCollection));
            IsMoveOperation = true;
        }

        internal void Paste(bool IsMoveOperation)
        {
            if (ClipBoardCollection != null && ClipBoardCollection.Count > 0)
            {
                var destinationPath = CurrentDirectory;
                if (!IsMoveOperation)
                {
                    foreach (var file in ClipBoardCollection)
                    {
                        var sourcePath = file.Path;
                        var destPath = CurrentDirectory + "\\" + file.Name;
                        destPath = Path.Combine(sourcePath, destPath);
                        var temp = Path.GetExtension(file.Path);

                        if (string.IsNullOrWhiteSpace(temp))
                            FileSystem.CopyDirectory(file.Path, destPath, UIOption.AllDialogs,
                                UICancelOption.DoNothing);
                        else
                            FileSystem.CopyFile(file.Path, destPath, UIOption.AllDialogs, UICancelOption.DoNothing);
                    }
                }
                else
                {
                    foreach (var file in ClipBoardCollection)
                    {
                        var sourcePath = file.Path;
                        var destPath = CurrentDirectory + "\\" + file.Name;
                        destPath = Path.Combine(sourcePath, destPath);
                        var temp = Path.GetExtension(file.Path);

                        if (string.IsNullOrWhiteSpace(temp))
                            FileSystem.MoveDirectory(file.Path, destPath, UIOption.AllDialogs,
                                UICancelOption.DoNothing);
                        else
                            FileSystem.MoveFile(file.Path, destPath, UIOption.AllDialogs, UICancelOption.DoNothing);
                    }
                }

                LoadDirectory(new FileDetailsModel()
                {
                    Path = destinationPath
                });
                IsMoveOperation = false;
            }
        }

        internal void Delete()
        {
            var selectedFiles = NavigatesFolderFiles.Where(file => file.IsSelected);
            if (selectedFiles.Count() > 1)
            {
                if (MessageBoxResult.Yes ==
                    MessageBox.Show(
                        $"Are you sure that you want to move these {selectedFiles.Count()} items to the Recycle Bin?",
                        "Delete Multiple Items", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No))
                {
                    foreach (var file in selectedFiles)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(Path.GetExtension(file.Path)))
                            {
                                FileSystem.DeleteDirectory(file.Path ?? string.Empty, UIOption.OnlyErrorDialogs,
                                    RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
                            }
                            else
                            {
                                FileSystem.DeleteFile(file.Path, UIOption.OnlyErrorDialogs,
                                    RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
                            }
                        }
                        catch
                        {
                            // Ignore
                        }
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(selectedFiles.ElementAt(0).Path))
                {
                    FileSystem.DeleteDirectory(selectedFiles.ElementAt(0).Path, UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
                }
                else
                {
                    FileSystem.DeleteFile(selectedFiles.ElementAt(0).Path, UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
                }
            }

            LoadDirectory(new FileDetailsModel() {Path = CurrentDirectory});
        }

        internal void Rename()
        {
            var selectedFiles =
                new ObservableCollection<FileDetailsModel>(NavigatesFolderFiles.Where(x => x.IsSelected));
            foreach (var file in selectedFiles)
            {
                if (file.IsSelected)
                {
                    restart:
                    try
                    {
                        new RenameDialog()
                        {
                            DataContext = this,
                            Owner = Application.Current.MainWindow,
                            ShowActivated = true,
                            ShowInTaskbar = false,
                            Topmost = true,
                            OldFolderName = $"Renaming: {file.Name}"
                        }.ShowDialog();

                        if (!string.IsNullOrWhiteSpace(NewFolderName))
                        {
                            if (file.IsDirectory)
                                FileSystem.RenameDirectory(file.Path, NewFolderName);
                            else
                                FileSystem.RenameFile(file.Path, $"{NewFolderName}.{file.FileExtension.ToLower()}");
                            file.Name = NewFolderName;
                            file.IsSelected = false;

                            NavigatesFolderFiles.Remove(file);
                            OnPropertyChanged(nameof(NavigatesFolderFiles));
                            NavigatesFolderFiles.Add(file);
                            OnPropertyChanged(nameof(NavigatesFolderFiles));

                            NewFolderName = string.Empty;
                            OnPropertyChanged(NewFolderName);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        goto restart;
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, exception.Source);
                    }
                }
            }
        }

        internal void CreateNewFolder()
        {
            CreateNewFolderCommand.Execute(null);
        }

        internal static string GetCreatedOn(string path)
        {
            try
            {
                if (FileSystem.DirectoryExists(path))
                {
                    return
                        $"{FileSystem.GetDirectoryInfo(path).CreationTime.ToShortDateString()} {FileSystem.GetDirectoryInfo(path).CreationTime.ToShortTimeString()}";
                }

                return
                    $"{FileSystem.GetFileInfo(path).CreationTime.ToShortDateString()} {FileSystem.GetFileInfo(path).CreationTime.ToShortTimeString()}";
            }
            catch (UnauthorizedAccessException)
            {
                return String.Empty;
            }
            catch (FileNotFoundException)
            {
                return String.Empty;
            }
            catch (DirectoryNotFoundException)
            {
                return String.Empty;
            }
        }

        internal static string GetDateModified(string path)
        {
            try
            {
                if (FileSystem.DirectoryExists(path))
                {
                    return
                        $"{FileSystem.GetDirectoryInfo(path).LastWriteTime.ToShortDateString()} {FileSystem.GetDirectoryInfo(path).LastWriteTime.ToShortTimeString()}";
                }

                return
                    $"{FileSystem.GetFileInfo(path).LastWriteTime.ToShortDateString()} {FileSystem.GetFileInfo(path).LastWriteTime.ToShortTimeString()}";
            }
            catch (UnauthorizedAccessException)
            {
                return String.Empty;
            }
            catch (FileNotFoundException)
            {
                return String.Empty;
            }
            catch (DirectoryNotFoundException)
            {
                return String.Empty;
            }
        }

        internal static string GetLastAccessedOn(string path)
        {
            try
            {
                if (FileSystem.DirectoryExists(path))
                {
                    return
                        $"{FileSystem.GetDirectoryInfo(path).LastAccessTime.ToShortDateString()} {FileSystem.GetDirectoryInfo(path).LastAccessTime.ToShortTimeString()}";
                }

                return
                    $"{FileSystem.GetFileInfo(path).LastAccessTime.ToShortDateString()} {FileSystem.GetFileInfo(path).LastAccessTime.ToShortTimeString()}";
            }
            catch (UnauthorizedAccessException)
            {
                return String.Empty;
            }
            catch (FileNotFoundException)
            {
                return String.Empty;
            }
            catch (DirectoryNotFoundException)
            {
                return String.Empty;
            }
        }

        internal void ShowProperties()
        {
            try
            {
                if (NavigatesFolderFiles.Count(file => file.IsSelected) == 1)
                {
                    var f = NavigatesFolderFiles.Where(file => file.IsSelected).ToArray();
                    new PropertiesDialog()
                    {
                        FileName = f[0].Name,
                        Icon = f[0].FileIcon,
                        FileExtension = f[0].FileExtension,
                        FullPath = f[0].Path,
                        Size = CalculateSize(GetDirectorySize(f[0].Path)),
                        CreatedOn = GetCreatedOn(f[0].Path),
                        DateModified = GetDateModified(f[0].Path),
                        AccessedOn = GetLastAccessedOn(f[0].Path),
                        IsReadOnly = f[0].IsReadOnly,
                        IsHidden = f[0].IsHidden,
                        Owner = Application.Current.MainWindow,
                        ShowInTaskbar = false,
                        Topmost = true
                    }.ShowDialog();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, exception.Source);
            }
        }

        public ViewModel()
        {
            RemoteFolders = new ObservableCollection<FileDetailsModel>()
            {
                new FileDetailsModel()
                {
                    Name = "OneDrive",
                    IsDirectory = true,
                    Path = Environment.GetEnvironmentVariable("OneDriveConsumer"),
                    FileIcon = (PathGeometry)_iconDictionary["OneDrive"]
                },

                new FileDetailsModel()
                {
                    Name = "Google Drive",
                    IsDirectory = true,
                    Path = "",
                    FileIcon = (PathGeometry)_iconDictionary["GoogleDrive"]
                },

                new FileDetailsModel()
                {
                    Name = "DevOps",
                    IsDirectory = true,
                    Path = "",
                    FileIcon = (PathGeometry)_iconDictionary["DevOps"]
                },

                new FileDetailsModel()
                {
                    Name = "GitHub",
                    IsDirectory = true,
                    Path = "",
                    FileIcon = (PathGeometry)_iconDictionary["GitHub"]
                },
            };

            LibraryFolders = new ObservableCollection<FileDetailsModel>()
            {
                new FileDetailsModel()
                {
                    Name = "Desktop",
                    IsDirectory = true,
                    Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    FileIcon = (PathGeometry)_iconDictionary["DesktopFolder"]
                },

                new FileDetailsModel()
                {
                    Name = "Documents",
                    IsDirectory = true,
                    Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    FileIcon = (PathGeometry)_iconDictionary["DocumentsFolder"]
                },

                new FileDetailsModel()
                {
                    Name = "Downloads",
                    IsDirectory = true,
                    Path = new KnownFolder(KnownFolderType.Downloads).Path,
                    FileIcon = (PathGeometry)_iconDictionary["DownloadsFolder"]
                },
            };

            ConnectedDevices = new ObservableCollection<FileDetailsModel>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                var name = string.IsNullOrEmpty(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
                ConnectedDevices.Add(new FileDetailsModel()
                {
                    Name = $"{name} ({drive.Name.Replace(@"\", "")})",
                    Path = drive.RootDirectory.FullName,
                    IsDirectory = true,
                    FileIcon = drive.Name.Contains("C:")
                        ? (PathGeometry)_iconDictionary["CDrive"]
                        : (PathGeometry)_iconDictionary["NormalDrive"]
                });
            }

            LoadSubMenuCollectionCommand.Execute(null);

            CurrentDirectory = @"C:\";
            OnPropertyChanged(nameof(CurrentDirectory));

            NavigatesFolderFiles = new ObservableCollection<FileDetailsModel>();
            bgGetFilesBackgroundWorker.DoWork += BgGetFilesBackgroundWorker_DoWork;
            bgGetFilesBackgroundWorker.ProgressChanged += BgGetFilesBackgroundWorker_ProgressChanged;
            bgGetFilesBackgroundWorker.RunWorkerCompleted += BgGetFilesBackgroundWorker_RunWorkerCompleted;

            LoadDirectory(new FileDetailsModel()
            {
                Path = CurrentDirectory
            });

            PathHistoryCollection = new ObservableCollection<string>();
            PathHistoryCollection.Add(CurrentDirectory);

            CanGoBack = position != 0;
            OnPropertyChanged(nameof(CanGoBack));
        }

        #endregion

        #region Commands

        private ICommand _openSettingsCommand;

        public ICommand openSettingsCommand
        {
            get
            {
                return _openSettingsCommand ??
                       (_openSettingsCommand = new Command(() => Process.Start("ms-settings:home")));
            }
        }

        private ICommand _openUserProfileCommand;

        public ICommand openUserProfileCommand
        {
            get
            {
                return _openUserProfileCommand ??
                       (_openUserProfileCommand = new Command(() => Process.Start("ms-settings:yourinfo")));
            }
        }

        private ICommand _loadSubMenuCollectionCommand;

        public ICommand LoadSubMenuCollectionCommand =>
            _loadSubMenuCollectionCommand ??
           (_loadSubMenuCollectionCommand = new Command(() =>
           {
               HomeTabSubMenuCollection = new ObservableCollection<SubMenuItemDetails>
               {
                   new SubMenuItemDetails()
                   {
                       Name = "Pin",
                       Icon = (PathGeometry) _iconDictionary["Pin"]
                   },

                   new SubMenuItemDetails()
                   {
                       Name = "Copy",
                       Icon = (PathGeometry) _iconDictionary["Copy"]
                   },

                   new SubMenuItemDetails()
                   {
                       Name = "Cut",
                       Icon = (PathGeometry) _iconDictionary["Cut"]
                   },

                   new SubMenuItemDetails()
                   {
                       Name = "Paste",
                       Icon = (PathGeometry) _iconDictionary["Paste"]
                   },

                   new SubMenuItemDetails()
                   {
                       Name = "Delete",
                       Icon = (PathGeometry) _iconDictionary["Delete"]
                   },

                   new SubMenuItemDetails()
                   {
                       Name = "Rename",
                       Icon = (PathGeometry) _iconDictionary["Rename"]
                   },

                   new SubMenuItemDetails()
                   {
                       Name = "New Folder",
                       Icon = (PathGeometry) _iconDictionary["NewFolder"]
                   },

                   new SubMenuItemDetails()
                   {
                       Name = "Properties",
                       Icon = (PathGeometry) _iconDictionary["FileSettings"]
                   }
               };

               ViewTabSubMenuCollection = new ObservableCollection<SubMenuItemDetails>
               {
                   new SubMenuItemDetails()
                   {
                       Name = "List",
                       Icon = (PathGeometry) _iconDictionary["ListView"]
                   },

                   new SubMenuItemDetails()
                   {
                       Name = "Tile",
                       Icon = (PathGeometry) _iconDictionary["TileView"]
                   },
               };
           }));

        public ICommand _getFilesListCommand;

        

        public ICommand GetFilesListCommand =>
            _getFilesListCommand ?? (_getFilesListCommand = new RelayCommand(parameter =>
            {
                var file = parameter as FileDetailsModel;
                if (file == null) return;

                SelectedFolderDetails = string.Empty;
                OnPropertyChanged(nameof(SelectedFolderDetails));

                if (Directory.Exists(file.Path))
                {
                    UpdatePathHistory(file.Path);
                    LoadDirectory(file);
                }

                else
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(file.Path));
                    }
                    catch (Win32Exception w3Ex)
                    {
                        MessageBox.Show(w3Ex.Message, w3Ex.Source);
                    }
                    catch (InvalidOperationException ioEx)
                    {
                        MessageBox.Show($"{file.Name} is not installed on this computer.", ioEx.Source);
                    }
                }
            }));

        public ICommand _getFilesSizeCommand;

        public ICommand GetFilesSizeCommand =>
            _getFilesSizeCommand ?? (_getFilesSizeCommand = new RelayCommand(parameter =>
            {
                var file = parameter as FileDetailsModel;
                if (file == null) return;
                ;
                SelectedFolderDetails = "Calculating Size...";
                OnPropertyChanged(nameof(SelectedFolderDetails));
                bgGetFilesSizeBackgroundWorker.DoWork += BgGetFilesSizeBackgroundWorker_DoWork;
                bgGetFilesSizeBackgroundWorker.DoWork += BgGetFilesSizeBackgroundWorker_DoWork;

                if (bgGetFilesSizeBackgroundWorker.IsBusy)
                    bgGetFilesSizeBackgroundWorker.CancelAsync();

                if (bgGetFilesSizeBackgroundWorker.CancellationPending)
                {
                    bgGetFilesSizeBackgroundWorker.Dispose();
                    bgGetFilesSizeBackgroundWorker = new BackgroundWorker()
                    {
                        WorkerSupportsCancellation = true
                    };
                }

                bgGetFilesSizeBackgroundWorker.RunWorkerAsync();
            }));

        protected ICommand _goToPreviousDirectoryCommand;

        public ICommand GoToPreviousDirectoryCommand =>
            _goToPreviousDirectoryCommand ?? (_goToPreviousDirectoryCommand = new Command(() =>
            {
                if (position >= 1)
                {
                    position--;
                    LoadDirectory(new FileDetailsModel()
                    {
                        Path = PathHistoryCollection.ElementAt(position)
                    });

                    CanGoForward = true;
                    OnPropertyChanged(nameof(CanGoForward));

                    PathDisrupted = false;
                    OnPropertyChanged(nameof(PathDisrupted));
                }
            }));

        protected ICommand _goToForwardDirectoryCommand;

        public ICommand GoToForwardDirectoryCommand =>
            _goToForwardDirectoryCommand ?? (_goToForwardDirectoryCommand = new Command(() =>
            {
                if (position < PathHistoryCollection.Count - 1)
                {
                    position++;
                    LoadDirectory(new FileDetailsModel()
                    {
                        Path = PathHistoryCollection.ElementAt(position)
                    });

                    CanGoForward =
                        position < PathHistoryCollection.Count - 1 &&
                        position != PathHistoryCollection.Count - 1;

                    OnPropertyChanged(nameof(CanGoForward));
                }
            }));

        protected ICommand _goToParentDirectoryCommand;

        public ICommand GoToParentDirectoryCommand =>
            _goToParentDirectoryCommand ?? (_goToParentDirectoryCommand = new Command(() =>
            {
                var ParentDirectory = string.Empty;
                PathDisrupted = true;

                var d = new DirectoryInfo(CurrentDirectory);

                if (d.Parent != null)
                {
                    ParentDirectory = d.Parent.FullName;
                    IsAtRootDirectory = false;
                    OnPropertyChanged(nameof(IsAtRootDirectory));
                }
                else if (d.Parent == null)
                {
                    IsAtRootDirectory = true;
                    OnPropertyChanged(nameof(IsAtRootDirectory));
                    return;;
                }
                else
                {
                    ParentDirectory = d.Parent.ToString()
                        .Split(Path.DirectorySeparatorChar)[1];
                }

                GetFilesListCommand.Execute(new FileDetailsModel()
                {
                    Path = ParentDirectory
                });
            }));

        protected ICommand _navigateToPathCommand;

        public ICommand NavigateToPathCommand =>
            _navigateToPathCommand ?? (_navigateToPathCommand = new RelayCommand((parameter) =>
            {
                var path = parameter as string;
                if(!string.IsNullOrEmpty(path))
                    GetFilesListCommand.Execute(new FileDetailsModel()
                    {
                        Path = path
                    });
            }));

        protected ICommand _subMenuFileOperationCommand;

        protected ICommand _unPinFavoriteFolderCommand;

        public ICommand UnPinFavoriteFolderCommand =>
            _unPinFavoriteFolderCommand ?? (_unPinFavoriteFolderCommand = new RelayCommand((parameter) =>
            {
                var folder = parameter as FileDetailsModel;
                if (folder == null) return;

                folder.IsPinned = false;
                FavoriteFolders.Remove(folder);
                OnPropertyChanged(nameof(FavoriteFolders));
            }));

        public ICommand SubMenuFileOperationCommand =>
            _subMenuFileOperationCommand ?? (_subMenuFileOperationCommand = new RelayCommand((parameter) =>
            {
                var subMenuItem = parameter as SubMenuItemDetails;
                if (subMenuItem == null) return;
                try
                {
                    switch (subMenuItem.Name)
                    {
                        case "Pin":
                            PinFolder();
                            break;
                        case "Copy":
                            Copy();
                            break;
                        case "Cut":
                            Cut();
                            break;
                        case "Paste":
                            Paste(IsMoveOperation);
                            break;
                        case "Delete":
                            Delete();
                            break;
                        case "Rename":
                            Rename();
                            break;
                        case "New Folder":
                            CreateNewFolder();
                            break;
                        case "Properties":
                            ShowProperties();
                            break;
                        case "List":
                            IsListView = true;
                            OnPropertyChanged(nameof(IsListView));
                            break;
                        case "Tile":
                            IsListView = false;
                            OnPropertyChanged(nameof(IsListView));
                            break;
                        default:
                            return;
                    }
                }
                catch{}
            }));

        protected ICommand _createNewFolderCommand;

        public ICommand CreateNewFolderCommand =>
            _createNewFolderCommand ?? (_createNewFolderCommand = new Command(() =>
            {
                foreach (var folder in NavigatesFolderFiles.Where(f => f.IsSelected))
                    folder.IsSelected = false;
                OnPropertyChanged(nameof(NavigatesFolderFiles));

                var i = FileSystem.GetDirectories(CurrentDirectory)
                    .Count(x => x.Contains("New Folder"));
                var path = i == 0
                    ? $"{CurrentDirectory}\\NewFolder"
                    : $"{CurrentDirectory}\\New Folder{i}";
                Directory.CreateDirectory(path);

                var file = new FileDetailsModel();
                file.Name = Path.GetFileName(path);
                file.Path = path;
                file.IsDirectory = true;
                file.FileExtension = string.Empty;
                file.IsImage = false;
                file.IsVideo = false;
                file.FileIcon = GetImageForExtension(file);
                file.IsSelected = true;

                NavigatesFolderFiles.Add(file);
                OnPropertyChanged(nameof(NavigatesFolderFiles));

                Rename();
            }));

        protected ICommand _sortFilesCommand;

        private bool SortedByAscending { get; set; }
        public string SortedBy { get; set; }

        public ICommand SortFilesCommand =>
            _sortFilesCommand ?? (_sortFilesCommand = new RelayCommand((parameter) =>
            {
                var header = parameter as GridViewColumnHeader;
                if (header == null ||
                    string.IsNullOrWhiteSpace(header.Content.ToString())) return;

                SortedByAscending = !SortedByAscending;
                OnPropertyChanged(nameof(SortedByAscending));

                CollectionViewSource.GetDefaultView(NavigatesFolderFiles)
                    .SortDescriptions.Clear();
                if (SortedByAscending)
                {
                    CollectionViewSource.GetDefaultView(NavigatesFolderFiles)
                        .SortDescriptions
                        .Add(new SortDescription(
                            header.Content.ToString().Replace(" ", ""),
                                        ListSortDirection.Descending));
                }
                else
                {
                    CollectionViewSource.GetDefaultView(NavigatesFolderFiles)
                        .SortDescriptions
                        .Add(new SortDescription(
                            header.Content.ToString().Replace(" ", ""),
                            ListSortDirection.Ascending));
                }

                SortedBy = (string) header.Content;
                OnPropertyChanged(nameof(SortedBy));
            }));
        #endregion
    }
}