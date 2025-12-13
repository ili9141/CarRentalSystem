using System;
using System.Drawing;
using System.Windows.Forms;
using CarRental.Data;
using CarRental.Admin.UserControls;

namespace CarRental.Admin
{
    public class MainForm : Form
    {
        private ApplicationDbContext _context;
        private int _currentAdminId;
        private TabControl _tabControl;

        public MainForm(ApplicationDbContext context, int currentAdminId)
        {
            // Store parameters
            _context = context;
            _currentAdminId = currentAdminId;

            // Setup form
            InitializeForm();
            SetupTabs();

            // Set window title
            SetWindowTitle();
        }

        private void InitializeForm()
        {
            this.Text = "Car Rental Admin";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
        }

        private void SetWindowTitle()
        {
            try
            {
                // Only try to get admin name if context exists
                if (_context != null)
                {
                    var admin = _context.AdminUsers.Find(_currentAdminId);
                    if (admin != null)
                    {
                        this.Text = $"Car Rental Admin - Logged in as: {admin.Username}";
                    }
                }
            }
            catch
            {
                // If error, keep default title
            }
        }

        private void SetupTabs()
        {
            // Create TabControl
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                ItemSize = new Size(120, 30)
            };

            // Add tabs in order
            _tabControl.TabPages.Add("Dashboard");
            _tabControl.TabPages.Add("Vehicles");
            _tabControl.TabPages.Add("Rentals");
            _tabControl.TabPages.Add("Clients");
            _tabControl.TabPages.Add("Maintenance");
            _tabControl.TabPages.Add("Payments");
            _tabControl.TabPages.Add("Admins");
            _tabControl.TabPages.Add("My Account");
            // Set up tab content
            SetupTabContent();

            // Add TabControl to form
            this.Controls.Add(_tabControl);
        }

        private void SetupTabContent()
        {
            // Only setup Admins tab for now (others are placeholders)
            foreach (TabPage tab in _tabControl.TabPages)
            {
                tab.Controls.Clear();

                if (tab.Text == "Admins")
                {
                    // Add Admins UserControl
                    var adminsControl = new AdminsUserControl(_context, _currentAdminId);
                    adminsControl.Dock = DockStyle.Fill;
                    tab.Controls.Add(adminsControl);
                }
                else if (tab.Text == "Dashboard")
                {
                    var dashboardControl = new DashboardUserControl(_context); // ADD THIS
                    dashboardControl.Dock = DockStyle.Fill;
                    tab.Controls.Add(dashboardControl);
                }
                else if (tab.Text == "My Account") 
                {
                    var profileControl = new UserProfileUserControl(_context, _currentAdminId);
                    profileControl.Dock = DockStyle.Fill;
                    tab.Controls.Add(profileControl);
                }
                else if (tab.Text == "Vehicles") 
                {
                    var vehiclesControl = new VehiclesUserControl(_context);
                    vehiclesControl.Dock = DockStyle.Fill;
                    tab.Controls.Add(vehiclesControl);
                }
                else if (tab.Text == "Clients") 
                {
                    var clientsControl = new ClientsUserControl(_context);
                    clientsControl.Dock = DockStyle.Fill;
                    tab.Controls.Add(clientsControl);
                }
                else if (tab.Text == "Rentals") 
                {
                    var rentalsControl = new RentalsUserControl(_context);
                    rentalsControl.Dock = DockStyle.Fill;
                    tab.Controls.Add(rentalsControl);
                }
                else if (tab.Text == "Maintenance") 
                {
                    var maintenanceControl = new MaintenanceUserControl(_context);
                    maintenanceControl.Dock = DockStyle.Fill;
                    tab.Controls.Add(maintenanceControl);
                }
                else if (tab.Text == "Payments") 
                {
                    var paymentsControl = new PaymentsUserControl(_context);
                    paymentsControl.Dock = DockStyle.Fill;
                    tab.Controls.Add(paymentsControl);
                }
                else
                {
                    // Add placeholder
                    var label = new Label
                    {
                        Text = $"{tab.Text} Tab\n\nComing Soon",
                        Font = new Font("Arial", 14),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Fill
                    };
                    tab.Controls.Add(label);
                }
            }
        }
    }
}