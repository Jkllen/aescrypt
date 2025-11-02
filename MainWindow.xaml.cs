using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace aescrypt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// AES Encryption/Decryption Program
    /// This program allows the user to input plaintext or ciphertext,
    /// encrypt/decrypt using AES algorithm, and save outputs to files.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Default output folder (the folder where the executable is running)
        private string outputFolder = AppDomain.CurrentDomain.BaseDirectory;

        // Default AES key and IV (not used directly — dynamic keys are generated per encryption)
        private readonly byte[] key = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes = 256-bit
        private readonly byte[] iv = new byte[16]; // Default IV size for AES (128-bit)

        public MainWindow()
        {
            InitializeComponent();
            // Display initial output folder path in UI
            OutputFolderText.Text = $"Current Output Folder: {outputFolder}";
        }

        /// <summary>
        /// Allows the user to set a new output folder for saving files.
        /// </summary>
        private void SetFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Uses an OpenFileDialog trick to let the user pick a folder
            var dialog = new OpenFileDialog
            {
                Title = "Select a folder",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                // Extract directory name from chosen "file"
                string selectedPath = Path.GetDirectoryName(dialog.FileName)!;

                // Update internal folder path
                outputFolder = selectedPath;
                OutputFolderText.Text = $"Current Output Folder: {outputFolder}";
                StatusText.Text = "Output folder updated.";
            }
        }

        /// <summary>
        /// Encrypts user input text using AES encryption algorithm.
        /// Saves encrypted text and the corresponding key/IV in separate files.
        /// </summary>
        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text.Trim();

            // Validation: ensure input is not empty
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter text to encrypt.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Generate random AES key and IV for this encryption session
                byte[] key;
                byte[] iv;

                // Perform encryption
                string encrypted = Encrypt(input, out key, out iv);
                OutputTextBox.Text = encrypted;

                // Prompt user to choose where to save the encrypted file
                var saveDialog = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = "encrypted_output.txt",
                    InitialDirectory = outputFolder
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Save encrypted text as .txt file
                    File.WriteAllText(saveDialog.FileName, encrypted);

                    // Derive paths for saving the key and IV
                    string baseName = Path.GetFileNameWithoutExtension(saveDialog.FileName);
                    string dir = Path.GetDirectoryName(saveDialog.FileName)!;

                    string keyPath = Path.Combine(dir, $"{baseName}_key.bin");
                    string ivPath = Path.Combine(dir, $"{baseName}_iv.bin");

                    // Save key and IV in binary format for later decryption
                    File.WriteAllBytes(keyPath, key);
                    File.WriteAllBytes(ivPath, iv);

                    // Update UI status messages
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
                // Handle unexpected errors gracefully
                MessageBox.Show($"Error: {ex.Message}", "Encryption Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Decrypts a previously encrypted file using its corresponding key and IV.
        /// </summary>
        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            string input = InputTextBox.Text.Trim();

            // Validation: ensure input is not empty
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

                // Get selected file path and derive related file names
                string filePath = openDialog.FileName;
                string dir = Path.GetDirectoryName(filePath)!;
                string baseName = Path.GetFileNameWithoutExtension(filePath);

                // Expect matching key and IV files in the same folder
                string keyPath = Path.Combine(dir, $"{baseName}_key.bin");
                string ivPath = Path.Combine(dir, $"{baseName}_iv.bin");

                // Validation: ensure key/IV files exist
                if (!File.Exists(keyPath) || !File.Exists(ivPath))
                {
                    MessageBox.Show("Matching key or IV file not found.\nMake sure they are in the same folder as the encrypted file.", 
                                    "Missing Files", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Load key, IV, and encrypted text from files
                byte[] key = File.ReadAllBytes(keyPath);
                byte[] iv = File.ReadAllBytes(ivPath);
                string cipherText = File.ReadAllText(filePath);

                // Perform decryption
                string decrypted = Decrypt(cipherText, key, iv);
                OutputTextBox.Text = decrypted;

                // Automatically save decrypted result
                string decryptedFile = Path.Combine(dir, $"{baseName}_decrypted.txt");
                File.WriteAllText(decryptedFile, decrypted);

                // Update UI status
                StatusText.Text = "Decrypted successfully.";
                LastSavedPathBox.Text = decryptedFile;
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                MessageBox.Show($"Error: {ex.Message}", "Decryption Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clears the placeholder text when the input box gains focus.
        /// </summary>
        private void InputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (InputTextBox.Text == "Enter your paragraph (plaintext or encrypted text)...")
                InputTextBox.Text = "";
        }

        /// <summary>
        /// Restores placeholder text when input box loses focus and is empty.
        /// </summary>
        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
                InputTextBox.Text = "Enter your paragraph (plaintext or encrypted text)...";
        }

        /// <summary>
        /// Encrypts a plaintext string using AES algorithm.
        /// Generates a random key and IV for every encryption session.
        /// </summary>
        /// <param name="plainText">Text to encrypt.</param>
        /// <param name="key">Output AES key.</param>
        /// <param name="iv">Output AES IV.</param>
        /// <returns>Base64-encoded encrypted string.</returns>
        private string Encrypt(string plainText, out byte[] key, out byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                // Generate random AES key and IV (per session)
                aes.GenerateKey();
                aes.GenerateIV();

                key = aes.Key;
                iv = aes.IV;

                // Perform encryption using CryptoStream
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Close();

                    // Return Base64 string for easy saving/transmission
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts AES-encrypted Base64 text using the provided key and IV.
        /// </summary>
        /// <param name="cipherText">Base64-encoded encrypted text.</param>
        /// <param name="key">The AES key used for encryption.</param>
        /// <param name="iv">The AES initialization vector.</param>
        /// <returns>Decrypted plaintext string.</returns>
        private string Decrypt(string cipherText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                // Set provided key and IV
                aes.Key = key;
                aes.IV = iv;

                // Perform decryption
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
