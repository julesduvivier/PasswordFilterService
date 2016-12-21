//-----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="SCRT">
// Licensed under the MIT License
// </copyright>
//-----------------------------------------------------------------------

namespace PasswordFilterService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Forms;
    using Microsoft.Win32;

    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consitency")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Review. Suppression is OK")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:PartialElementsMustBeDocumented", Justification = "Review. Suppression is OK")]

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            List<string> resourcesNameList = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceNames().ToList();
            resourcesNameList.RemoveAll(EndWithResources);
            string removeString = "PasswordFilterService.Resources.";
            for (var x = 0; x < resourcesNameList.Count; x++)
            {
               resourcesNameList[x] = Path.GetFileNameWithoutExtension(resourcesNameList[x].Remove(resourcesNameList[x].IndexOf(removeString), removeString.Length));
            }

            List<string> contentNotificationPackages;
            const string KeyLsa = @"SYSTEM\CurrentControlSet\Control\Lsa";
            RegistryKey rkLsa = Registry.LocalMachine.OpenSubKey(KeyLsa, true);
            RegistryValueKind rvk = rkLsa.GetValueKind("Notification Packages");
            object rNotificationPackages = rkLsa.GetValue("Notification Packages");
            if (rvk == RegistryValueKind.MultiString)
            {               
                contentNotificationPackages = new List<string>((string[])rNotificationPackages);
            }
           else
            {         
                contentNotificationPackages = new List<string>((string[])rNotificationPackages.ToString().Split(' '));
            }

            bool notificationPackagesContainDll = contentNotificationPackages.Intersect(resourcesNameList).Any();
            bool is64BitProcess = IntPtr.Size == 8;
            bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();
            string resourceToExtract;

            if (is64BitOperatingSystem)
            {
                resourceToExtract = resourcesNameList.Find(item => item.Contains("x86_64"));
                if (resourceToExtract == null)
                {
                    System.Windows.MessageBox.Show("Fatal Error please contact the administrator");
                    Environment.Exit(0);
                }

                if (!File.Exists(@"C:\Windows\Sysnative\" + resourceToExtract + ".dll"))
                {
                    ExtractEmbeddedResource(@"C:\Windows\Sysnative", "PasswordFilterService.Resources", resourceToExtract + ".dll");
                }
            }
            else
            {
                resourceToExtract = resourcesNameList.Find(item => item.Contains("x86_32"));
                if (resourceToExtract == null)
                {
                    System.Windows.MessageBox.Show("Fatal Error please contact the administrator");
                    Environment.Exit(0);
                }

                if (!File.Exists(@"C:\Windows\System32\" + resourceToExtract + ".dll"))
                {
                   ExtractEmbeddedResource(@"C:\Windows\System32", "PasswordFilterService.Resources", resourceToExtract + ".dll");
                }
            }

            if (!notificationPackagesContainDll)
            {
                    if (rvk == RegistryValueKind.MultiString)
                    {
                    contentNotificationPackages.RemoveAll(str => str.Contains("PasswordFilter"));
                    contentNotificationPackages.Add(resourceToExtract);                       
                        rkLsa.SetValue("Notification Packages", contentNotificationPackages.ToArray());
                    }
                    else
                    {
                    contentNotificationPackages.RemoveAll(str => str.Contains("PasswordFilter"));
                    contentNotificationPackages.Add(resourceToExtract);
                        
                        rkLsa.SetValue("Notification Packages", string.Join(" ", contentNotificationPackages.ToArray()));
                    }

                contentNotificationPackages.ForEach(Console.WriteLine);
            } 

            const string Key = "SOFTWARE\\WOW6432Node\\PasswordFilter";            
            RegistryKey rk = Registry.LocalMachine.OpenSubKey(Key);
            if (rk != null)
            {
                const int FLAGLOWER = 0x01;
                const int FLAGUPPER = 0x02;
                const int FLAGDIGIT = 0x04;
                const int FLAGSPECIAL = 0x08;
                int complexityFlag;
                object rMinimumPasswordLength = rk.GetValue("MinimumPasswordLength");
                object rNumberMaxOfConsecutivesLetters = rk.GetValue("NumberMaxOfConsecutivesLetters");
                object rLog = rk.GetValue("Log");
                object rTokensWordList = rk.GetValue("TokensWordlist");
                object rWordList = rk.GetValue("WordList");
                object rComplexity = rk.GetValue("Complexity");
                object rUseWordList = rk.GetValue("UseWordlist");
                object rUseTokensWordList = rk.GetValue("UseTokensWordlist");
                if (rMinimumPasswordLength != null)
                {
                    textBox_length.Text = rMinimumPasswordLength.ToString();
                }

                if (rNumberMaxOfConsecutivesLetters != null)
                {
                    textBox_consecutive.Text = rNumberMaxOfConsecutivesLetters.ToString();
                }

                if (rLog != null)
                {
                    textBox_log.Text = rLog.ToString();
                    textBox_log.Focus();
                    textBox_log.Select(textBox_log.Text.Length, 0);
                }

                if (rTokensWordList != null)
                {
                    textBox_tokens.Text = rTokensWordList.ToString();
                    textBox_tokens.Focus();
                    textBox_tokens.Select(textBox_log.Text.Length, 0);
                }

                if (rWordList != null)
                {
                    textBox_wordlist.Text = rWordList.ToString();
                    textBox_wordlist.Focus();
                    textBox_wordlist.Select(textBox_log.Text.Length, 0);
                }

                if (rComplexity != null)
                {
                    complexityFlag = (int)rComplexity;
                    checkBox_lowercase.IsChecked = Convert.ToBoolean(complexityFlag & FLAGLOWER);
                    checkBox_uppercase.IsChecked = Convert.ToBoolean(complexityFlag & FLAGUPPER);
                    checkBox_digit.IsChecked = Convert.ToBoolean(complexityFlag & FLAGDIGIT);
                    checkBox_special.IsChecked = Convert.ToBoolean(complexityFlag & FLAGSPECIAL);
                }

                if (rUseWordList != null)
                {
                    checkBox_wordlist.IsChecked = Convert.ToBoolean(rUseWordList);
                }

                if (rUseTokensWordList != null)
                {
                    checkBox_wordlistTokens.IsChecked = Convert.ToBoolean(rUseTokensWordList);
                }

                if (checkBox_wordlist.IsChecked == false)
                {
                    textBox_wordlist.IsEnabled = false;
                    button_wordlist.IsEnabled = false;
                }

                if (checkBox_wordlistTokens.IsChecked == false)
                {
                    textBox_tokens.IsEnabled = false;
                    button_tokens.IsEnabled = false;
                }
            }            
        } 

        private static bool EndWithResources(string s)
        {
            return s.ToLower().EndsWith("resources");
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
        [In] IntPtr hProcess,
        [Out] out bool wow64Process);

        private static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }

                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }

        private static void ExtractEmbeddedResource(string outputDir, string resourceLocation, string file)
        {   
                using (System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceLocation + @"." + file))
                {
                    using (System.IO.FileStream fileStream = new System.IO.FileStream(System.IO.Path.Combine(outputDir, file), System.IO.FileMode.Create))
                    {
                        for (int i = 0; i < stream.Length; i++)
                        {
                            fileStream.WriteByte((byte)stream.ReadByte());
                        }

                        fileStream.Close();
                    }
                }            
        }

        private void EnableTokensWordList(object sender, RoutedEventArgs e)
        {
          if (button_tokens.IsEnabled)
            {
                textBox_tokens.IsEnabled = false;
                button_tokens.IsEnabled = false;
            }
          else
            {
                textBox_tokens.IsEnabled = true;
                button_tokens.IsEnabled = true;
            }
        }

        private void EnableWordList(object sender, RoutedEventArgs e)
        {
             if (button_wordlist.IsEnabled)
            {
                textBox_wordlist.IsEnabled = false;
                button_wordlist.IsEnabled = false;
            }
          else
            {
                textBox_wordlist.IsEnabled = true;
                button_wordlist.IsEnabled = true;
            }
        }

        private void ButtonWordlist_Click(object sender, RoutedEventArgs e)
        {
            string wordlistFile;
            int lineCount;
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox_wordlist.Text = fileDialog.FileName;
                textBox_wordlist.Focus();
                textBox_wordlist.Select(textBox_wordlist.Text.Length, 0);
                if (File.Exists(textBox_wordlist.Text))
                {
                    wordlistFile = textBox_wordlist.Text;
                    lineCount = File.ReadLines(wordlistFile).Count();
                    textBlock_lineWordlist.Text = "(" + lineCount.ToString() + " words)";
                }
                else
                {
                    System.Windows.MessageBox.Show("The specified wordlist doesn't exist", "Error");
                    return;
                }
            }
        }

        private void ButtonToken_Click(object sender, RoutedEventArgs e)
        {
            string tokensWordlistFile;
            int lineCount;
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox_tokens.Text = fileDialog.FileName;
                textBox_tokens.Focus();
                textBox_tokens.Select(textBox_tokens.Text.Length, 0);
                if (File.Exists(textBox_tokens.Text))
                {
                    tokensWordlistFile = textBox_tokens.Text;
                    lineCount = File.ReadLines(tokensWordlistFile).Count();
                    textBlock_lineTokenWordlist.Text = "(" + lineCount.ToString() + " words)";
                }
                else
                {
                    System.Windows.MessageBox.Show("The specified wordlist doesn't exist", "Error");
                    return;
                }
            }
        }

        private void ButtonLog_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fileDialog = new FolderBrowserDialog();
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox_log.Text = fileDialog.SelectedPath;
                textBox_log.Focus();
                textBox_log.Select(textBox_log.Text.Length, 0);
            }
        }

        private void ButtonSubmit_Click(object sender, RoutedEventArgs e)
        {            
            int passwordLength;
            int consecutiveLetters;
            int flagComplexity = 0x00;
            const int FlagLower = 0x01;
            const int FlagUpper = 0x02;
            const int FlagDigit = 0x04;
            const int FlagSpecial = 0x08;
            string logFile;
            string wordlistFile = string.Empty;
            string tokensWordlistFile = string.Empty;
            bool useWordlist = false;
            bool useTokensWordlist = false;

            if (!int.TryParse(textBox_length.Text, out passwordLength))
            {
                System.Windows.MessageBox.Show("Password length must be numeric value", "Error");
                return;
            }

            if (!int.TryParse(textBox_consecutive.Text, out consecutiveLetters))
            {
                System.Windows.MessageBox.Show("Maximum of consecutive letters must be numeric value", "Error");
                return;
            }

            if (checkBox_lowercase.IsChecked == true)
            {
                flagComplexity += FlagLower;
            }

            if (checkBox_uppercase.IsChecked == true)
            {
                flagComplexity += FlagUpper;
            }

            if (checkBox_digit.IsChecked == true)
            {
                flagComplexity += FlagDigit;
            }

            if (checkBox_special.IsChecked == true)
            {
                flagComplexity += FlagSpecial;
            }

            if (textBox_log.Text == null)
            {
                System.Windows.MessageBox.Show("You must specified a directory for the log file", "Error");
                return;
            }

            if (!Directory.Exists(textBox_log.Text))
            {
                System.Windows.MessageBox.Show("The specified directory for the log file is invalid", "Error");
                return;
            }
            else
            {
                logFile = textBox_log.Text;
            }

            if (checkBox_wordlist.IsChecked == true)
            {
                useWordlist = true;
                if (File.Exists(textBox_wordlist.Text))
                {
                    wordlistFile = textBox_wordlist.Text;
                }
                else
                {
                    System.Windows.MessageBox.Show("The specified wordlist doesn't exist", "Error");
                    return;
                }
            }

            if (checkBox_wordlistTokens.IsChecked == true)
            {
                useTokensWordlist = true;
                if (File.Exists(textBox_tokens.Text))
                {
                    tokensWordlistFile = textBox_tokens.Text;
                }
                else
                {
                    System.Windows.MessageBox.Show("The specified tokens wordlist doesn't exist", "Error");
                    return;
                }
            }

            const string Key = "SOFTWARE\\WOW6432Node\\PasswordFilter";
            RegistryKey rk = Registry.LocalMachine.CreateSubKey(Key);
            rk.SetValue("Complexity", flagComplexity, RegistryValueKind.DWord);
            rk.SetValue("MinimumPasswordLength", passwordLength, RegistryValueKind.DWord);
            rk.SetValue("NumberMaxOfConsecutivesLetters", consecutiveLetters, RegistryValueKind.DWord);
            rk.SetValue("UseTokensWordlist", useTokensWordlist, RegistryValueKind.DWord);
            rk.SetValue("UseWordlist", useWordlist, RegistryValueKind.DWord);
            rk.SetValue("Log", logFile, RegistryValueKind.String);
            rk.SetValue("TokensWordlist", tokensWordlistFile, RegistryValueKind.String);
            rk.SetValue("WordList", wordlistFile, RegistryValueKind.String);
            Environment.Exit(0);
        }
    }
}
