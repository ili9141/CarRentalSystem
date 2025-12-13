using CarRental.Data;
using CarRental.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace CarRental.Admin.UserControls
{
    public partial class UserProfileUserControl : UserControl
    {
        private ApplicationDbContext _context;
        private int _adminId;
        private AdminUser _currentAdmin;

        private Label lblUsername;
        private Label lblEmail;
        private Label lblCreatedAt;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private Button btnChangePassword;
        private Button btnDeleteAccount;
        private Button btnLogout;

        public UserProfileUserControl(ApplicationDbContext context, int adminId)
        {
            // Create fresh context
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost;Database=CarRentalDB;Trusted_Connection=True;TrustServerCertificate=True;")
                .Options;

            _context = new ApplicationDbContext(options);
            _adminId = adminId;

            LoadCurrentAdmin();
            SetupUI();
        }

        private void LoadCurrentAdmin()
        {
            _currentAdmin = _context.AdminUsers.Find(_adminId);
        }

        private void SetupUI()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;

            // Main container
            var panelMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };

            // Title
            var lblTitle = new Label
            {
                Text = "My Account",
                Font = new Font("Arial", 18, FontStyle.Bold),
                Location = new Point(0, 0),
                Size = new Size(300, 40)
            };

            // Info panel
            var panelInfo = new Panel
            {
                Location = new Point(0, 60),
                Size = new Size(400, 150),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Admin info
            lblUsername = new Label
            {
                Text = $"Username: {_currentAdmin?.Username ?? "N/A"}",
                Location = new Point(20, 20),
                Size = new Size(350, 25),
                Font = new Font("Arial", 11)
            };

            lblEmail = new Label
            {
                Text = $"Email: {_currentAdmin?.Email ?? "N/A"}",
                Location = new Point(20, 50),
                Size = new Size(350, 25),
                Font = new Font("Arial", 11)
            };

            lblCreatedAt = new Label
            {
                Text = $"Member since: {_currentAdmin?.CreatedAt.ToString("yyyy-MM-dd") ?? "N/A"}",
                Location = new Point(20, 80),
                Size = new Size(350, 25),
                Font = new Font("Arial", 11)
            };

            panelInfo.Controls.AddRange(new Control[] { lblUsername, lblEmail, lblCreatedAt });

            // Change password section
            var panelPassword = new Panel
            {
                Location = new Point(0, 230),
                Size = new Size(400, 150),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblPasswordTitle = new Label
            {
                Text = "Change Password",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(20, 15),
                Size = new Size(200, 25)
            };

            var lblNewPassword = new Label
            {
                Text = "New Password:",
                Location = new Point(20, 50),
                Size = new Size(120, 25)
            };

            txtNewPassword = new TextBox
            {
                Location = new Point(150, 47),
                Size = new Size(200, 25),
                PasswordChar = '*'
            };

            var lblConfirm = new Label
            {
                Text = "Confirm:",
                Location = new Point(20, 85),
                Size = new Size(120, 25)
            };

            txtConfirmPassword = new TextBox
            {
                Location = new Point(150, 82),
                Size = new Size(200, 25),
                PasswordChar = '*'
            };

            btnChangePassword = new Button
            {
                Text = "Change Password",
                Location = new Point(150, 115),
                Size = new Size(150, 30),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White
            };
            btnChangePassword.Click += BtnChangePassword_Click;

            panelPassword.Controls.AddRange(new Control[]
            {
                lblPasswordTitle, lblNewPassword, txtNewPassword,
                lblConfirm, txtConfirmPassword, btnChangePassword
            });

            // Danger zone
            var panelDanger = new Panel
            {
                Location = new Point(0, 400),
                Size = new Size(400, 120),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightPink
            };

            var lblDanger = new Label
            {
                Text = "⚠️ Danger Zone",
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(20, 15),
                Size = new Size(200, 25),
                ForeColor = Color.DarkRed
            };

            btnDeleteAccount = new Button
            {
                Text = "Delete My Account",
                Location = new Point(20, 50),
                Size = new Size(180, 35),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnDeleteAccount.Click += BtnDeleteAccount_Click;

            btnLogout = new Button
            {
                Text = "Logout",
                Location = new Point(220, 50),
                Size = new Size(150, 35),
                BackColor = Color.Orange,
                ForeColor = Color.White
            };
            btnLogout.Click += BtnLogout_Click;

            panelDanger.Controls.AddRange(new Control[] { lblDanger, btnDeleteAccount, btnLogout });

            // Add everything to main panel
            panelMain.Controls.AddRange(new Control[]
            {
                lblTitle, panelInfo, panelPassword, panelDanger
            });

            this.Controls.Add(panelMain);
        }

        private void BtnChangePassword_Click(object sender, EventArgs e)
        {
            string newPass = txtNewPassword.Text;
            string confirm = txtConfirmPassword.Text;

            if (string.IsNullOrEmpty(newPass))
            {
                MessageBox.Show("Please enter new password", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass.Length < 4)
            {
                MessageBox.Show("Password must be at least 4 characters", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (newPass != confirm)
            {
                MessageBox.Show("Passwords do not match", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Update password with BCrypt hash
                _currentAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPass);
                _context.SaveChanges();

                MessageBox.Show("Password changed successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtNewPassword.Clear();
                txtConfirmPassword.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeleteAccount_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ WARNING: This will permanently delete your account!\n\n" +
                "You will be logged out immediately and cannot login again.\n" +
                "Are you absolutely sure?",
                "Confirm Account Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Delete admin
                    _context.AdminUsers.Remove(_currentAdmin);
                    _context.SaveChanges();

                    MessageBox.Show("Account deleted. Goodbye!", "Account Deleted",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Logout
                    Logout();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?",
                "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Logout();
            }
        }

        private void Logout()
        {
            // Find parent MainForm
            Form mainForm = this.FindForm();

            // Create new LoginForm
            var loginForm = new LoginForm();
            loginForm.StartPosition = FormStartPosition.CenterScreen;

            // Show LoginForm and close MainForm
            loginForm.Show();
            mainForm?.Close();
        }
    }
}