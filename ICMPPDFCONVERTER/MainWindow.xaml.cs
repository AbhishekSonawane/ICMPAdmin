using Syncfusion.Windows.PdfViewer;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PdfEncryptionApp
{
    public partial class MainWindow : Window
    {
        private string inputFolderEncrypt;
        private string outputFolderEncrypt;

        private readonly byte[] key = Convert.FromBase64String("6IIIDHX77SvuDsJ/w1uOoL/OASjFHCTorfJdM5ifjP8=");
        private readonly byte[] iv = Convert.FromBase64String("fAacmGN+kHvRVlqNdWb7BA==");
        private string outputFolderEncrypttatic = "C:\\Users\\iariana\\Desktop\\d";
        private string decryptedFilePath = "";
        public MainWindow()
        {
            InitializeComponent();
            LoadTreeView();
        }

        private void BtnSelectInputFolderEncrypt_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = BrowseForFolder();
            if (!string.IsNullOrEmpty(folderPath))
            {
                TxtInputFolderEncrypt.Text = folderPath;
                inputFolderEncrypt = folderPath;
            }
        }

        private void BtnSelectOutputFolderEncrypt_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = BrowseForFolder();
            if (!string.IsNullOrEmpty(folderPath))
            {
                TxtOutputFolderEncrypt.Text = folderPath;
                outputFolderEncrypt = folderPath;
                LoadTreeView(); // Load the tree view for the new output folder
            }
        }

        private async void BtnEncryptFolder_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(inputFolderEncrypt) || string.IsNullOrEmpty(outputFolderEncrypt))
            {
                System.Windows.MessageBox.Show("Please select both input and output folders.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                ShowProgress();
                await Task.Run(() => EncryptFolder(inputFolderEncrypt, outputFolderEncrypt, key, iv));
                HideProgress();
                System.Windows.MessageBox.Show("Folder encryption completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadTreeView(); // Refresh the tree view to show newly encrypted files
            }
            catch (Exception ex)
            {
                HideProgress();
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EncryptFolder(string inputFolder, string outputFolder, byte[] key, byte[] iv)
        {
            Directory.CreateDirectory(outputFolder);

            var directories = Directory.GetDirectories(inputFolder, "*", SearchOption.AllDirectories);
            var files = Directory.GetFiles(inputFolder, "*", SearchOption.AllDirectories);

            int totalFiles = files.Length;
            int processedFiles = 0;

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(inputFolder, file);
                var outputFilePath = Path.Combine(outputFolder, relativePath + ".enc");
                var outputDir = Path.GetDirectoryName(outputFilePath);

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                EncryptFile(file, outputFilePath, key, iv);

                processedFiles++;
                UpdateProgress(processedFiles, totalFiles);
            }

            foreach (var directory in directories)
            {
                var relativePath = Path.GetRelativePath(inputFolder, directory);
                var outputDir = Path.Combine(outputFolder, relativePath);

                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
            }
        }

        private void EncryptFile(string inputFile, string outputFile, byte[] key, byte[] iv)
        {
            using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (FileStream fsEncrypted = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (CryptoStream cs = new CryptoStream(fsEncrypted, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    fsInput.CopyTo(cs);
                }
            }
        }

        private void UpdateProgress(int processedFiles, int totalFiles)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = (double)processedFiles / totalFiles * 100;
                StatusTextBlock.Text = $"Processing: {processedFiles}/{totalFiles} files";
            });
        }

        private void ShowProgress()
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = false;
                StatusTextBlock.Visibility = Visibility.Visible;
            });
        }

        private void HideProgress()
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                StatusTextBlock.Visibility = Visibility.Collapsed;
            });
        }

        private string BrowseForFolder()
        {
            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = folderDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    return folderDialog.SelectedPath;
                }
            }
            return null;
        }

        private void LoadTreeView()
        {
           
            if (string.IsNullOrEmpty(outputFolderEncrypttatic))
                return;

            DirectoryTreeView.Items.Clear();
            var rootDir = new DirectoryInfo(outputFolderEncrypttatic);
            var rootNode = new TreeViewItem { Header = rootDir.Name, Tag = rootDir.FullName, IsExpanded = true };
            DirectoryTreeView.Items.Add(rootNode);
            LoadSubDirectories(rootDir, rootNode);
        }

        private void LoadSubDirectories(DirectoryInfo dirInfo, TreeViewItem parentNode)
        {
            foreach (var directory in dirInfo.GetDirectories())
            {
                var directoryNode = new TreeViewItem
                {
                    Header = directory.Name,
                    Tag = directory.FullName
                };

                parentNode.Items.Add(directoryNode);
                LoadSubDirectories(directory, directoryNode);
            }
        }

        private void DirectoryTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DirectoryTreeView.SelectedItem is TreeViewItem selectedItem)
            {
                string selectedPath = selectedItem.Tag.ToString();
                DisplayFiles(selectedPath);
            }
        }

        private void DisplayFiles(string folderPath)
        {
            FileListView.Items.Clear();
            if (Directory.Exists(folderPath))
            {
                foreach (var file in Directory.GetFiles(folderPath))
                {
                    FileListView.Items.Add(Path.GetFileName(file));
                }
            }
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListView.SelectedItem != null)
            {
                string selectedFile = FileListView.SelectedItem.ToString();
                string filePath = Path.Combine(((TreeViewItem)DirectoryTreeView.SelectedItem).Tag.ToString(), selectedFile);
                // Assuming the selected file is encrypted and we need to decrypt it before displaying
                decryptedFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(filePath) + ".pdf");
                DecryptFile(filePath, decryptedFilePath, key, iv);
            }
        }
        private void DecryptFile(string inputFile, string outputFile, byte[] key, byte[] iv)
        {
            using (FileStream fsInput = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
            using (FileStream fsDecrypted = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (CryptoStream cs = new CryptoStream(fsInput, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cs.CopyTo(fsDecrypted);
                }
            }
        }
    }
}
