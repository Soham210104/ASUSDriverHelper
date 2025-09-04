using System;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.IO;
using System.Text.Json;

namespace ASUSDriverHelper
{
    public partial class Form1 : Form
    {
        private Label lblWifiStatus;
        private Label lblBluetoothStatus;
        private Button btnDownloadWifi;
        private Button btnDownloadBluetooth;
        private Button btnRefresh;
        private Button btnShowDetails;
        private string wifiDriverUrl = "https://www.asus.com/us/supportonly/fx506hm/helpdesk_download/";
        private string bluetoothDriverUrl = "https://www.asus.com/us/supportonly/fx506hm/helpdesk_download/";
        private string wifiInstalledVersion = "";
        private string bluetoothInstalledVersion = "";
        private string wifiLatestVersion = "";
        private string bluetoothLatestVersion = "";

        public Form1()
        {
            InitializeComponent();
            SetupUI(); 
            CheckDrivers(); 
        }

        private void LoadLatestVersions()
        {
            try
            {
                // Try multiple locations for the JSON file
                string[] possiblePaths = {
                    "latestDrivers.json",
                    Path.Combine(Application.StartupPath, "latestDrivers.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "latestDrivers.json")
                };

                string jsonText = "";
                bool fileFound = false;

                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        jsonText = File.ReadAllText(path);
                        fileFound = true;
                        break;
                    }
                }

                if (!fileFound)
                {
                    throw new FileNotFoundException("latestDrivers.json not found in any expected location");
                }

                var latestDrivers = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);
                wifiLatestVersion = latestDrivers["wifi"];
                bluetoothLatestVersion = latestDrivers["bluetooth"];
            }
            catch (Exception ex)
            {
                // Show error for debugging, then use defaults
                MessageBox.Show($"Could not load latest driver versions: {ex.Message}\nUsing default values.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                wifiLatestVersion = "0.0.0.0";
                bluetoothLatestVersion = "0.0.0.0";
            }
        }
        
        private void CheckDrivers()
        {
            // Load latest driver versions from JSON file every time we check
            LoadLatestVersions();
            
            bool wifiFound = false; 
            bool bluetoothFound = false;
            wifiInstalledVersion = "";
            bluetoothInstalledVersion = "";

            try
            {
                // Try PowerShell-based approach for published executables
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Get-WmiObject Win32_PnPSignedDriver | Where-Object {$_.DeviceName -like '*wifi*' -or $_.DeviceName -like '*wireless*' -or $_.DeviceName -like '*bluetooth*' -or $_.DeviceName -like '*wlan*'} | Select-Object DeviceName, DriverVersion | Format-Table -AutoSize\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new Exception($"PowerShell error: {error}");
                    }

                    // Parse the output to find drivers and versions
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.ToLower().Contains("wifi") || line.ToLower().Contains("wireless") || line.ToLower().Contains("wlan"))
                        {
                            wifiFound = true;
                            // Try to extract version from the line
                            var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1)
                            {
                                // Look for version-like string (contains dots and numbers)
                                foreach (var part in parts)
                                {
                                    if (part.Contains(".") && char.IsDigit(part[0]))
                                    {
                                        wifiInstalledVersion = part.Trim();
                                        break;
                                    }
                                }
                            }
                        }
                        
                        if (line.ToLower().Contains("bluetooth"))
                        {
                            bluetoothFound = true;
                            // Try to extract version from the line
                            var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1)
                            {
                                // Look for version-like string (contains dots and numbers)
                                foreach (var part in parts)
                                {
                                    if (part.Contains(".") && char.IsDigit(part[0]))
                                    {
                                        bluetoothInstalledVersion = part.Trim();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                // If PowerShell/WMI fails, try alternative method using network adapters
                try
                {
                    CheckDriversAlternative(out wifiFound, out bluetoothFound);
                }
                catch (Exception)
                {
                    // Both methods failed - show error in the labels
                    lblWifiStatus.Text = "Wi-Fi: Cannot check (run as admin)";
                    lblBluetoothStatus.Text = "Bluetooth: Cannot check (run as admin)";
                    return;
                }
            }

            // Show status + version info with enhanced messaging
            if (!wifiFound) 
            {
                lblWifiStatus.Text = "Wi-Fi: Missing ❌";
            }
            else if (string.IsNullOrEmpty(wifiInstalledVersion)) 
            {
                lblWifiStatus.Text = "Wi-Fi: Installed ✅ (version not available)";
            }
            else 
            {
                int comparison = CompareVersions(wifiInstalledVersion, wifiLatestVersion);
                if (comparison < 0) 
                {
                    lblWifiStatus.Text = $"Wi-Fi: Outdated ⚠️ v{wifiInstalledVersion}";
                }
                else if (comparison > 0)
                {
                    lblWifiStatus.Text = $"Wi-Fi: Newer ✅ (v{wifiInstalledVersion})";
                }
                else 
                {
                    lblWifiStatus.Text = $"Wi-Fi: Up to Date ✅ (v{wifiInstalledVersion})";
                }
            }

            if (!bluetoothFound) 
            {
                lblBluetoothStatus.Text = "Bluetooth: Missing ❌";
            }
            else if (string.IsNullOrEmpty(bluetoothInstalledVersion)) 
            {
                lblBluetoothStatus.Text = "Bluetooth: Installed ✅ (version not available)";
            }
            else 
            {
                int comparison = CompareVersions(bluetoothInstalledVersion, bluetoothLatestVersion);
                if (comparison < 0) 
                {
                    lblBluetoothStatus.Text = $"Bluetooth: Outdated ⚠️ v{bluetoothInstalledVersion}";
                }
                else if (comparison > 0)
                {
                    lblBluetoothStatus.Text = $"Bluetooth: Newer ✅ (v{bluetoothInstalledVersion})";
                }
                else 
                {
                    lblBluetoothStatus.Text = $"Bluetooth: Up to Date ✅ (v{bluetoothInstalledVersion})";
                }
            }

            // Enable download buttons if missing or outdated, and update button text
            if (!wifiFound || CompareVersions(wifiInstalledVersion, wifiLatestVersion) < 0)
            {
                btnDownloadWifi.Enabled = true;
                btnDownloadWifi.Text = "Download Wi-Fi Driver";
            }
            else
            {
                btnDownloadWifi.Enabled = false;
                btnDownloadWifi.Text = "No Need to Download";
            }

            if (!bluetoothFound || CompareVersions(bluetoothInstalledVersion, bluetoothLatestVersion) < 0)
            {
                btnDownloadBluetooth.Enabled = true;
                btnDownloadBluetooth.Text = "Download Bluetooth Driver";
            }
            else
            {
                btnDownloadBluetooth.Enabled = false;
                btnDownloadBluetooth.Text = "No Need to Download";
            }
        }

        // Returns -1 if installed < latest, 0 if equal, 1 if installed > latest
        private int CompareVersions(string installed, string latest)
        {
            if (string.IsNullOrEmpty(installed)) return -1;

            var installedParts = installed.Split('.');
            var latestParts = latest.Split('.');

            int len = Math.Max(installedParts.Length, latestParts.Length);
            for (int i = 0; i < len; i++)
            {
                int installedNum = i < installedParts.Length ? int.Parse(installedParts[i]) : 0;
                int latestNum = i < latestParts.Length ? int.Parse(latestParts[i]) : 0;

                if (installedNum < latestNum) return -1;
                if (installedNum > latestNum) return 1;
            }
            return 0;
        }

        private void CheckDriversAlternative(out bool wifiFound, out bool bluetoothFound)
        {
            wifiFound = false;
            bluetoothFound = false;

            // Check using NetworkInterface class (doesn't require admin)
            var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            
            foreach (var adapter in networkInterfaces)
            {
                string description = adapter.Description.ToLower();
                string name = adapter.Name.ToLower();
                
                if (description.Contains("wifi") || description.Contains("wireless") || 
                    description.Contains("wlan") || description.Contains("802.11") ||
                    name.Contains("wifi") || name.Contains("wireless"))
                {
                    wifiFound = true;
                }
                
                if (description.Contains("bluetooth") || name.Contains("bluetooth"))
                {
                    bluetoothFound = true;
                }
            }
        }

        private void SetupUI()
        {
            // Form properties
            this.Text = "ASUS FX506HM Driver Helper";
            this.Size = new System.Drawing.Size(450, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;
            this.MaximizeBox = false; // Disable maximize button
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Prevent resizing
            this.BackColor = System.Drawing.Color.FromArgb(240, 240, 240); // Light gray background

            // Wi-Fi Label
            lblWifiStatus = new Label();
            lblWifiStatus.Text = "Wi-Fi Status: Checking...";
            lblWifiStatus.Location = new System.Drawing.Point(30, 30);
            lblWifiStatus.Size = new System.Drawing.Size(250, 25);
            lblWifiStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            this.Controls.Add(lblWifiStatus);

            // Wi-Fi Button
            btnDownloadWifi = new Button();
            btnDownloadWifi.Text = "Download Wi-Fi Driver";
            btnDownloadWifi.Location = new System.Drawing.Point(30, 65);
            btnDownloadWifi.Size = new System.Drawing.Size(180, 35);
            btnDownloadWifi.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            btnDownloadWifi.BackColor = System.Drawing.Color.FromArgb(0, 120, 215); // Windows blue
            btnDownloadWifi.ForeColor = System.Drawing.Color.White;
            btnDownloadWifi.FlatStyle = FlatStyle.Flat;
            btnDownloadWifi.FlatAppearance.BorderSize = 0;
            btnDownloadWifi.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(40, 140, 235); // Lighter blue on hover
            btnDownloadWifi.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 100, 195); // Darker blue when clicked
            this.Controls.Add(btnDownloadWifi);
            btnDownloadWifi.Click += BtnDownloadWifi_Click;
            btnDownloadWifi.Enabled = false;

            // Bluetooth Label
            lblBluetoothStatus = new Label();
            lblBluetoothStatus.Text = "Bluetooth Status: Checking...";
            lblBluetoothStatus.Location = new System.Drawing.Point(30, 130);
            lblBluetoothStatus.Size = new System.Drawing.Size(300, 25);
            lblBluetoothStatus.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular);
            this.Controls.Add(lblBluetoothStatus);

            // Bluetooth Button
            btnDownloadBluetooth = new Button();
            btnDownloadBluetooth.Text = "Download Bluetooth Driver";
            btnDownloadBluetooth.Location = new System.Drawing.Point(30, 165);
            btnDownloadBluetooth.Size = new System.Drawing.Size(180, 35);
            btnDownloadBluetooth.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            btnDownloadBluetooth.BackColor = System.Drawing.Color.FromArgb(0, 120, 215); // Windows blue
            btnDownloadBluetooth.ForeColor = System.Drawing.Color.White;
            btnDownloadBluetooth.FlatStyle = FlatStyle.Flat;
            btnDownloadBluetooth.FlatAppearance.BorderSize = 0;
            btnDownloadBluetooth.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(40, 140, 235); // Lighter blue on hover
            btnDownloadBluetooth.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(0, 100, 195); // Darker blue when clicked
            btnDownloadBluetooth.Enabled = false;
            btnDownloadBluetooth.Click += BtnDownloadBluetooth_Click;
            this.Controls.Add(btnDownloadBluetooth);

            // Refresh Button
            btnRefresh = new Button();
            btnRefresh.Text = "Refresh Status";
            btnRefresh.Location = new System.Drawing.Point(300, 30);
            btnRefresh.Size = new System.Drawing.Size(120, 35);
            btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            btnRefresh.BackColor = System.Drawing.Color.FromArgb(76, 175, 80); // Green
            btnRefresh.ForeColor = System.Drawing.Color.White;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(96, 195, 100); // Lighter green on hover
            btnRefresh.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(56, 155, 60); // Darker green when clicked
            btnRefresh.Click += BtnRefresh_Click;
            this.Controls.Add(btnRefresh);

            // Details Button
            btnShowDetails = new Button();
            btnShowDetails.Text = "Details";
            btnShowDetails.Location = new System.Drawing.Point(300, 75);
            btnShowDetails.Size = new System.Drawing.Size(120, 35);
            btnShowDetails.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            btnShowDetails.BackColor = System.Drawing.Color.FromArgb(128, 128, 128); // Gray
            btnShowDetails.ForeColor = System.Drawing.Color.White;
            btnShowDetails.FlatStyle = FlatStyle.Flat;
            btnShowDetails.FlatAppearance.BorderSize = 0;
            btnShowDetails.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(148, 148, 148); // Lighter gray on hover
            btnShowDetails.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(108, 108, 108); // Darker gray when clicked
            btnShowDetails.Click += BtnShowDetails_Click;
            this.Controls.Add(btnShowDetails);

            // Add a separator line (moved down to avoid overlap)
            var separator = new Label();
            separator.BorderStyle = BorderStyle.Fixed3D;
            separator.Location = new System.Drawing.Point(30, 215);
            separator.Size = new System.Drawing.Size(390, 2);
            this.Controls.Add(separator);

            // Add info label
            var infoLabel = new Label();
            infoLabel.Text = "ASUS FX506HM Driver Helper - Automatically detects missing drivers";
            infoLabel.Location = new System.Drawing.Point(30, 230);
            infoLabel.Size = new System.Drawing.Size(390, 20);
            infoLabel.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            infoLabel.ForeColor = System.Drawing.Color.Gray;
            infoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.Controls.Add(infoLabel);
        }

        private void BtnDownloadWifi_Click(object sender,EventArgs e){
            try{
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = wifiDriverUrl,
                    UseShellExecute = true
                });
            }
            catch(Exception ex){
                MessageBox.Show($"Failed to open Wi-Fi driver page:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDownloadBluetooth_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = bluetoothDriverUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open Bluetooth driver page:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
             CheckDrivers();
        }

        private void BtnShowDetails_Click(object sender, EventArgs e)
        {
            string message = "Driver Version Information\n\n";
            
            // Wi-Fi info
            message += "Wi-Fi Driver:\n";
            if (string.IsNullOrEmpty(wifiInstalledVersion))
            {
                message += "Status: Detected but version unavailable\n";
                message += "Note: Try running as administrator for version details\n";
            }
            else
            {
                message += $"Version: {wifiInstalledVersion}\n";
                message += "Status: Installed ✅\n";
            }
            
            // Bluetooth info
            message += "\nBluetooth Driver:\n";
            if (string.IsNullOrEmpty(bluetoothInstalledVersion))
            {
                message += "Status: Detected but version unavailable\n";
                message += "Note: Try running as administrator for version details\n";
            }
            else
            {
                message += $"Version: {bluetoothInstalledVersion}\n";
                message += "Status: Installed ✅\n";
            }
            
            message += "\nNote: Click Refresh to update information";
            
            MessageBox.Show(message, "Driver Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}