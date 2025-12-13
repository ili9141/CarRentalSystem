using CarRental.Data;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CarRental.Admin.UserControls
{
    public partial class MaintenanceUserControl : UserControl
    {
        private ApplicationDbContext _context;
        private DataGridView dataGridView;
        private Button btnMarkCompleted;
        private Button btnCreateAlert;
        private Button btnViewHistory;
        private Button btnRefresh;
        private ComboBox cmbFilterStatus;
        private Label lblStats;

        public MaintenanceUserControl(ApplicationDbContext context)
        {
            // Create fresh context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new ApplicationDbContext(options);

            SetupUI();
            LoadMaintenanceAlerts();
            UpdateStats();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;

            // Top panel with controls
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 100 };
            var panelGrid = new Panel { Dock = DockStyle.Fill };

            // Stats label
            lblStats = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(400, 25),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };

            // Filter section
            var lblFilter = new Label { Text = "Filter by:", Location = new Point(10, 45), AutoSize = true };
            cmbFilterStatus = new ComboBox { Location = new Point(90, 42), Width = 150 };
            cmbFilterStatus.Items.AddRange(new string[] {
                "⚠️ All Alerts",
                "⚠️ Active Alerts",
                "✅ Completed",
                "🚗 Vehicles in Maintenance"
            });
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndexChanged += FilterChanged;

            // Action buttons
            btnMarkCompleted = new Button
            {
                Text = "✅ Mark Completed",
                Location = new Point(270, 40),
                Width = 140,
                BackColor = Color.LightGreen,
                Font = new Font("Arial", 9, FontStyle.Bold),
                AutoSize = true
            };
            btnMarkCompleted.Click += BtnMarkCompleted_Click;

            btnCreateAlert = new Button
            {
                Text = "➕ Create Alert",
                Location = new Point(440, 40),
                Width = 120,
                BackColor = Color.LightBlue,
                AutoSize = true
            };
            btnCreateAlert.Click += BtnCreateAlert_Click;

            btnViewHistory = new Button
            {
                Text = "📋 View History",
                Location = new Point(590, 40),
                Width = 120,
                AutoSize = true
            };
            btnViewHistory.Click += BtnViewHistory_Click;

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Location = new Point(750, 40),
                Width = 100,
                AutoSize = true
            };
            btnRefresh.Click += BtnRefresh_Click;

            // Add to top panel
            panelTop.Controls.AddRange(new Control[]
            {
                lblStats,
                lblFilter, cmbFilterStatus,
                btnMarkCompleted, btnCreateAlert, btnViewHistory, btnRefresh
            });

            // DataGridView
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };

            panelGrid.Controls.Add(dataGridView);

            // Add panels to user control
            this.Controls.Add(panelGrid);
            this.Controls.Add(panelTop);
        }

        private void LoadMaintenanceAlerts(string filter = "⚠️ All Alerts")
        {
            try
            {
                DateTime today = DateTime.Today;
                DateTime sixMonthsAgo = today.AddMonths(-6);

                // Get all vehicles
                var vehicles = _context.Vehicles
                    .Include(v => v.Rentals)
                    .OrderBy(v => v.Brand)
                    .ThenBy(v => v.Model)
                    .ToList();

                // Determine maintenance status for each vehicle
                var maintenanceData = vehicles.Select(v =>
                {
                    bool needsMaintenance = false;
                    string alertReason = "";
                    string status = "✅ OK";

                    // Rule 1: Last maintenance was more than 6 months ago
                    if (v.LastMaintenanceDate < sixMonthsAgo)
                    {
                        needsMaintenance = true;
                        alertReason = $"Last maintenance: {v.LastMaintenanceDate:yyyy-MM-dd} (>6 months)";
                        status = "⚠️ Time-Based";
                    }

                    // Rule 2: High mileage (more than 10,000 km since last maintenance)
                    // Note: We need to track kilometers at last maintenance
                    // For now, we'll use a simpler rule

                    // Rule 3: Manual flag (if we had one)
                    // We'll add a NeedsMaintenance boolean later

                    return new
                    {
                        v.Id,
                        Vehicle = $"{v.Brand} {v.Model}",
                        LicensePlate = v.LicensePlate,
                        LastMaintenance = v.LastMaintenanceDate.ToString("yyyy-MM-dd"),
                        CurrentKilometers = $"{v.Kilometers} km",
                        Status = status,
                        AlertReason = alertReason,
                        IsActiveAlert = needsMaintenance,
                        IsInMaintenance = !v.IsAvailable && v.Rentals.All(r => r.EndDate < today)
                    };
                }).ToList();

                // Apply filter
                var filteredData = filter switch
                {
                    "⚠️ Active Alerts" => maintenanceData.Where(v => v.IsActiveAlert).ToList(),
                    "✅ Completed" => maintenanceData.Where(v => !v.IsActiveAlert).ToList(),
                    "🚗 Vehicles in Maintenance" => maintenanceData.Where(v => v.IsInMaintenance).ToList(),
                    _ => maintenanceData // All Alerts
                };

                dataGridView.DataSource = filteredData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading maintenance alerts: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStats()
        {
            try
            {
                DateTime sixMonthsAgo = DateTime.Today.AddMonths(-6);

                int totalVehicles = _context.Vehicles.Count();
                int needMaintenance = _context.Vehicles.Count(v => v.LastMaintenanceDate < sixMonthsAgo);
                int inMaintenance = _context.Vehicles.Count(v => !v.IsAvailable &&
                    v.Rentals.All(r => r.EndDate < DateTime.Today));

                var oldestMaintenance = _context.Vehicles
                    .Where(v => v.LastMaintenanceDate < sixMonthsAgo)
                    .OrderBy(v => v.LastMaintenanceDate)
                    .FirstOrDefault();

                string oldestInfo = oldestMaintenance != null
                    ? $"{oldestMaintenance.Brand} {oldestMaintenance.Model} ({oldestMaintenance.LastMaintenanceDate:yyyy-MM-dd})"
                    : "None";

                lblStats.Text = $"🔧 Stats: {totalVehicles} Vehicles | {needMaintenance} Need Maintenance | {inMaintenance} In Maintenance | Oldest: {oldestInfo}";
            }
            catch
            {
                lblStats.Text = "🔧 Stats: Loading...";
            }
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            LoadMaintenanceAlerts(cmbFilterStatus.SelectedItem.ToString());
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadMaintenanceAlerts();
            UpdateStats();
        }

        private void BtnMarkCompleted_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a vehicle to mark maintenance as completed",
                    "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int vehicleId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            string vehicleName = dataGridView.CurrentRow.Cells["Vehicle"].Value.ToString();
            string currentKmStr = dataGridView.CurrentRow.Cells["CurrentKilometers"].Value.ToString();

            // Parse kilometers (remove " km")
            int currentKilometers = 0;
            if (int.TryParse(currentKmStr.Replace(" km", ""), out int km))
            {
                currentKilometers = km;
            }

            using (var form = new CompleteMaintenanceForm(vehicleName, currentKilometers))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var vehicle = _context.Vehicles.Find(vehicleId);
                        if (vehicle != null)
                        {
                            // Update maintenance date and kilometers
                            vehicle.LastMaintenanceDate = form.MaintenanceDate;
                            vehicle.Kilometers = form.UpdatedKilometers;

                            // Mark vehicle as available if it was in maintenance
                            if (!vehicle.IsAvailable)
                            {
                                // Check if vehicle has active rentals
                                bool hasActiveRentals = _context.Rentals
                                    .Any(r => r.VehicleId == vehicleId &&
                                           r.StartDate <= DateTime.Now &&
                                           r.EndDate >= DateTime.Now);

                                if (!hasActiveRentals)
                                {
                                    vehicle.IsAvailable = true;
                                }
                            }

                            _context.SaveChanges();

                            MessageBox.Show($"Maintenance completed for {vehicleName}!", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            LoadMaintenanceAlerts(cmbFilterStatus.SelectedItem.ToString());
                            UpdateStats();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error completing maintenance: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnCreateAlert_Click(object sender, EventArgs e)
        {
            using (var form = new CreateMaintenanceAlertForm(_context))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var vehicle = _context.Vehicles.Find(form.SelectedVehicleId);
                        if (vehicle != null)
                        {
                            // For now, we'll just update the last maintenance date to be old
                            // In a real system, you'd have a separate MaintenanceAlert entity
                            vehicle.LastMaintenanceDate = form.AlertDate;

                            // Mark vehicle as unavailable if requested
                            if (form.MarkUnavailable)
                            {
                                vehicle.IsAvailable = false;
                            }

                            _context.SaveChanges();

                            MessageBox.Show($"Maintenance alert created for {vehicle.Brand} {vehicle.Model}!", "Alert Created",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                            LoadMaintenanceAlerts();
                            UpdateStats();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating alert: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnViewHistory_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a vehicle to view maintenance history",
                    "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int vehicleId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            string vehicleName = dataGridView.CurrentRow.Cells["Vehicle"].Value.ToString();

            var vehicle = _context.Vehicles
                .Include(v => v.Rentals)
                .FirstOrDefault(v => v.Id == vehicleId);

            if (vehicle == null)
            {
                MessageBox.Show("Vehicle not found", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var form = new MaintenanceHistoryForm(vehicle))
            {
                form.ShowDialog();
            }
        }

        // Nested form for completing maintenance
        private class CompleteMaintenanceForm : Form
        {
            private DateTimePicker dtpMaintenanceDate;
            private NumericUpDown numKilometers;
            private TextBox txtNotes;
            private Button btnComplete;
            private Button btnCancel;

            public DateTime MaintenanceDate => dtpMaintenanceDate.Value;
            public int UpdatedKilometers => (int)numKilometers.Value;
            public string Notes => txtNotes.Text;

            public CompleteMaintenanceForm(string vehicleName, int currentKilometers)
            {
                InitializeForm(vehicleName, currentKilometers);
            }

            private void InitializeForm(string vehicleName, int currentKilometers)
            {
                this.Text = $"Complete Maintenance: {vehicleName}";
                this.Size = new Size(400, 350);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

                int y = 20;

                // Title
                var lblTitle = new Label
                {
                    Text = $"✅ COMPLETE MAINTENANCE",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 40;

                // Vehicle info
                var lblVehicle = new Label
                {
                    Text = $"Vehicle: {vehicleName}",
                    Location = new Point(20, y),
                    Size = new Size(350, 25),
                    Font = new Font("Arial", 10)
                };
                y += 30;

                // Maintenance date
                var lblDate = new Label { Text = "Maintenance Date:", Location = new Point(20, y), Width = 150 };
                dtpMaintenanceDate = new DateTimePicker
                {
                    Location = new Point(180, y - 3),
                    Width = 150,
                    Format = DateTimePickerFormat.Short,
                    Value = DateTime.Today
                };
                y += 35;

                // Updated kilometers
                var lblKm = new Label { Text = "Updated Kilometers:", Location = new Point(20, y), Width = 150 };
                numKilometers = new NumericUpDown
                {
                    Location = new Point(180, y - 3),
                    Width = 150,
                    Minimum = 0,
                    Maximum = 1000000,
                    Value = currentKilometers
                };
                y += 35;

                // Notes
                var lblNotes = new Label { Text = "Notes:", Location = new Point(20, y), Width = 150 };
                txtNotes = new TextBox
                {
                    Location = new Point(180, y - 3),
                    Width = 150,
                    Height = 60,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };
                y += 70;

                // Buttons
                btnComplete = new Button
                {
                    Text = "✅ Complete",
                    Location = new Point(100, y),
                    Width = 100,
                    BackColor = Color.LightGreen,
                    AutoSize = true
                };
                btnComplete.Click += BtnComplete_Click;

                btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(210, y),
                    Width = 100,
                    AutoSize = true
                };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                panel.Controls.AddRange(new Control[]
                {
            lblTitle, lblVehicle, lblDate, dtpMaintenanceDate,
            lblKm, numKilometers, lblNotes, txtNotes,
            btnComplete, btnCancel
                });

                this.Controls.Add(panel);
            }

            private void BtnComplete_Click(object sender, EventArgs e)
            {
                if (numKilometers.Value < 0)
                {
                    MessageBox.Show("Kilometers cannot be negative", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        // Nested form for creating maintenance alerts
        private class CreateMaintenanceAlertForm : Form
        {
            private ApplicationDbContext _context;
            private ComboBox cmbVehicles;
            private DateTimePicker dtpAlertDate;
            private NumericUpDown numThresholdKm;
            private TextBox txtReason;
            private CheckBox chkMarkUnavailable;
            private Button btnCreate;
            private Button btnCancel;

            public int SelectedVehicleId { get; private set; }
            public DateTime AlertDate => dtpAlertDate.Value;
            public bool MarkUnavailable => chkMarkUnavailable.Checked;

            public CreateMaintenanceAlertForm(ApplicationDbContext context)
            {
                _context = context;
                InitializeForm();
                LoadVehicles();
            }

            private void InitializeForm()
            {
                this.Text = "Create Maintenance Alert";
                this.Size = new Size(450, 400);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

                int y = 20;

                // Title
                var lblTitle = new Label
                {
                    Text = "⚠️ CREATE MAINTENANCE ALERT",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 40;

                // Vehicle selection
                var lblVehicle = new Label { Text = "Select Vehicle:", Location = new Point(20, y), Width = 150 };
                cmbVehicles = new ComboBox
                {
                    Location = new Point(180, y - 3),
                    Width = 200,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                y += 35;

                // Alert date
                var lblDate = new Label { Text = "Alert Date:", Location = new Point(20, y), Width = 150 };
                dtpAlertDate = new DateTimePicker
                {
                    Location = new Point(180, y - 3),
                    Width = 200,
                    Format = DateTimePickerFormat.Short,
                    Value = DateTime.Today
                };
                y += 35;

                // Threshold kilometers (optional)
                var lblThreshold = new Label { Text = "Threshold KM (optional):", Location = new Point(20, y), Width = 150 };
                numThresholdKm = new NumericUpDown
                {
                    Location = new Point(180, y - 3),
                    Width = 200,
                    Minimum = 0,
                    Maximum = 1000000,
                    Value = 0
                };
                y += 35;

                // Reason
                var lblReason = new Label { Text = "Reason/Description:", Location = new Point(20, y), Width = 150 };
                txtReason = new TextBox
                {
                    Location = new Point(180, y - 3),
                    Width = 200,
                    Height = 60,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };
                y += 70;

                // Mark unavailable
                chkMarkUnavailable = new CheckBox
                {
                    Text = "Mark vehicle as unavailable during maintenance",
                    Location = new Point(20, y),
                    Width = 350,
                    Checked = true
                };
                y += 35;

                // Buttons
                btnCreate = new Button
                {
                    Text = "Create Alert",
                    Location = new Point(120, y),
                    Width = 100,
                    BackColor = Color.LightBlue,
                    AutoSize = true
                };
                btnCreate.Click += BtnCreate_Click;

                btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(230, y),
                    Width = 100,
                    AutoSize = true
                };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                panel.Controls.AddRange(new Control[]
                {
            lblTitle, lblVehicle, cmbVehicles, lblDate, dtpAlertDate,
            lblThreshold, numThresholdKm, lblReason, txtReason,
            chkMarkUnavailable, btnCreate, btnCancel
                });

                this.Controls.Add(panel);
            }

            private void LoadVehicles()
            {
                var vehicles = _context.Vehicles
                    .OrderBy(v => v.Brand)
                    .ThenBy(v => v.Model)
                    .ToList();

                cmbVehicles.Items.Clear();
                cmbVehicles.Items.Add("-- Select Vehicle --");

                foreach (var vehicle in vehicles)
                {
                    cmbVehicles.Items.Add(new VehicleItem
                    {
                        Id = vehicle.Id,
                        DisplayText = $"{vehicle.Brand} {vehicle.Model} - Last maintained: {vehicle.LastMaintenanceDate:yyyy-MM-dd}"
                    });
                }

                cmbVehicles.SelectedIndex = 0;
            }

            private void BtnCreate_Click(object sender, EventArgs e)
            {
                if (cmbVehicles.SelectedIndex == 0)
                {
                    MessageBox.Show("Please select a vehicle", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var vehicleItem = (VehicleItem)cmbVehicles.SelectedItem;
                SelectedVehicleId = vehicleItem.Id;

                this.DialogResult = DialogResult.OK;
                this.Close();
            }

            // Helper class for combo box items
            private class VehicleItem
            {
                public int Id { get; set; }
                public string DisplayText { get; set; }
                public override string ToString() => DisplayText;
            }
        }
        // Nested form for viewing maintenance history
        private class MaintenanceHistoryForm : Form
        {
            public MaintenanceHistoryForm(Vehicle vehicle)
            {
                InitializeForm(vehicle);
            }

            private void InitializeForm(Vehicle vehicle)
            {
                this.Text = $"Maintenance History: {vehicle.Brand} {vehicle.Model}";
                this.Size = new Size(600, 500);
                this.StartPosition = FormStartPosition.CenterParent;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

                // Title
                var lblTitle = new Label
                {
                    Text = $"🔧 MAINTENANCE HISTORY: {vehicle.Brand.ToUpper()} {vehicle.Model.ToUpper()}",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    Location = new Point(10, 10),
                    AutoSize = true
                };

                // Vehicle info panel
                var panelInfo = new Panel
                {
                    Location = new Point(10, 50),
                    Size = new Size(560, 100),
                    BorderStyle = BorderStyle.FixedSingle
                };

                var lblVehicleInfo = new Label
                {
                    Text = $"Vehicle: {vehicle.Brand} {vehicle.Model}\n" +
                           $"License Plate: {vehicle.LicensePlate}\n" +
                           $"Category: {vehicle.Category}\n" +
                           $"Current Kilometers: {vehicle.Kilometers} km\n" +
                           $"Last Maintenance: {vehicle.LastMaintenanceDate:yyyy-MM-dd}",
                    Location = new Point(10, 10),
                    Size = new Size(540, 80),
                    Font = new Font("Arial", 9)
                };

                panelInfo.Controls.Add(lblVehicleInfo);

                // Maintenance schedule info
                DateTime nextMaintenance = vehicle.LastMaintenanceDate.AddMonths(6);
                string nextMaintenanceStr = nextMaintenance < DateTime.Today
                    ? "OVERDUE!"
                    : nextMaintenance.ToString("yyyy-MM-dd");

                var lblSchedule = new Label
                {
                    Text = $"⏰ Next maintenance due: {nextMaintenanceStr} (6 months from last)\n" +
                           $"📅 Days remaining: {(nextMaintenance - DateTime.Today).Days} days",
                    Location = new Point(10, 160),
                    Size = new Size(560, 40),
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    ForeColor = nextMaintenance < DateTime.Today ? Color.Red : Color.DarkGreen
                };

                // Simulated maintenance history (in real app, this would be from MaintenanceRecords table)
                var dataGridView = new DataGridView
                {
                    Location = new Point(10, 210),
                    Size = new Size(560, 200),
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    RowHeadersVisible = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };

                // Create sample history (in real app, fetch from database)
                var maintenanceHistory = new[]
                {
            new
            {
                Date = vehicle.LastMaintenanceDate.ToString("yyyy-MM-dd"),
                Type = "Regular Service",
                Kilometers = $"{vehicle.Kilometers - 5000} km",
                Cost = "$150.00",
                Notes = "Oil change, filter replacement"
            },
            new
            {
                Date = vehicle.LastMaintenanceDate.AddMonths(-6).ToString("yyyy-MM-dd"),
                Type = "Regular Service",
                Kilometers = $"{vehicle.Kilometers - 10000} km",
                Cost = "$120.00",
                Notes = "Routine checkup"
            },
            new
            {
                Date = vehicle.LastMaintenanceDate.AddMonths(-12).ToString("yyyy-MM-dd"),
                Type = "Major Service",
                Kilometers = $"{vehicle.Kilometers - 20000} km",
                Cost = "$350.00",
                Notes = "Brake pads replacement"
            }
        };

                dataGridView.DataSource = maintenanceHistory;

                // Format columns
                if (dataGridView.Columns.Count > 0)
                {
                    dataGridView.Columns["Date"].Width = 100;
                    dataGridView.Columns["Type"].Width = 120;
                    dataGridView.Columns["Kilometers"].Width = 100;
                    dataGridView.Columns["Cost"].Width = 80;
                    dataGridView.Columns["Notes"].Width = 160;
                }

                // Info label (since we're simulating)
                var lblInfo = new Label
                {
                    Text = "ℹ️ Note: Maintenance history is simulated. In a full implementation,\n" +
                           "a MaintenanceRecords table would store actual maintenance history.",
                    Location = new Point(10, 420),
                    Size = new Size(560, 40),
                    Font = new Font("Arial", 8),
                    ForeColor = Color.DarkSlateGray
                };

                // Close button
                var btnClose = new Button
                {
                    Text = "Close",
                    Location = new Point(250, 430),
                    Width = 100
                };
                btnClose.Click += (s, e) => this.Close();

                panel.Controls.AddRange(new Control[]
                {
            lblTitle, panelInfo, lblSchedule, dataGridView, lblInfo, btnClose
                });

                this.Controls.Add(panel);
            }
        }
    }
}