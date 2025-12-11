using CarRental.Data;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CarRental.Admin.UserControls
{
    public partial class VehiclesUserControl : UserControl
    {
        private ApplicationDbContext _context;
        private DataGridView dataGridView;
        private Button btnAddVehicle;
        private Button btnEditVehicle;
        private Button btnDeleteVehicle;
        private Button btnUpdateKilometers;
        private Button btnToggleAvailability;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnRefresh;
        private ComboBox cmbFilterCategory;
        private ComboBox cmbFilterAvailability;

        public VehiclesUserControl(ApplicationDbContext context)
        {
            // Create fresh context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new ApplicationDbContext(options);

            SetupUI();
            LoadVehicles();
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;

            // Top panel with controls
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 100 };
            var panelGrid = new Panel { Dock = DockStyle.Fill };

            // Search section
            var lblSearch = new Label { Text = "Search:", Location = new Point(10, 15), AutoSize = true };
            txtSearch = new TextBox { PlaceholderText = "Search by plate, brand, model...", Location = new Point(60, 12), Width = 200 };

            btnSearch = new Button { Text = "Search", Location = new Point(270, 10), Width = 80 };
            btnSearch.Click += BtnSearch_Click;

            btnRefresh = new Button { Text = "Clear", Location = new Point(360, 10), Width = 80 };
            btnRefresh.Click += BtnRefresh_Click;

            // Filter section
            var lblFilterCategory = new Label { Text = "Category:", Location = new Point(10, 50), AutoSize = true };
            cmbFilterCategory = new ComboBox { Location = new Point(75, 47), Width = 120 };
            cmbFilterCategory.Items.AddRange(new string[] { "All Categories", "SUV", "Sedan", "Truck", "Van", "Luxury", "Economy" });
            cmbFilterCategory.SelectedIndex = 0;
            cmbFilterCategory.SelectedIndexChanged += FilterChanged;

            var lblFilterAvail = new Label { Text = "Status:", Location = new Point(205, 50), AutoSize = true };
            cmbFilterAvailability = new ComboBox { Location = new Point(250, 47), Width = 120 };
            cmbFilterAvailability.Items.AddRange(new string[] { "All", "Available", "Unavailable" });
            cmbFilterAvailability.SelectedIndex = 0;
            cmbFilterAvailability.SelectedIndexChanged += FilterChanged;

            // Action buttons
            btnAddVehicle = new Button { Text = "+ Add Vehicle", Location = new Point(450, 10), Width = 120, BackColor = Color.LightGreen };
            btnAddVehicle.Click += BtnAddVehicle_Click;

            btnEditVehicle = new Button { Text = "Edit Vehicle", Location = new Point(450, 45), Width = 120 };
            btnEditVehicle.Click += BtnEditVehicle_Click;

            btnDeleteVehicle = new Button { Text = "Delete Vehicle", Location = new Point(580, 10), Width = 120, BackColor = Color.LightCoral };
            btnDeleteVehicle.Click += BtnDeleteVehicle_Click;

            btnUpdateKilometers = new Button { Text = "Update KM", Location = new Point(580, 45), Width = 120 };
            btnUpdateKilometers.Click += BtnUpdateKilometers_Click;

            btnToggleAvailability = new Button { Text = "Toggle Available", Location = new Point(710, 10), Width = 120 };
            btnToggleAvailability.Click += BtnToggleAvailability_Click;

            // Add to top panel
            panelTop.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch, btnSearch, btnRefresh,
                lblFilterCategory, cmbFilterCategory, lblFilterAvail, cmbFilterAvailability,
                btnAddVehicle, btnEditVehicle, btnDeleteVehicle, btnUpdateKilometers, btnToggleAvailability
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

        private void LoadVehicles()
        {
            try
            {
                var vehicles = _context.Vehicles
                    .OrderBy(v => v.Brand)
                    .ThenBy(v => v.Model)
                    .ToList();

                dataGridView.DataSource = vehicles.Select(v => new
                {
                    v.Id,
                    Plate = v.LicensePlate,
                    v.Brand,
                    v.Model,
                    v.Year,
                    Category = v.Category.ToString(),
                    Rate = $"${v.DailyRate}/day",
                    Kilometers = $"{v.Kilometers} km",
                    Available = v.IsAvailable ? "✅" : "❌",
                    LastMaintenance = v.LastMaintenanceDate.ToString("yyyy-MM-dd")
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading vehicles: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cmbFilterCategory.SelectedIndex = 0;
            cmbFilterAvailability.SelectedIndex = 0;
            LoadVehicles();
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            try
            {
                var query = _context.Vehicles.AsQueryable();

                // Search text
                if (!string.IsNullOrEmpty(txtSearch.Text))
                {
                    string search = txtSearch.Text.ToLower();
                    query = query.Where(v =>
                        v.LicensePlate.ToLower().Contains(search) ||
                        v.Brand.ToLower().Contains(search) ||
                        v.Model.ToLower().Contains(search));
                }

                // Filter by category
                if (cmbFilterCategory.SelectedIndex > 0)
                {
                    string selectedCategory = cmbFilterCategory.SelectedItem.ToString();
                    query = query.Where(v => v.Category.ToString() == selectedCategory);
                }

                // Filter by availability
                if (cmbFilterAvailability.SelectedIndex == 1) // Available
                    query = query.Where(v => v.IsAvailable);
                else if (cmbFilterAvailability.SelectedIndex == 2) // Unavailable
                    query = query.Where(v => !v.IsAvailable);

                var vehicles = query
                    .OrderBy(v => v.Brand)
                    .ThenBy(v => v.Model)
                    .ToList();

                dataGridView.DataSource = vehicles.Select(v => new
                {
                    v.Id,
                    Plate = v.LicensePlate,
                    v.Brand,
                    v.Model,
                    v.Year,
                    Category = v.Category.ToString(),
                    Rate = $"${v.DailyRate}/day",
                    Kilometers = $"{v.Kilometers} km",
                    Available = v.IsAvailable ? "✅" : "❌",
                    LastMaintenance = v.LastMaintenanceDate.ToString("yyyy-MM-dd")
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filter error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddVehicle_Click(object sender, EventArgs e)
        {
            using (var form = new VehicleForm(null))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _context.Vehicles.Add(form.Vehicle);
                        _context.SaveChanges();
                        MessageBox.Show("Vehicle added successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadVehicles();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding vehicle: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnEditVehicle_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a vehicle to edit", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int vehicleId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var vehicle = _context.Vehicles.Find(vehicleId);

            if (vehicle == null)
            {
                MessageBox.Show("Vehicle not found", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var form = new VehicleForm(vehicle))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _context.SaveChanges();
                        MessageBox.Show("Vehicle updated successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadVehicles();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating vehicle: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnDeleteVehicle_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a vehicle to delete", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int vehicleId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            string plate = dataGridView.CurrentRow.Cells["Plate"].Value.ToString();
            string brandModel = $"{dataGridView.CurrentRow.Cells["Brand"].Value} {dataGridView.CurrentRow.Cells["Model"].Value}";

            // Check for active rentals
            var hasActiveRentals = _context.Rentals.Any(r =>
     r.VehicleId == vehicleId &&
     r.StartDate <= DateTime.Now &&
     r.EndDate >= DateTime.Now);

            if (hasActiveRentals)
            {
                MessageBox.Show($"Cannot delete {brandModel} - it has active rentals!", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Delete vehicle {brandModel} ({plate})?\n\n⚠️ WARNING: This will DELETE ALL associated rentals!\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var vehicle = _context.Vehicles.Find(vehicleId);
                    if (vehicle != null)
                    {
                        // Cascade delete rentals
                        var rentals = _context.Rentals.Where(r => r.VehicleId == vehicleId).ToList();
                        _context.Rentals.RemoveRange(rentals);

                        // Delete vehicle
                        _context.Vehicles.Remove(vehicle);
                        _context.SaveChanges();

                        MessageBox.Show($"Vehicle {brandModel} deleted successfully", "Deleted",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadVehicles();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting vehicle: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnUpdateKilometers_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a vehicle", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int vehicleId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var vehicle = _context.Vehicles.Find(vehicleId);

            if (vehicle == null) return;

            using (var form = new UpdateKilometersForm(vehicle.Kilometers))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        vehicle.Kilometers = form.NewKilometers;
                        _context.SaveChanges();
                        MessageBox.Show("Kilometers updated successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadVehicles();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating kilometers: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnToggleAvailability_Click(object sender, EventArgs e)
        {
            if (dataGridView.CurrentRow == null)
            {
                MessageBox.Show("Please select a vehicle", "Selection Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int vehicleId = (int)dataGridView.CurrentRow.Cells["Id"].Value;
            var vehicle = _context.Vehicles.Find(vehicleId);

            if (vehicle == null) return;

            try
            {
                vehicle.IsAvailable = !vehicle.IsAvailable;
                _context.SaveChanges();

                string status = vehicle.IsAvailable ? "available" : "unavailable";
                MessageBox.Show($"Vehicle marked as {status}", "Status Updated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadVehicles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Nested VehicleForm class
        private class VehicleForm : Form
        {
            private Vehicle _vehicle;
            private TextBox txtLicensePlate;
            private TextBox txtBrand;
            private TextBox txtModel;
            private NumericUpDown numYear;
            private ComboBox cmbCategory;
            private NumericUpDown numDailyRate;
            private NumericUpDown numKilometers;
            private CheckBox chkAvailable;
            private DateTimePicker dtpLastMaintenance;
            private TextBox txtImagePath;
            private Button btnSave;
            private Button btnCancel;

            public Vehicle Vehicle => _vehicle;

            public VehicleForm(Vehicle vehicle = null)
            {
                _vehicle = vehicle ?? new Vehicle();
                InitializeForm();
                LoadVehicleData();
            }

            private void InitializeForm()
            {
                this.Text = _vehicle.Id == 0 ? "Add New Vehicle" : "Edit Vehicle";
                this.Size = new Size(500, 500);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                // Create layout
                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

                int y = 20;
                int labelWidth = 140;
                int fieldWidth = 200;

                // License Plate
                var lblPlate = new Label { Text = "License Plate:", Location = new Point(20, y), Width = labelWidth };
                txtLicensePlate = new TextBox { Location = new Point(170, y - 3), Width = fieldWidth };
                y += 35;

                // Brand
                var lblBrand = new Label { Text = "Brand:", Location = new Point(20, y), Width = labelWidth };
                txtBrand = new TextBox { Location = new Point(170, y - 3), Width = fieldWidth };
                y += 35;

                // Model
                var lblModel = new Label { Text = "Model:", Location = new Point(20, y), Width = labelWidth };
                txtModel = new TextBox { Location = new Point(170, y - 3), Width = fieldWidth };
                y += 35;

                // Year
                var lblYear = new Label { Text = "Year:", Location = new Point(20, y), Width = labelWidth };
                numYear = new NumericUpDown
                {
                    Location = new Point(170, y - 3),
                    Width = fieldWidth,
                    Minimum = 2000,
                    Maximum = DateTime.Now.Year + 1,
                    Value = DateTime.Now.Year
                };
                y += 35;

                // Category
                var lblCategory = new Label { Text = "Category:", Location = new Point(20, y), Width = labelWidth };
                cmbCategory = new ComboBox { Location = new Point(170, y - 3), Width = fieldWidth };
                cmbCategory.Items.AddRange(new string[] { "SUV", "Sedan", "Truck", "Van", "Luxury", "Economy" });
                cmbCategory.SelectedIndex = 0;
                y += 35;

                // Daily Rate
                var lblDailyRate = new Label { Text = "Daily Rate ($):", Location = new Point(20, y), Width = labelWidth };
                numDailyRate = new NumericUpDown
                {
                    Location = new Point(170, y - 3),
                    Width = fieldWidth,
                    Minimum = 10,
                    Maximum = 1000,
                    Value = 50,
                    DecimalPlaces = 2
                };
                y += 35;

                // Kilometers
                var lblKilometers = new Label { Text = "Kilometers:", Location = new Point(20, y), Width = labelWidth };
                numKilometers = new NumericUpDown
                {
                    Location = new Point(170, y - 3),
                    Width = fieldWidth,
                    Minimum = 0,
                    Maximum = 1000000,
                    Value = 0
                };
                y += 35;

                // Last Maintenance
                var lblMaintenance = new Label { Text = "Last Maintenance:", Location = new Point(20, y), Width = labelWidth };
                dtpLastMaintenance = new DateTimePicker
                {
                    Location = new Point(170, y - 3),
                    Width = fieldWidth,
                    Format = DateTimePickerFormat.Short
                };
                y += 35;

                // Available
                chkAvailable = new CheckBox { Text = "Available for Rent", Location = new Point(170, y), Width = fieldWidth, Checked = true };
                y += 35;

                // Image Path (optional)
                var lblImage = new Label { Text = "Image Path:", Location = new Point(20, y), Width = labelWidth };
                txtImagePath = new TextBox { Location = new Point(170, y - 3), Width = fieldWidth, PlaceholderText = "Optional" };
                y += 40;

                // Buttons
                btnSave = new Button { Text = "Save", Location = new Point(170, y), Width = 100 };
                btnSave.Click += BtnSave_Click;

                btnCancel = new Button { Text = "Cancel", Location = new Point(280, y), Width = 100 };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                // Add controls to panel
                panel.Controls.AddRange(new Control[]
                {
                    lblPlate, txtLicensePlate,
                    lblBrand, txtBrand,
                    lblModel, txtModel,
                    lblYear, numYear,
                    lblCategory, cmbCategory,
                    lblDailyRate, numDailyRate,
                    lblKilometers, numKilometers,
                    lblMaintenance, dtpLastMaintenance,
                    chkAvailable,
                    lblImage, txtImagePath,
                    btnSave, btnCancel
                });

                this.Controls.Add(panel);
            }

            private void LoadVehicleData()
            {
                if (_vehicle.Id != 0)
                {
                    txtLicensePlate.Text = _vehicle.LicensePlate;
                    txtBrand.Text = _vehicle.Brand;
                    txtModel.Text = _vehicle.Model;
                    numYear.Value = _vehicle.Year;

                    // Set category
                    if (Enum.IsDefined(typeof(VehicleCategory), _vehicle.Category))
                        cmbCategory.SelectedItem = _vehicle.Category.ToString();

                    numDailyRate.Value = _vehicle.DailyRate;
                    numKilometers.Value = _vehicle.Kilometers;
                    dtpLastMaintenance.Value = _vehicle.LastMaintenanceDate;
                    chkAvailable.Checked = _vehicle.IsAvailable;
                    txtImagePath.Text = _vehicle.ImagePath;
                }
            }

            private void BtnSave_Click(object sender, EventArgs e)
            {
                // Validation
                if (string.IsNullOrWhiteSpace(txtLicensePlate.Text))
                {
                    MessageBox.Show("License plate is required", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtBrand.Text))
                {
                    MessageBox.Show("Brand is required", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtModel.Text))
                {
                    MessageBox.Show("Model is required", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Update vehicle object
                _vehicle.LicensePlate = txtLicensePlate.Text.Trim().ToUpper();
                _vehicle.Brand = txtBrand.Text.Trim();
                _vehicle.Model = txtModel.Text.Trim();
                _vehicle.Year = (int)numYear.Value;

                // Parse category from string to enum
                if (Enum.TryParse<VehicleCategory>(cmbCategory.SelectedItem.ToString(), out var category))
                {
                    _vehicle.Category = category;
                }
                else
                {
                    _vehicle.Category = VehicleCategory.Sedan; // Default
                }

                _vehicle.DailyRate = numDailyRate.Value;
                _vehicle.Kilometers = (int)numKilometers.Value;
                _vehicle.LastMaintenanceDate = dtpLastMaintenance.Value;
                _vehicle.IsAvailable = chkAvailable.Checked;
                _vehicle.ImagePath = txtImagePath.Text.Trim();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        // Nested UpdateKilometersForm
        private class UpdateKilometersForm : Form
        {
            private NumericUpDown numKilometers;
            private Button btnSave;
            private Button btnCancel;

            public int NewKilometers => (int)numKilometers.Value;

            public UpdateKilometersForm(int currentKilometers)
            {
                InitializeForm(currentKilometers);
            }

            private void InitializeForm(int currentKilometers)
            {
                this.Text = "Update Kilometers";
                this.Size = new Size(300, 150);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;

                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

                var label = new Label
                {
                    Text = "Enter new kilometers:",
                    Location = new Point(20, 20),
                    AutoSize = true
                };

                numKilometers = new NumericUpDown
                {
                    Location = new Point(20, 50),
                    Width = 240,
                    Minimum = 0,
                    Maximum = 1000000,
                    Value = currentKilometers
                };

                btnSave = new Button { Text = "Update", Location = new Point(20, 90), Width = 100 };
                btnSave.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

                btnCancel = new Button { Text = "Cancel", Location = new Point(130, 90), Width = 100 };
                btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

                panel.Controls.AddRange(new Control[] { label, numKilometers, btnSave, btnCancel });
                this.Controls.Add(panel);
            }
        }
    }
}