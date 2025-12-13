using CarRental.Data;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CarRental.Admin.UserControls
{
    public partial class RentalsUserControl : UserControl
    {
        private ApplicationDbContext _context;
        private DataGridView dataGridView;
        private Button btnCreateRental;
        private Button btnEditRental;
        private Button btnCancelRental;
        private Button btnViewDetails;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnRefresh;
        private ComboBox cmbFilterStatus;
        private ComboBox cmbFilterPeriod;
        private Label lblStats;

        public RentalsUserControl(ApplicationDbContext context)
        {
            // Create fresh context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new ApplicationDbContext(options);

            SetupUI();
            LoadRentals();
            UpdateStats();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;

            // Top panel with controls
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 120 };
            var panelGrid = new Panel { Dock = DockStyle.Fill };

            // Stats label
            lblStats = new Label
            {
                Location = new Point(10, 10),
                Size = new Size(300, 25),
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            // Search section
            var lblSearch = new Label { Text = "Search:", Location = new Point(10, 45), AutoSize = true };
            txtSearch = new TextBox
            {
                PlaceholderText = "Search by client, vehicle, rental ID...",
                Location = new Point(80, 42),
                Width = 250
            };

            btnSearch = new Button { Text = "Search", Location = new Point(340, 40), Width = 80, AutoSize = true };
            btnSearch.Click += BtnSearch_Click;

            btnRefresh = new Button { Text = "Clear", Location = new Point(430, 40), Width = 80, AutoSize = true };
            btnRefresh.Click += BtnRefresh_Click;

            // ================= FILTERS + ACTIONS =================
            var filtersPanel = new FlowLayoutPanel
            {
                Location = new Point(10, 82),
                Size = new Size(900, 36),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            // Status label
            var lblStatus = new Label
            {
                Text = "Status",
                AutoSize = true,
                Padding = new Padding(0, 7, 6, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Status combobox
            cmbFilterStatus = new ComboBox
            {
                Width = 130,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbFilterStatus.Items.AddRange(new[]
            {
    "All Rentals", "Active", "Upcoming", "Completed", "Cancelled"
});
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndexChanged += FilterChanged;

            // Period label
            var lblPeriod = new Label
            {
                Text = "Period",
                AutoSize = true,
                Padding = new Padding(15, 7, 6, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Period combobox
            cmbFilterPeriod = new ComboBox
            {
                Width = 130,
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            cmbFilterPeriod.Items.AddRange(new[]
            {
    "All Time", "Today", "This Week", "This Month", "Next 7 Days"
});
            cmbFilterPeriod.SelectedIndex = 0;
            cmbFilterPeriod.SelectedIndexChanged += FilterChanged;

            // Buttons
            btnCreateRental = CreateActionButton("➕ Create", Color.LightGreen);
            btnCreateRental.Click += BtnCreateRental_Click;

            btnEditRental = CreateActionButton("✏ Edit", Color.Gainsboro);
            btnEditRental.Click += BtnEditRental_Click;

            btnCancelRental = CreateActionButton("❌ Cancel", Color.LightCoral);
            btnCancelRental.Click += BtnCancelRental_Click;

            btnViewDetails = CreateActionButton("📋 View", Color.Gainsboro);
            btnViewDetails.Click += BtnViewDetails_Click;

            // Add all to panel
            filtersPanel.Controls.Add(lblStatus);
            filtersPanel.Controls.Add(cmbFilterStatus);
            filtersPanel.Controls.Add(lblPeriod);
            filtersPanel.Controls.Add(cmbFilterPeriod);
            filtersPanel.Controls.Add(btnCreateRental);
            filtersPanel.Controls.Add(btnEditRental);
            filtersPanel.Controls.Add(btnCancelRental);
            filtersPanel.Controls.Add(btnViewDetails);

            // Add to top panel
            panelTop.Controls.Add(filtersPanel);
            panelTop.Controls.Add(lblStats);
            panelTop.Controls.Add(lblSearch);
            panelTop.Controls.Add(txtSearch);
            panelTop.Controls.Add(btnSearch);
            panelTop.Controls.Add(btnRefresh);

            // ====================================================




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
        private Button CreateActionButton(string text, Color bg)
        {
            return new Button
            {
                Text = text,
                Size = new Size(110, 30),
                BackColor = bg,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
        }

        private void LoadRentals(string searchTerm = "", string statusFilter = "All Rentals", string periodFilter = "All Time")
        {
            try
            {
                var query = _context.Rentals
                    .Include(r => r.Client)
                    .Include(r => r.Vehicle)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    string search = searchTerm.ToLower();
                    query = query.Where(r =>
                        (r.Client.FirstName + " " + r.Client.LastName).ToLower().Contains(search) ||
                        (r.Vehicle.Brand + " " + r.Vehicle.Model).ToLower().Contains(search) ||
                        r.Id.ToString().Contains(search));
                }

                // Apply status filter
                DateTime now = DateTime.Now;
                switch (statusFilter)
                {
                    case "Active":
                        query = query.Where(r => r.StartDate <= now && r.EndDate >= now);
                        break;
                    case "Upcoming":
                        query = query.Where(r => r.StartDate > now);
                        break;
                    case "Completed":
                        query = query.Where(r => r.EndDate < now);
                        break;
                    case "Cancelled":
                        // Note: We don't have cancellation flag yet
                        // For now, show none or implement cancellation
                        query = query.Where(r => false);
                        break;
                }

                // Apply period filter
                switch (periodFilter)
                {
                    case "Today":
                        query = query.Where(r => r.StartDate.Date == now.Date || r.EndDate.Date == now.Date);
                        break;
                    case "This Week":
                        DateTime weekStart = now.AddDays(-(int)now.DayOfWeek);
                        DateTime weekEnd = weekStart.AddDays(7);
                        query = query.Where(r => r.StartDate >= weekStart && r.StartDate <= weekEnd);
                        break;
                    case "This Month":
                        query = query.Where(r => r.StartDate.Month == now.Month && r.StartDate.Year == now.Year);
                        break;
                    case "Next 7 Days":
                        DateTime nextWeek = now.AddDays(7);
                        query = query.Where(r => r.StartDate >= now && r.StartDate <= nextWeek);
                        break;
                }

                var rentals = query
                    .OrderByDescending(r => r.StartDate)
                    .ToList();

                // Determine rental status for display
                var rentalData = rentals.Select(r => new
                {
                    r.Id,
                    Client = $"{r.Client.FirstName} {r.Client.LastName}",
                    Vehicle = $"{r.Vehicle.Brand} {r.Vehicle.Model}",
                    Period = $"{r.StartDate:yyyy-MM-dd} to {r.EndDate:yyyy-MM-dd}",
                    Days = (r.EndDate - r.StartDate).Days,
                    Total = $"${r.TotalPrice:F2}",
                    BookedOn = r.CreatedAt.ToString("yyyy-MM-dd"),
                    Status = GetRentalStatus(r, now)
                }).ToList();

                dataGridView.DataSource = rentalData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rentals: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetRentalStatus(Rental rental, DateTime now)
        {
            if (rental.StartDate > now)
                return "📅 Upcoming";
            else if (rental.StartDate <= now && rental.EndDate >= now)
                return "✅ Active";
            else
                return "📋 Completed";
        }

        private void UpdateStats()
        {
            try
            {
                DateTime now = DateTime.Now;

                int totalRentals = _context.Rentals.Count();
                int activeRentals = _context.Rentals.Count(r => r.StartDate <= now && r.EndDate >= now);
                int upcomingRentals = _context.Rentals.Count(r => r.StartDate > now);

                decimal todayRevenue = _context.Rentals
                    .Where(r => r.StartDate.Date == now.Date)
                    .Sum(r => r.TotalPrice);

                lblStats.Text = $"📊 Stats: {totalRentals} Total | {activeRentals} Active | {upcomingRentals} Upcoming | Today: ${todayRevenue:F2}";
            }
            catch
            {
                lblStats.Text = "📊 Stats: Loading...";
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            LoadRentals(txtSearch.Text, cmbFilterStatus.SelectedItem.ToString(), cmbFilterPeriod.SelectedItem.ToString());
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterPeriod.SelectedIndex = 0;
            LoadRentals();
            UpdateStats();
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            LoadRentals(txtSearch.Text, cmbFilterStatus.SelectedItem.ToString(), cmbFilterPeriod.SelectedItem.ToString());
        }

        private void BtnCreateRental_Click(object sender, EventArgs e)
        {
            using (var form = new CreateRentalForm(_context))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Create new rental
                        var rental = new Rental
                        {
                            ClientId = form.SelectedClientId,
                            VehicleId = form.SelectedVehicleId,
                            StartDate = form.StartDate,
                            EndDate = form.EndDate,
                            TotalPrice = form.TotalPrice,
                            CreatedAt = DateTime.Now
                        };

                        _context.Rentals.Add(rental);

                        // Mark vehicle as unavailable during rental period
                        var vehicle = _context.Vehicles.Find(form.SelectedVehicleId);
                        if (vehicle != null)
                        {
                            vehicle.IsAvailable = false;
                        }

                        _context.SaveChanges();

                        MessageBox.Show("Rental created successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadRentals();
                        UpdateStats();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating rental: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnEditRental_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a rental to edit", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rentalId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var rental = _context.Rentals
                .Include(r => r.Client)
                .Include(r => r.Vehicle)
                .FirstOrDefault(r => r.Id == rentalId);

            if (rental == null)
            {
                MessageBox.Show("Rental not found", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Check if rental has already started
            if (rental.StartDate < DateTime.Now)
            {
                MessageBox.Show("Cannot edit dates for ongoing or completed rentals", "Not Allowed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new EditRentalForm(rental))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        rental.StartDate = form.NewStartDate;
                        rental.EndDate = form.NewEndDate;
                        rental.TotalPrice = form.NewTotalPrice;

                        _context.SaveChanges();

                        MessageBox.Show("Rental dates updated successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        LoadRentals();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating rental: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnCancelRental_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a rental to cancel", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rentalId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            string clientName = dataGridView.CurrentRow.Cells["Client"].Value.ToString();
            string vehicleInfo = dataGridView.CurrentRow.Cells["Vehicle"].Value.ToString();

            // Check if rental has already started
            var rental = _context.Rentals.Find(rentalId);
            if (rental != null && rental.StartDate < DateTime.Now)
            {
                MessageBox.Show("Cannot cancel ongoing or completed rentals", "Not Allowed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Cancel rental for {clientName} ({vehicleInfo})?\n\n" +
                "This will free up the vehicle for other bookings.",
                "Confirm Cancellation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Free the vehicle
                    var vehicle = _context.Vehicles.Find(rental.VehicleId);
                    if (vehicle != null)
                    {
                        vehicle.IsAvailable = true;
                    }

                    // Remove rental
                    _context.Rentals.Remove(rental);
                    _context.SaveChanges();

                    MessageBox.Show("Rental cancelled successfully", "Cancelled",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadRentals();
                    UpdateStats();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error cancelling rental: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnViewDetails_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a rental", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int rentalId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var rental = _context.Rentals
                .Include(r => r.Client)
                .Include(r => r.Vehicle)
                .FirstOrDefault(r => r.Id == rentalId);

            if (rental == null)
            {
                MessageBox.Show("Rental not found", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var form = new RentalDetailsForm(rental))
            {
                form.ShowDialog();
            }
        }

        // Nested form for creating new rentals
        private class CreateRentalForm : Form
        {
            private ApplicationDbContext _context;
            private ComboBox cmbClients;
            private ComboBox cmbVehicles;
            private DateTimePicker dtpStartDate;
            private DateTimePicker dtpEndDate;
            private NumericUpDown numDays;
            private Label lblDailyRate;
            private Label lblTotalPrice;
            private Button btnCheckAvailability;
            private Button btnSave;
            private Button btnCancel;

            private int _selectedClientId = 0;
            private int _selectedVehicleId = 0;
            private decimal _dailyRate = 0;
            private decimal _totalPrice = 0;

            public int SelectedClientId => _selectedClientId;
            public int SelectedVehicleId => _selectedVehicleId;
            public DateTime StartDate => dtpStartDate.Value;
            public DateTime EndDate => dtpEndDate.Value;
            public decimal TotalPrice => _totalPrice;

            public CreateRentalForm(ApplicationDbContext context)
            {
                _context = context;
                InitializeForm();
                LoadClients();
                LoadAvailableVehicles();
            }

            private void InitializeForm()
            {
                this.Text = "Create New Rental";
                this.Size = new Size(500, 450);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

                int y = 20;

                // Title
                var lblTitle = new Label
                {
                    Text = "📅 CREATE NEW RENTAL",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 40;

                // Client selection
                var lblClient = new Label { Text = "Select Client:", Location = new Point(20, y), Width = 120 };
                cmbClients = new ComboBox { Location = new Point(150, y - 3), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
                cmbClients.SelectedIndexChanged += CmbClients_SelectedIndexChanged;
                y += 35;

                // Vehicle selection
                var lblVehicle = new Label { Text = "Select Vehicle:", Location = new Point(20, y), Width = 120 };
                cmbVehicles = new ComboBox { Location = new Point(150, y - 3), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
                cmbVehicles.SelectedIndexChanged += CmbVehicles_SelectedIndexChanged;
                y += 35;

                // Daily rate display
                lblDailyRate = new Label { Text = "Daily Rate: $0.00", Location = new Point(20, y), Width = 200, Font = new Font("Arial", 9) };
                y += 25;

                // Dates
                var lblDates = new Label { Text = "Rental Dates:", Location = new Point(20, y), Width = 120, Font = new Font("Arial", 10, FontStyle.Bold) };
                y += 25;

                var lblStartDate = new Label { Text = "Start Date:", Location = new Point(40, y), Width = 100 };
                dtpStartDate = new DateTimePicker
                {
                    Location = new Point(150, y - 3),
                    Width = 150,
                    Format = DateTimePickerFormat.Short,
                    MinDate = DateTime.Today
                };
                dtpStartDate.ValueChanged += DatesChanged;
                y += 35;

                var lblEndDate = new Label { Text = "End Date:", Location = new Point(40, y), Width = 100 };
                dtpEndDate = new DateTimePicker
                {
                    Location = new Point(150, y - 3),
                    Width = 150,
                    Format = DateTimePickerFormat.Short,
                    MinDate = DateTime.Today.AddDays(1),
                    Value = DateTime.Today.AddDays(1)
                };
                dtpEndDate.ValueChanged += DatesChanged;
                y += 35;

                // Days display
                var lblDaysLabel = new Label { Text = "Total Days:", Location = new Point(40, y), Width = 100 };
                numDays = new NumericUpDown
                {
                    Location = new Point(150, y - 3),
                    Width = 100,
                    Minimum = 1,
                    Maximum = 365,
                    Value = 1,
                    ReadOnly = true
                };
                numDays.ValueChanged += NumDays_ValueChanged;
                y += 40;

                // Check availability button
                btnCheckAvailability = new Button
                {
                    Text = "✅ Check Availability",
                    Location = new Point(150, y),
                    Width = 150,
                    BackColor = Color.LightBlue
                };
                btnCheckAvailability.Click += BtnCheckAvailability_Click;
                y += 40;

                // Total price
                lblTotalPrice = new Label
                {
                    Text = "Total Price: $0.00",
                    Location = new Point(20, y),
                    Width = 300,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    ForeColor = Color.Green
                };
                y += 40;

                // Buttons
                btnSave = new Button { Text = "Create Rental", Location = new Point(150, y), Width = 120 };
                btnSave.Click += BtnSave_Click;
                btnSave.Enabled = false;

                btnCancel = new Button { Text = "Cancel", Location = new Point(280, y), Width = 120 };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                // Add controls
                panel.Controls.AddRange(new Control[]
                {
            lblTitle, lblClient, cmbClients, lblVehicle, cmbVehicles, lblDailyRate,
            lblDates, lblStartDate, dtpStartDate, lblEndDate, dtpEndDate,
            lblDaysLabel, numDays, btnCheckAvailability, lblTotalPrice,
            btnSave, btnCancel
                });

                this.Controls.Add(panel);

                // Initial calculation
                CalculateTotal();
            }

            private void LoadClients()
            {
                var clients = _context.Clients
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .ToList();

                cmbClients.Items.Clear();
                cmbClients.Items.Add("-- Select Client --");

                foreach (var client in clients)
                {
                    cmbClients.Items.Add(new ClientItem
                    {
                        Id = client.Id,
                        DisplayText = $"{client.FirstName} {client.LastName} - {client.Email}"
                    });
                }

                cmbClients.SelectedIndex = 0;
            }

            private void LoadAvailableVehicles()
            {
                var vehicles = _context.Vehicles
                    .Where(v => v.IsAvailable)
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
                        DisplayText = $"{vehicle.Brand} {vehicle.Model} - ${vehicle.DailyRate}/day - {vehicle.LicensePlate}"
                    });
                }

                cmbVehicles.SelectedIndex = 0;
            }

            private void CmbClients_SelectedIndexChanged(object sender, EventArgs e)
            {
                if (cmbClients.SelectedIndex > 0)
                {
                    var clientItem = (ClientItem)cmbClients.SelectedItem;
                    _selectedClientId = clientItem.Id;
                }
                else
                {
                    _selectedClientId = 0;
                }
                ValidateForm();
            }

            private void CmbVehicles_SelectedIndexChanged(object sender, EventArgs e)
            {
                if (cmbVehicles.SelectedIndex > 0)
                {
                    var vehicleItem = (VehicleItem)cmbVehicles.SelectedItem;
                    _selectedVehicleId = vehicleItem.Id;

                    // Get daily rate
                    var vehicle = _context.Vehicles.Find(_selectedVehicleId);
                    if (vehicle != null)
                    {
                        _dailyRate = vehicle.DailyRate;
                        lblDailyRate.Text = $"Daily Rate: ${_dailyRate:F2}";
                        CalculateTotal();
                    }
                }
                else
                {
                    _selectedVehicleId = 0;
                    _dailyRate = 0;
                    lblDailyRate.Text = "Daily Rate: $0.00";
                    CalculateTotal();
                }
                ValidateForm();
            }

            private void DatesChanged(object sender, EventArgs e)
            {
                // Ensure end date is after start date
                if (dtpEndDate.Value < dtpStartDate.Value)
                {
                    dtpEndDate.Value = dtpStartDate.Value.AddDays(1);
                }

                // Update days
                int days = (dtpEndDate.Value - dtpStartDate.Value).Days;
                if (days < 1) days = 1;

                numDays.Value = days;
                CalculateTotal();
            }

            private void NumDays_ValueChanged(object sender, EventArgs e)
            {
                // Update end date based on days
                dtpEndDate.Value = dtpStartDate.Value.AddDays((int)numDays.Value);
                CalculateTotal();
            }

            private void CalculateTotal()
            {
                int days = (int)numDays.Value;
                _totalPrice = _dailyRate * days;
                lblTotalPrice.Text = $"Total Price: ${_totalPrice:F2}";
            }

            private void BtnCheckAvailability_Click(object sender, EventArgs e)
            {
                if (_selectedVehicleId == 0)
                {
                    MessageBox.Show("Please select a vehicle first", "No Vehicle Selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if vehicle is available for selected dates
                var conflictingRentals = _context.Rentals
                    .Where(r => r.VehicleId == _selectedVehicleId)
                    .Where(r => (r.StartDate <= dtpEndDate.Value && r.EndDate >= dtpStartDate.Value))
                    .ToList();

                if (conflictingRentals.Any())
                {
                    MessageBox.Show("⚠️ Vehicle is not available for the selected dates.\nPlease choose different dates.",
                        "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("✅ Vehicle is available for the selected dates!",
                        "Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            private void ValidateForm()
            {
                bool isValid = _selectedClientId > 0 && _selectedVehicleId > 0 && _dailyRate > 0;
                btnSave.Enabled = isValid;
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                // Final availability check
                var conflictingRentals = _context.Rentals
                    .Where(r => r.VehicleId == _selectedVehicleId)
                    .Where(r => (r.StartDate <= dtpEndDate.Value && r.EndDate >= dtpStartDate.Value))
                    .ToList();

                if (conflictingRentals.Any())
                {
                    MessageBox.Show("Cannot create rental: Vehicle is no longer available for selected dates",
                        "Conflict", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }

            // Helper classes for combo box items
            private class ClientItem
            {
                public int Id { get; set; }
                public string DisplayText { get; set; }
                public override string ToString() => DisplayText;
            }

            private class VehicleItem
            {
                public int Id { get; set; }
                public string DisplayText { get; set; }
                public override string ToString() => DisplayText;
            }
        }
        // Nested form for editing rental dates
        private class EditRentalForm : Form
        {
            private Rental _rental;
            private DateTimePicker dtpStartDate;
            private DateTimePicker dtpEndDate;
            private NumericUpDown numDays;
            private Label lblDailyRate;
            private Label lblTotalPrice;
            private Button btnSave;
            private Button btnCancel;

            private decimal _dailyRate = 0;

            public DateTime NewStartDate => dtpStartDate.Value;
            public DateTime NewEndDate => dtpEndDate.Value;
            public decimal NewTotalPrice { get; private set; }

            public EditRentalForm(Rental rental)
            {
                _rental = rental;

                // Get vehicle daily rate
                using (var context = new ApplicationDbContext(
                    new DbContextOptionsBuilder<ApplicationDbContext>()
                        .UseSqlServer("Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                        .Options))
                {
                    var vehicle = context.Vehicles.Find(rental.VehicleId);
                    _dailyRate = vehicle?.DailyRate ?? 0;
                }

                InitializeForm();
            }

            private void InitializeForm()
            {
                this.Text = "Edit Rental Dates";
                this.Size = new Size(400, 350);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

                int y = 20;

                // Title
                var lblTitle = new Label
                {
                    Text = "✏️ EDIT RENTAL DATES",
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 40;

                // Client and vehicle info
                var client = _rental.Client;
                var vehicle = _rental.Vehicle;

                var lblInfo = new Label
                {
                    Text = $"Client: {client.FirstName} {client.LastName}\nVehicle: {vehicle.Brand} {vehicle.Model}",
                    Location = new Point(20, y),
                    Size = new Size(350, 40),
                    Font = new Font("Arial", 9)
                };
                y += 50;

                // Daily rate
                lblDailyRate = new Label
                {
                    Text = $"Daily Rate: ${_dailyRate:F2}",
                    Location = new Point(20, y),
                    Width = 200
                };
                y += 25;

                // Dates
                var lblStartDate = new Label { Text = "New Start Date:", Location = new Point(20, y), Width = 120 };
                dtpStartDate = new DateTimePicker
                {
                    Location = new Point(150, y - 3),
                    Width = 150,
                    Format = DateTimePickerFormat.Short,
                    MinDate = DateTime.Today,
                    Value = _rental.StartDate < DateTime.Today ? DateTime.Today : _rental.StartDate
                };
                dtpStartDate.ValueChanged += DatesChanged;
                y += 35;

                var lblEndDate = new Label { Text = "New End Date:", Location = new Point(20, y), Width = 120 };
                dtpEndDate = new DateTimePicker
                {
                    Location = new Point(150, y - 3),
                    Width = 150,
                    Format = DateTimePickerFormat.Short,
                    MinDate = dtpStartDate.Value.AddDays(1),
                    Value = _rental.EndDate < dtpStartDate.Value.AddDays(1) ? dtpStartDate.Value.AddDays(1) : _rental.EndDate
                };
                dtpEndDate.ValueChanged += DatesChanged;
                y += 35;

                // Days
                var lblDays = new Label { Text = "Total Days:", Location = new Point(20, y), Width = 120 };
                numDays = new NumericUpDown
                {
                    Location = new Point(150, y - 3),
                    Width = 100,
                    Minimum = 1,
                    Maximum = 365,
                    Value = Math.Max(1, (_rental.EndDate - _rental.StartDate).Days),
                    ReadOnly = true
                };
                numDays.ValueChanged += NumDays_ValueChanged;
                y += 40;

                // Total price
                lblTotalPrice = new Label
                {
                    Text = $"Total Price: ${_rental.TotalPrice:F2}",
                    Location = new Point(20, y),
                    Width = 300,
                    Font = new Font("Arial", 11, FontStyle.Bold)
                };
                CalculateTotal();
                y += 40;

                // Buttons
                btnSave = new Button { Text = "Update", Location = new Point(100, y), Width = 100 };
                btnSave.Click += BtnSave_Click;

                btnCancel = new Button { Text = "Cancel", Location = new Point(210, y), Width = 100 };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                panel.Controls.AddRange(new Control[]
                {
            lblTitle, lblInfo, lblDailyRate,
            lblStartDate, dtpStartDate, lblEndDate, dtpEndDate,
            lblDays, numDays, lblTotalPrice,
            btnSave, btnCancel
                });

                this.Controls.Add(panel);
            }

            private void DatesChanged(object sender, EventArgs e)
            {
                if (dtpEndDate.Value < dtpStartDate.Value)
                {
                    dtpEndDate.Value = dtpStartDate.Value.AddDays(1);
                }

                int days = (dtpEndDate.Value - dtpStartDate.Value).Days;
                if (days < 1) days = 1;

                numDays.Value = days;
                CalculateTotal();
            }

            private void NumDays_ValueChanged(object sender, EventArgs e)
            {
                dtpEndDate.Value = dtpStartDate.Value.AddDays((int)numDays.Value);
                CalculateTotal();
            }

            private void CalculateTotal()
            {
                int days = (int)numDays.Value;
                NewTotalPrice = _dailyRate * days;
                lblTotalPrice.Text = $"Total Price: ${NewTotalPrice:F2} (was ${_rental.TotalPrice:F2})";
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        // Nested form for viewing rental details
        private class RentalDetailsForm : Form
        {
            public RentalDetailsForm(Rental rental)
            {
                InitializeForm(rental);
            }

            private void InitializeForm(Rental rental)
            {
                this.Text = $"Rental Details #{rental.Id}";
                this.Size = new Size(500, 400);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

                int y = 20;

                // Title
                var lblTitle = new Label
                {
                    Text = $"📋 RENTAL DETAILS #{rental.Id}",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 40;

                // Rental Status
                DateTime now = DateTime.Now;
                string status;
                Color statusColor;

                if (rental.StartDate > now)
                {
                    status = "📅 UPCOMING";
                    statusColor = Color.Blue;
                }
                else if (rental.StartDate <= now && rental.EndDate >= now)
                {
                    status = "✅ ACTIVE";
                    statusColor = Color.Green;
                }
                else
                {
                    status = "📋 COMPLETED";
                    statusColor = Color.Gray;
                }

                var lblStatus = new Label
                {
                    Text = status,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true,
                    ForeColor = statusColor
                };
                y += 40;

                // Client Information
                var lblClientTitle = new Label
                {
                    Text = "👤 CLIENT INFORMATION",
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 25;

                AddLabelValue(panel, "Name:", $"{rental.Client.FirstName} {rental.Client.LastName}", ref y);
                AddLabelValue(panel, "Email:", rental.Client.Email, ref y);
                AddLabelValue(panel, "Phone:", rental.Client.Phone ?? "N/A", ref y);
                AddLabelValue(panel, "License:", rental.Client.LicenseNumber ?? "N/A", ref y);

                y += 10;

                // Vehicle Information
                var lblVehicleTitle = new Label
                {
                    Text = "🚗 VEHICLE INFORMATION",
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 25;

                AddLabelValue(panel, "Vehicle:", $"{rental.Vehicle.Brand} {rental.Vehicle.Model}", ref y);
                AddLabelValue(panel, "License Plate:", rental.Vehicle.LicensePlate, ref y);
                AddLabelValue(panel, "Year:", rental.Vehicle.Year.ToString(), ref y);
                AddLabelValue(panel, "Category:", rental.Vehicle.Category.ToString(), ref y);

                y += 10;

                // Rental Details
                var lblRentalTitle = new Label
                {
                    Text = "📅 RENTAL DETAILS",
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    Location = new Point(20, y),
                    AutoSize = true
                };
                y += 25;

                AddLabelValue(panel, "Start Date:", rental.StartDate.ToString("yyyy-MM-dd"), ref y);
                AddLabelValue(panel, "End Date:", rental.EndDate.ToString("yyyy-MM-dd"), ref y);
                AddLabelValue(panel, "Duration:", $"{(rental.EndDate - rental.StartDate).Days} days", ref y);
                AddLabelValue(panel, "Daily Rate:", $"${rental.Vehicle.DailyRate:F2}", ref y);
                AddLabelValue(panel, "Total Price:", $"${rental.TotalPrice:F2}", ref y, true);
                AddLabelValue(panel, "Booked On:", rental.CreatedAt.ToString("yyyy-MM-dd HH:mm"), ref y);

                // Close button
                var btnClose = new Button
                {
                    Text = "Close",
                    Location = new Point(200, y + 20),
                    Width = 100
                };
                btnClose.Click += (s, e) => this.Close();

                panel.Controls.AddRange(new Control[]
                {
            lblTitle, lblStatus, lblClientTitle, lblVehicleTitle, lblRentalTitle, btnClose
                });

                this.Controls.Add(panel);
            }

            private void AddLabelValue(Panel panel, string label, string value, ref int y, bool bold = false)
            {
                var lbl = new Label
                {
                    Text = label,
                    Location = new Point(20, y),
                    Width = 120,
                    Font = bold ? new Font("Arial", 9, FontStyle.Bold) : new Font("Arial", 9)
                };

                var lblValue = new Label
                {
                    Text = value,
                    Location = new Point(150, y),
                    AutoSize = true,
                    Font = bold ? new Font("Arial", 9, FontStyle.Bold) : new Font("Arial", 9)
                };

                panel.Controls.Add(lbl);
                panel.Controls.Add(lblValue);
                y += 25;
            }
        }
    }
}