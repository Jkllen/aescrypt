using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace aescrypt
{
    public partial class MainWindow : Window
    {
        private string outputFolder = AppDomain.CurrentDomain.BaseDirectory;
        private readonly byte[] key = Encoding.UTF8.GetBytes("12345678901234567890123456789012");
        private readonly byte[] iv = new byte[16];

        public MainWindow()
        {
            InitializeComponent();
            OutputFolderText.Text = $"Current Output Folder: {outputFolder}";
        }

        private void SetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select a folder",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = Path.GetDirectoryName(dialog.FileName)!;

                outputFolder = selectedPath;
                OutputFolderText.Text = $"Current Output Folder: {outputFolder}";
                StatusText.Text = "Output folder updated.";
            }
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter text to encrypt.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Generate random AES key and IV
                byte[] key;
                byte[] iv;
                string encrypted = Encrypt(input, out key, out iv);
                OutputTextBox.Text = encrypted;

                // Save encrypted text
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = "encrypted_output.txt",
                    InitialDirectory = outputFolder
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, encrypted);

                    // Save key and IV with same name pattern
                    string baseName = Path.GetFileNameWithoutExtension(saveDialog.FileName);
                    string dir = Path.GetDirectoryName(saveDialog.FileName)!;

                    string keyPath = Path.Combine(dir, $"{baseName}_key.bin");
                    string ivPath = Path.Combine(dir, $"{baseName}_iv.bin");

                    File.WriteAllBytes(keyPath, key);
                    File.WriteAllBytes(ivPath, iv);

                    StatusText.Text = $"Encrypted successfully.\nKey/IV saved in same folder.";
                    LastSavedPathBox.Text = saveDialog.FileName;
                }
                else
                {
                    StatusText.Text = "Encryption canceled.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Encryption Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter text to decrypt.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Ask user to locate the encrypted file
                var openDialog = new OpenFileDialog
                {
                    Title = "Select Encrypted File",
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    InitialDirectory = outputFolder
                };

                if (openDialog.ShowDialog() != true)
                {
                    StatusText.Text = "Decryption canceled.";
                    return;
                }

                string filePath = openDialog.FileName;
                string dir = Path.GetDirectoryName(filePath)!;
                string baseName = Path.GetFileNameWithoutExtension(filePath);

                string keyPath = Path.Combine(dir, $"{baseName}_key.bin");
                string ivPath = Path.Combine(dir, $"{baseName}_iv.bin");

                if (!File.Exists(keyPath) || !File.Exists(ivPath))
                {
                    MessageBox.Show("Matching key or IV file not found.\nMake sure they are in the same folder as the encrypted file.", 
                                    "Missing Files", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                byte[] key = File.ReadAllBytes(keyPath);
                byte[] iv = File.ReadAllBytes(ivPath);

                string cipherText = File.ReadAllText(filePath);
                string decrypted = Decrypt(cipherText, key, iv);
                OutputTextBox.Text = decrypted;

                // Save decrypted text
                string decryptedFile = Path.Combine(dir, $"{baseName}_decrypted.txt");
                File.WriteAllText(decryptedFile, decrypted);

                StatusText.Text = "Decrypted successfully.";
                LastSavedPathBox.Text = decryptedFile;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Decryption Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void InputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (InputTextBox.Text == "Enter your paragraph (plaintext or encrypted text)...")
                InputTextBox.Text = "";
        }

        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
                InputTextBox.Text = "Enter your paragraph (plaintext or encrypted text)...";
        }

        private string Encrypt(string plainText, out byte[] key, out byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                key = aes.Key;
                iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Close();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private string Decrypt(string cipherText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
